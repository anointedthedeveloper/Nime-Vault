using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NimeVault.Models;
using NimeVault.Services.Interfaces;

namespace NimeVault.Services
{
    public class HlsDownloadService : IDownloadService
    {
        private readonly IStreamService _streamService;
        private readonly ISettingsService _settingsService;
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeCts = new();
        private readonly ConcurrentDictionary<string, bool> _paused = new();

        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        static HlsDownloadService()
        {
            _http.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124 Safari/537.36");
        }

        public event EventHandler<DownloadItem>? DownloadProgressChanged;
        public event EventHandler<DownloadItem>? DownloadStatusChanged;
        public event EventHandler<DownloadItem>? DownloadCompleted;
        public event EventHandler<DownloadItem>? DownloadFailed;

        public HlsDownloadService(IStreamService streamService, ISettingsService settingsService)
        {
            _streamService = streamService;
            _settingsService = settingsService;
        }

        public async Task StartDownloadAsync(DownloadItem item, CancellationToken cancellationToken = default)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _activeCts[item.Id] = cts;
            _paused[item.Id] = false;

            item.Status = DownloadStatus.Downloading;
            item.StartedAt = DateTime.Now;
            DownloadStatusChanged?.Invoke(this, item);

            try
            {
                // 1. Resolve .m3u8 URL via Playwright
                var m3u8Url = await _streamService.ResolveStreamUrlAsync(
                    item.AnimeId, item.EpisodeNumber, item.Language.ToLower(), cts.Token);

                if (string.IsNullOrEmpty(m3u8Url))
                    throw new Exception("Could not resolve stream URL. The site may have changed or the episode is unavailable.");

                // 2. Parse the .m3u8 playlist to get segment URLs
                var segments = await ParseM3U8Async(m3u8Url, cts.Token);
                if (segments.Count == 0)
                    throw new Exception("No video segments found in the stream playlist.");

                item.TotalBytes = segments.Count * 1_500_000L; // rough estimate: ~1.5MB per segment

                // 3. Download all segments
                var saveDir = _settingsService.Settings.DownloadLocation;
                Directory.CreateDirectory(saveDir);

                var safeName = string.Concat($"{item.AnimeTitle} - Ep{item.EpisodeNumber:D2} [{item.Language}]"
                    .Split(Path.GetInvalidFileNameChars()));
                var tempDir = Path.Combine(saveDir, $".tmp_{item.Id}");
                Directory.CreateDirectory(tempDir);

                var segmentFiles = new List<string>();
                long downloadedBytes = 0;
                var sw = Stopwatch.StartNew();

                for (int i = 0; i < segments.Count; i++)
                {
                    cts.Token.ThrowIfCancellationRequested();

                    // Handle pause
                    while (_paused.TryGetValue(item.Id, out bool paused) && paused)
                    {
                        item.Status = DownloadStatus.Paused;
                        DownloadStatusChanged?.Invoke(this, item);
                        await Task.Delay(200, cts.Token);
                    }
                    if (item.Status == DownloadStatus.Paused)
                    {
                        item.Status = DownloadStatus.Downloading;
                        DownloadStatusChanged?.Invoke(this, item);
                    }

                    var segPath = Path.Combine(tempDir, $"seg_{i:D5}.ts");
                    segmentFiles.Add(segPath);

                    if (!File.Exists(segPath))
                    {
                        var segBytes = await _http.GetByteArrayAsync(segments[i], cts.Token);
                        await File.WriteAllBytesAsync(segPath, segBytes, cts.Token);
                        downloadedBytes += segBytes.Length;
                    }

                    item.DownloadedBytes = downloadedBytes;
                    item.Progress = (double)(i + 1) / segments.Count * 100;
                    item.CurrentSpeed = downloadedBytes / Math.Max(sw.Elapsed.TotalSeconds, 0.001);
                    item.AverageSpeed = item.CurrentSpeed;
                    var remaining = segments.Count - i - 1;
                    item.EstimatedTimeRemaining = item.CurrentSpeed > 0
                        ? TimeSpan.FromSeconds(remaining * 1_500_000 / item.CurrentSpeed)
                        : TimeSpan.Zero;

                    DownloadProgressChanged?.Invoke(this, item);
                }

                // 4. Merge segments with ffmpeg (concat demuxer)
                var outputPath = Path.Combine(saveDir, safeName + ".mp4");
                item.SavePath = outputPath;
                await MergeSegmentsAsync(segmentFiles, outputPath, cts.Token);

                // 5. Cleanup temp files
                try { Directory.Delete(tempDir, true); } catch { }

                item.Status = DownloadStatus.Completed;
                item.Progress = 100;
                item.CompletedAt = DateTime.Now;
                item.CurrentSpeed = 0;
                item.EstimatedTimeRemaining = TimeSpan.Zero;
                DownloadCompleted?.Invoke(this, item);
            }
            catch (OperationCanceledException)
            {
                if (item.Status != DownloadStatus.Paused)
                {
                    item.Status = DownloadStatus.Cancelled;
                    DownloadStatusChanged?.Invoke(this, item);
                }
            }
            catch (Exception ex)
            {
                item.Status = DownloadStatus.Failed;
                item.ErrorMessage = ex.Message;
                DownloadFailed?.Invoke(this, item);
            }
            finally
            {
                _activeCts.TryRemove(item.Id, out _);
            }
        }

