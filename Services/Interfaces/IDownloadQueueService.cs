using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NimeVault.Models;

namespace NimeVault.Services.Interfaces
{
    public interface IDownloadQueueService
    {
        event EventHandler<QueueItem>? QueueChanged;

        IReadOnlyList<QueueItem> Queue { get; }
        IReadOnlyList<DownloadItem> ActiveDownloads { get; }
        IReadOnlyList<DownloadItem> CompletedDownloads { get; }

        Task EnqueueAsync(DownloadItem item);
        Task DequeueAsync(string queueItemId);
        Task MoveUpAsync(string queueItemId);
        Task MoveDownAsync(string queueItemId);
        Task PauseAllAsync();
        Task ResumeAllAsync();
        Task ClearCompletedAsync();
        Task StartQueueAsync();
    }
}
