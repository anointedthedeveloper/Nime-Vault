using System.ComponentModel;
using System.Windows.Controls;
using NimeVault.ViewModels;

namespace NimeVault.Views
{
    public partial class PlayerView : UserControl
    {
        private PlayerViewModel? _vm;

        public PlayerView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            await PlayerWebView.EnsureCoreWebView2Async();

            _vm = DataContext as PlayerViewModel;
            if (_vm != null)
                _vm.PropertyChanged += OnVmPropertyChanged;
        }

        private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlayerViewModel.StreamUrl) && _vm?.StreamUrl != null)
                Dispatcher.Invoke(() => LoadPlayer(_vm.StreamUrl));
        }

        private void LoadPlayer(string m3u8Url)
        {
            // Inject a self-contained HLS.js player page
            var html = $@"<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
<style>
  * {{ margin:0; padding:0; box-sizing:border-box; }}
  body {{ background:#000; overflow:hidden; }}
  video {{ width:100vw; height:100vh; }}
</style>
<script src='https://cdn.jsdelivr.net/npm/hls.js@latest'></script>
</head>
<body>
<video id='v' controls autoplay></video>
<script>
  var video = document.getElementById('v');
  var src = '{m3u8Url.Replace("'", "\\'")}';
  if (Hls.isSupported()) {{
    var hls = new Hls();
    hls.loadSource(src);
    hls.attachMedia(video);
    hls.on(Hls.Events.MANIFEST_PARSED, function() {{ video.play(); }});
  }} else if (video.canPlayType('application/vnd.apple.mpegurl')) {{
    video.src = src;
    video.play();
  }}
</script>
</body>
</html>";
            PlayerWebView.NavigateToString(html);
        }
    }
}
