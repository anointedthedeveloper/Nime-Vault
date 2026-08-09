using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NimeVault.Models;
using NimeVault.Services.Interfaces;

namespace NimeVault.Services.Mock
{
    public class MockDownloadService : IDownloadService
    {
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeCts = new();
        private readonly ConcurrentDictionary<string, bool> _pausedDownloads = new();
        private readonly Random _random = new();

        public event EventHandler<DownloadItem>? DownloadProgressChanged;
        public event EventHandler<DownloadItem>? DownloadStatusChanged;
        public event EventHandler<DownloadItem>? DownloadCompleted;
        public event EventHandler<DownloadItem>? DownloadFailed;

        public async Task StartDownloadAsync(DownloadItem item, CancellationToken cancellationToken = default)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _activeCts[item.Id] = cts;
            _pausedDownloads[item.Id] = false;

            item.Status = DownloadStatus.Downloading;
            item.StartedAt = DateTime.Now;
            // Simulate a file between 300 MB and 1.2 GB
            item.TotalBytes = (long)(_random.NextDouble() * 900_000_000 + 300_000_000);
            item.DownloadedBytes = 0;
            item.Progress = 0;
            DownloadStatusChanged?.Invoke(this, item);

            try
            {
                await SimulateDownloadAsync(item, cts.Token);
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

        private async Task SimulateDownloadAsync(DownloadItem item, CancellationToken token)
        {
            // Occasionally simulate a failure after some progress
            bool willFail = _random.NextDouble() < 0.15;
            long failAt = willFail ? (long)(item.TotalBytes * (_random.NextDouble() * 0.4 + 0.2)) : long.MaxValue;

            while (item.DownloadedBytes < item.TotalBytes)
            {
                token.ThrowIfCancellationRequested();

                // Check pause
                while (_pausedDownloads.TryGetValue(item.Id, out bool paused) && paused)
                {
                    await Task.Delay(100, token);
                    token.ThrowIfCancellationRequested();
                }

                // Simulate variable speed: 20–55 MB/s
                double speedMBps = _random.NextDouble() * 35 + 20;
                double speedBps = speedMBps * 1_000_000;

                // Each tick = 100ms
                long chunkBytes = (long)(speedBps * 0.1);
                item.DownloadedBytes = Math.Min(item.DownloadedBytes + chunkBytes, item.TotalBytes);
                item.Progress = (double)item.DownloadedBytes / item.TotalBytes * 100;
                item.CurrentSpeed = speedBps;
                item.AverageSpeed = speedBps;

                double remaining = item.TotalBytes - item.DownloadedBytes;
                item.EstimatedTimeRemaining = speedBps > 0
                    ? TimeSpan.FromSeconds(remaining / speedBps)
                    : TimeSpan.Zero;

                DownloadProgressChanged?.Invoke(this, item);

                if (item.DownloadedBytes >= failAt)
                    throw new Exception("Network connection interrupted.");

                await Task.Delay(100, token);
            }

            item.Status = DownloadStatus.Completed;
            item.Progress = 100;
            item.DownloadedBytes = item.TotalBytes;
            item.CompletedAt = DateTime.Now;
            item.CurrentSpeed = 0;
            item.EstimatedTimeRemaining = TimeSpan.Zero;
            DownloadCompleted?.Invoke(this, item);
        }

        public Task PauseDownloadAsync(string downloadId)
        {
            _pausedDownloads[downloadId] = true;
            return Task.CompletedTask;
        }

        public Task ResumeDownloadAsync(string downloadId)
        {
            _pausedDownloads[downloadId] = false;
            return Task.CompletedTask;
        }

        public Task CancelDownloadAsync(string downloadId)
        {
            if (_activeCts.TryGetValue(downloadId, out var cts))
                cts.Cancel();
            return Task.CompletedTask;
        }

        public async Task RetryDownloadAsync(string downloadId)
        {
            // Caller is responsible for providing the item and calling StartDownloadAsync again
            await Task.CompletedTask;
        }
    }
}
