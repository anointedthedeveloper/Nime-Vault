using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using NimeVault.Models;
using NimeVault.Services.Interfaces;

namespace NimeVault.Services.AnimePahe
{
    public class AnimePaheDownloadService : IDownloadService
    {
        private readonly AnimePaheClient _client;
        private readonly KwikResolver _resolver;
        private readonly ISettingsService _settings;

        private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeCts = new();
        private readonly ConcurrentDictionary<string, bool> _paused = new();

        public event EventHandler<DownloadItem>? DownloadProgressChanged;
        public event EventHandler<DownloadItem>? DownloadStatusChanged;
        public event EventHandler<DownloadItem>? DownloadCompleted;
        public event EventHandler<DownloadItem>? DownloadFailed;

        public AnimePaheDownloadService(AnimePaheClient client, KwikResolver resolver, ISettingsService settings)
        {
            _client = client;
            _resolver = resolver;
            _settings = settings;
        }

        public async Task StartDownloadAsync(DownloadItem item, CancellationToken cancellationToken = default)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _activeCts[item.Id] = cts;
            _paused[item.Id] = false;

            item.Status = DownloadStatus.Downloading;
            item.ErrorMessage = null;
            item.StartedAt ??= DateTime.Now;
            DownloadStatusChanged?.Invoke(this, item);

