using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;

namespace NimeVault.Views
{
    /// <summary>
    /// Loads a URL in a real WebView2 browser until the Cloudflare/DDoS-Guard check passes,
    /// then copies that site's cookies into the app's HTTP cookie jar.
    /// </summary>
    public partial class ChallengeWindow : Window
    {
        private readonly string _url;
        private readonly CookieContainer _jar;
        private readonly TaskCompletionSource<string?> _result = new();
        private readonly DispatcherTimer _poll = new() { Interval = TimeSpan.FromMilliseconds(700) };
        private readonly DispatcherTimer _timeout = new() { Interval = TimeSpan.FromMinutes(3) };
        private bool _checking;

        public ChallengeWindow(string url, CookieContainer jar)
        {
            InitializeComponent();
            _url = url;
            _jar = jar;
            Closed += (_, _) => { _poll.Stop(); _timeout.Stop(); _result.TrySetResult(null); };
        }

        public async Task<string?> RunAsync()
        {
            try
            {
                var dataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NimeVault", "webview");
                var env = await CoreWebView2Environment.CreateAsync(null, dataDir);
                await Browser.EnsureCoreWebView2Async(env);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    "The embedded browser (WebView2 Runtime) could not start:\n" + ex.Message +
                    "\n\nInstall it from https://developer.microsoft.com/microsoft-edge/webview2/",
                    "Nime Vault", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            _poll.Tick += async (_, _) => await CheckAsync();
            _timeout.Tick += (_, _) => Close();
            _poll.Start();
            _timeout.Start();

            Browser.CoreWebView2.Navigate(_url);
            Show();

            var ua = await _result.Task;
            if (IsLoaded) Close();
            return ua;
        }

        private async Task CheckAsync()
        {
            if (_checking || Browser.CoreWebView2 == null) return;
            _checking = true;
            try
            {
                var title = Browser.CoreWebView2.DocumentTitle ?? string.Empty;
                if (title.Length == 0 ||
                    title.Contains("Just a moment", StringComparison.OrdinalIgnoreCase) ||
                    title.Contains("DDoS-Guard", StringComparison.OrdinalIgnoreCase) ||
                    title.Contains("Attention Required", StringComparison.OrdinalIgnoreCase))
                    return;

                var cookies = await Browser.CoreWebView2.CookieManager.GetCookiesAsync(Browser.CoreWebView2.Source);
                foreach (var c in cookies)
                {
                    try
                    {
                        _jar.Add(new Cookie(c.Name, c.Value, string.IsNullOrEmpty(c.Path) ? "/" : c.Path, c.Domain));
                    }
                    catch (CookieException) { }
                }

                StatusText.Text = "Verified. Closing...";
                _poll.Stop();
                _result.TrySetResult(Browser.CoreWebView2.Settings.UserAgent);
            }
            finally { _checking = false; }
        }
    }
}
