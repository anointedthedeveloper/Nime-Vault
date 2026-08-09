using System;
using System.Threading;
using System.Threading.Tasks;
using NimeVault.Models;

namespace NimeVault.Services.Interfaces
{
    public interface IDownloadService
    {
        event EventHandler<DownloadItem>? DownloadProgressChanged;
        event EventHandler<DownloadItem>? DownloadStatusChanged;
        event EventHandler<DownloadItem>? DownloadCompleted;
        event EventHandler<DownloadItem>? DownloadFailed;

        Task StartDownloadAsync(DownloadItem item, CancellationToken cancellationToken = default);
        Task PauseDownloadAsync(string downloadId);
        Task ResumeDownloadAsync(string downloadId);
        Task CancelDownloadAsync(string downloadId);
        Task RetryDownloadAsync(string downloadId);
    }
}
