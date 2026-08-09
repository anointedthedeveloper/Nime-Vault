namespace NimeVault.Models
{
    public enum DownloadStatus
    {
        Queued,
        Downloading,
        Paused,
        Completed,
        Failed,
        Retrying,
        Cancelled
    }

    public class DownloadItem
    {
        public string Id { get; set; } = System.Guid.NewGuid().ToString();
        public string AnimeId { get; set; } = string.Empty;
        public string EpisodeId { get; set; } = string.Empty;
        public string AnimeTitle { get; set; } = string.Empty;
        public string EpisodeTitle { get; set; } = string.Empty;
        public int EpisodeNumber { get; set; }
        public string Language { get; set; } = "SUB";
        public string PosterUrl { get; set; } = string.Empty;
        public DownloadStatus Status { get; set; } = DownloadStatus.Queued;
        public double Progress { get; set; }
        public long DownloadedBytes { get; set; }
        public long TotalBytes { get; set; }
        public double CurrentSpeed { get; set; }
        public double AverageSpeed { get; set; }
        public System.TimeSpan EstimatedTimeRemaining { get; set; }
        public int RetryCount { get; set; }
        public int MaxRetries { get; set; } = 3;
        public string? ErrorMessage { get; set; }
        public System.DateTime? StartedAt { get; set; }
        public System.DateTime? CompletedAt { get; set; }
        public string? SavePath { get; set; }
    }
}
