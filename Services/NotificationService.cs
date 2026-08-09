using System;
using NimeVault.Services.Interfaces;

namespace NimeVault.Services
{
    public class NotificationService : INotificationService
    {
        public event EventHandler<ToastNotificationArgs>? NotificationRequested;

        public void Show(string message, NotificationType type = NotificationType.Info, int durationMs = 3000)
        {
            NotificationRequested?.Invoke(this, new ToastNotificationArgs(message, type, durationMs));
        }

        public void ShowSuccess(string message) => Show(message, NotificationType.Success);
        public void ShowError(string message) => Show(message, NotificationType.Error);
        public void ShowWarning(string message) => Show(message, NotificationType.Warning);
    }

    public class ToastNotificationArgs
    {
        public string Message { get; }
        public NotificationType Type { get; }
        public int DurationMs { get; }

        public ToastNotificationArgs(string message, NotificationType type, int durationMs)
        {
            Message = message;
            Type = type;
            DurationMs = durationMs;
        }
    }
}
