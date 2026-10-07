using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using NimeVault.Views;

namespace NimeVault.Services.AnimePahe
{
    /// <summary>Shows <see cref="ChallengeWindow"/> on the UI thread and returns the browser's User-Agent.</summary>
    public class WebView2ChallengeSolver : IChallengeSolver
    {
        public async Task<string?> SolveAsync(string url, CookieContainer jar, CancellationToken ct)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null) return null;

            return await dispatcher.InvokeAsync(async () =>
            {
                var window = new ChallengeWindow(url, jar)
                {
                    Owner = Application.Current?.MainWindow
                };
                using var reg = ct.Register(() => dispatcher.BeginInvoke(new Action(window.Close)));
                return await window.RunAsync();
            }).Task.Unwrap();
        }
    }
}
