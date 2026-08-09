namespace NimeVault.Models
{
    public enum QueueStatus
    {
        Waiting,
        Downloading,
        Paused,
        Completed,
        Failed,
        Cancelled
    }

    public enum QueuePriority
    {
        Low,
        Normal,
        High
    }

    public class QueueItem
    {
        public string Id { get; set; } = System.Guid.NewGuid().ToString();
        public string DownloadItemId { get; set; } = string.Empty;
        public DownloadItem? DownloadItem { get; set; }
        public int Position { get; set; }
        public QueuePriority Priority { get; set; } = QueuePriority.Normal;
        public QueueStatus Status { get; set; } = QueueStatus.Waiting;
    }
}