        private static async Task<List<string>> ParseM3U8Async(string m3u8Url, CancellationToken ct)
        {
            var content = await _http.GetStringAsync(m3u8Url, ct);
            var baseUri = new Uri(m3u8Url);
            var segments = new List<string>();

            foreach (var line in content.Split('\n'))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#')) continue;

                // If it's another .m3u8 (master playlist pointing to quality variant), recurse once
                if (trimmed.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase))
                {
                    var variantUrl = trimmed.StartsWith("http") ? trimmed : new Uri(baseUri, trimmed).ToString();
                    return await ParseM3U8Async(variantUrl, ct);
                }

                var segUrl = trimmed.StartsWith("http") ? trimmed : new Uri(baseUri, trimmed).ToString();
                segments.Add(segUrl);
            }

            return segments;
        }

        private static async Task MergeSegmentsAsync(List<string> segmentFiles, string outputPath, CancellationToken ct)
        {
            // Write ffmpeg concat list
            var listPath = outputPath + ".concat.txt";
            var lines = segmentFiles.Select(f => $"file '{f.Replace("'", "'\\''")}'");
            await File.WriteAllLinesAsync(listPath, lines, ct);

            var ffmpegPath = FindFfmpeg();
            if (ffmpegPath == null)
            {
                // Fallback: simple binary concat (works for most TS streams)
                await using var output = File.Create(outputPath);
                foreach (var seg in segmentFiles)
                {
                    var data = await File.ReadAllBytesAsync(seg, ct);
                    await output.WriteAsync(data, ct);
                }
                File.Delete(listPath);
                return;
            }

            var psi = new ProcessStartInfo(ffmpegPath,
                $"-y -f concat -safe 0 -i \"{listPath}\" -c copy \"{outputPath}\"")
            {
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi)!;
            await proc.WaitForExitAsync(ct);
            File.Delete(listPath);
        }

        private static string? FindFfmpeg()
        {
            // Check app directory first, then PATH
            var local = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");
            if (File.Exists(local)) return local;

            foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'))
            {
                var candidate = Path.Combine(dir.Trim(), "ffmpeg.exe");
                if (File.Exists(candidate)) return candidate;
            }
            return null;
        }

        public Task PauseDownloadAsync(string downloadId)
        {
            _paused[downloadId] = true;
            return Task.CompletedTask;
        }

        public Task ResumeDownloadAsync(string downloadId)
        {
            _paused[downloadId] = false;
            return Task.CompletedTask;
        }

        public Task CancelDownloadAsync(string downloadId)
        {
            if (_activeCts.TryGetValue(downloadId, out var cts)) cts.Cancel();
            return Task.CompletedTask;
        }

        public Task RetryDownloadAsync(string downloadId) => Task.CompletedTask;
    }
}
