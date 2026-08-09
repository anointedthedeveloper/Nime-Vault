using System.Windows;
using NimeVault.Services;

namespace NimeVault.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Wire up toast notifications
            var notificationService = App.Services.GetService(typeof(NotificationService)) as NotificationService;
            if (notificationService != null)
                notificationService.NotificationRequested += (_, args) =>
                    ToastHostControl.ShowToast(args.Message, args.Type, args.DurationMs);

            // Load home data
            var vm = App.Services.GetService(typeof(ViewModels.MainViewModel)) as ViewModels.MainViewModel;
            _ = vm?.HomeVM.LoadAsync();
        }
    }
}
