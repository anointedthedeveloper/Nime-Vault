namespace NimeVault.Services.Interfaces
{
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }

    public interface INotificationService
    {
        void Show(string message, NotificationType type = NotificationType.Info, int durationMs = 3000);
        void ShowSuccess(string message);
        void ShowError(string message);
        void ShowWarning(string message);
    }
}