            string? partPath = null;
            try
            {
                var finalPath = BuildPath(item);
                partPath = finalPath + ".part";
                Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
                item.SavePath = finalPath;

                if (File.Exists(finalPath) && new FileInfo(finalPath).Length > 0)
                {
                    var len = new FileInfo(finalPath).Length;
                    Complete(item, len);
                    return;
                }

                var options = await _resolver.GetOptionsAsync(item.AnimeId, item.EpisodeId, cts.Token);
                var choice = KwikResolver.Pick(options, item.Language.Equals("DUB", StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException("No download links were found for this episode.");

                if (!_settings.Settings.ResumeInterruptedDownloads && File.Exists(partPath))
                    File.Delete(partPath);

                await DownloadFileAsync(item, choice, partPath, cts.Token);

                if (File.Exists(finalPath)) File.Delete(finalPath);
                File.Move(partPath, finalPath);
                Complete(item, new FileInfo(finalPath).Length);
            }
            catch (OperationCanceledException) when (!cts.IsCancellationRequested)
            {
                item.Status = DownloadStatus.Failed;
                item.ErrorMessage = "The connection to AnimePahe timed out.";
                item.CurrentSpeed = 0;
                DownloadFailed?.Invoke(this, item);
            }
            catch (OperationCanceledException)
            {
                if (partPath != null) TryDelete(partPath);
                item.Status = DownloadStatus.Cancelled;
                item.CurrentSpeed = 0;
                DownloadStatusChanged?.Invoke(this, item);
            }
            catch (Exception ex)
            {
                item.Status = DownloadStatus.Failed;
                item.ErrorMessage = ex.Message;
                item.CurrentSpeed = 0;
                DownloadFailed?.Invoke(this, item);
            }
            finally
            {
                _activeCts.TryRemove(item.Id, out _);
                _paused.TryRemove(item.Id, out _);
                cts.Dispose();
            }
        }

        private async Task DownloadFileAsync(DownloadItem item, DownloadOption choice, string partPath, CancellationToken ct)
        {
            var directUrl = await _resolver.ResolveDirectUrlAsync(choice, ct);
            long existing = File.Exists(partPath) ? new FileInfo(partPath).Length : 0;
            int staleLinkRetries = 0;

            var clock = Stopwatch.StartNew();
            var lastReport = TimeSpan.Zero;
            long windowBytes = 0;
            var windowStart = TimeSpan.Zero;
            var totalClock = Stopwatch.StartNew();
            long sessionBytes = 0;

            while (true)
            {
                ct.ThrowIfCancellationRequested();
                await WaitWhilePausedAsync(item.Id, ct);

                using var req = new HttpRequestMessage(HttpMethod.Get, directUrl);
                req.Headers.Referrer = new Uri("https://kwik.si/");
                if (existing > 0) req.Headers.Range = new RangeHeaderValue(existing, null);

                using var res = await _client.Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

                if (res.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
                {
                    // Partial file is already complete (or invalid): complete if sizes agree, else restart.
                    if (item.TotalBytes > 0 && existing >= item.TotalBytes) return;
                    TryDelete(partPath); existing = 0; continue;
                }
                if ((res.StatusCode == HttpStatusCode.Forbidden || res.StatusCode == HttpStatusCode.Gone) && staleLinkRetries++ < 2)
                {
                    directUrl = await _resolver.ResolveDirectUrlAsync(choice, ct); // link expired
                    continue;
                }
                res.EnsureSuccessStatusCode();

                bool isPartial = res.StatusCode == HttpStatusCode.PartialContent;
                if (!isPartial && existing > 0) { existing = 0; TryDelete(partPath); } // server ignored Range

                long contentLen = res.Content.Headers.ContentLength ?? 0;
                item.TotalBytes = isPartial
                    ? (res.Content.Headers.ContentRange?.Length ?? existing + contentLen)
                    : contentLen;

                await using var net = await res.Content.ReadAsStreamAsync(ct);
                await using var file = new FileStream(partPath, isPartial ? FileMode.Append : FileMode.Create,
                    FileAccess.Write, FileShare.Read, 81920, useAsync: true);

                var buffer = new byte[81920];
                bool pausedMidway = false;

                while (true)
                {
                    if (_paused.TryGetValue(item.Id, out var p) && p) { pausedMidway = true; break; }

                    int read = await net.ReadAsync(buffer.AsMemory(), ct);
                    if (read == 0) break;

                    await file.WriteAsync(buffer.AsMemory(0, read), ct);
                    existing += read;
                    sessionBytes += read;
                    windowBytes += read;

                    var now = clock.Elapsed;
                    if (now - lastReport >= TimeSpan.FromMilliseconds(250))
                    {
                        var windowSecs = (now - windowStart).TotalSeconds;
                        item.CurrentSpeed = windowSecs > 0 ? windowBytes / windowSecs : 0;
                        item.AverageSpeed = totalClock.Elapsed.TotalSeconds > 0 ? sessionBytes / totalClock.Elapsed.TotalSeconds : 0;
                        item.DownloadedBytes = existing;
                        item.Progress = item.TotalBytes > 0 ? (double)existing / item.TotalBytes * 100 : 0;
                        item.EstimatedTimeRemaining = item.CurrentSpeed > 0 && item.TotalBytes > 0
                            ? TimeSpan.FromSeconds((item.TotalBytes - existing) / item.CurrentSpeed)
                            : TimeSpan.Zero;
                        lastReport = now;
                        windowStart = now;
                        windowBytes = 0;
                        DownloadProgressChanged?.Invoke(this, item);
                    }
                }

                await file.FlushAsync(ct);
                item.DownloadedBytes = existing;
                item.CurrentSpeed = 0;

                if (pausedMidway) continue; // loop top waits for resume, then reconnects with Range

                if (item.TotalBytes > 0 && existing < item.TotalBytes)
                    throw new IOException("Connection closed before the download finished.");
                return;
            }
        }

        private async Task WaitWhilePausedAsync(string id, CancellationToken ct)
        {
            while (_paused.TryGetValue(id, out var p) && p)
                await Task.Delay(200, ct);
        }

        private void Complete(DownloadItem item, long size)
        {
            item.Status = DownloadStatus.Completed;
            item.TotalBytes = size;
            item.DownloadedBytes = size;
            item.Progress = 100;
            item.CurrentSpeed = 0;
            item.EstimatedTimeRemaining = TimeSpan.Zero;
            item.CompletedAt = DateTime.Now;
            DownloadProgressChanged?.Invoke(this, item);
            DownloadCompleted?.Invoke(this, item);
        }

        private string BuildPath(DownloadItem item)
        {
            var root = _settings.Settings.DownloadLocation;
            var anime = Sanitize(item.AnimeTitle.Length > 0 ? item.AnimeTitle : "Unknown");
            var tag = item.Language.Equals("DUB", StringComparison.OrdinalIgnoreCase) ? " [Dub]" : "";
            var name = $"{anime} - E{item.EpisodeNumber:00}{tag}.mp4";
            return Path.Combine(root, anime, name);
        }

        private static string Sanitize(string s)
        {
            var bad = Path.GetInvalidFileNameChars();
            var clean = new string(s.Select(c => bad.Contains(c) ? '_' : c).ToArray()).Trim().TrimEnd('.');
            return clean.Length == 0 ? "_" : clean;
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
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
            if (_activeCts.TryGetValue(downloadId, out var cts))
            {
                try { cts.Cancel(); } catch (ObjectDisposedException) { }
            }
            return Task.CompletedTask;
        }

        // Retry is driven by the caller (it re-invokes StartDownloadAsync); the .part file makes it resume.
        public Task RetryDownloadAsync(string downloadId) => Task.CompletedTask;
    }
}
