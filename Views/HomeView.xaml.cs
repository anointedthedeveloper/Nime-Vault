using System;
using System.Windows.Controls;
using System.Windows.Threading;
using NimeVault.ViewModels;

namespace NimeVault.Views
{
    public partial class HomeView : UserControl
    {
        private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(5) };

        public HomeView()
        {
            InitializeComponent();
            _timer.Tick += (_, _) =>
            {
                if (DataContext is HomeViewModel vm && vm.SpotlightAnime.Count > 1)
                    vm.SpotlightNextCommand.Execute(null);
            };
            Loaded   += (_, _) => _timer.Start();
            Unloaded += (_, _) => _timer.Stop();
        }
    }
}
