using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NimeVault.Services.Interfaces;

namespace NimeVault.Services
{
    public class PlaywrightStreamService : IStreamService, IAsyncDisposable
    {
        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private bool _initialized;

        public async Task<string?> ResolveStreamUrlAsync(
            string animeId, int episodeNumber, string language = "sub", CancellationToken ct = default)
        {
            await EnsureInitializedAsync();

            var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124 Safari/537.36",
                ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
            });

            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            // Intercept .m3u8 across ALL frames (main page + iframes from embed servers)
            context.Request += (_, req) =>
            {
                var url = req.Url;
                if (url.Contains(".m3u8", StringComparison.OrdinalIgnoreCase) ||
                    url.Contains("/playlist", StringComparison.OrdinalIgnoreCase) && url.Contains("m3u8", StringComparison.OrdinalIgnoreCase))
                    tcs.TrySetResult(url);
            };

            var page = await context.NewPageAsync();
            try
            {
                var slug = animeId;
                var watchUrl = $"https://aniwaves.ru/watch/{slug}?ep={episodeNumber}";

                await page.GotoAsync(watchUrl, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 30_000
                });

                // Click the play button / player area to trigger the embed iframe load
                try
                {
                    await page.WaitForSelectorAsync("#player, #w-player, .play-btn, [aria-label='Play']",
                        new PageWaitForSelectorOptions { Timeout = 6000 });
                    await page.ClickAsync("#player, #w-player, .play-btn, [aria-label='Play']");
                }
                catch { }

                // Also try clicking any visible iframe play button after a short wait
                await Task.Delay(2000);
                try
                {
                    var frames = page.Frames;
                    foreach (var frame in frames)
                    {
                        try
                        {
                            await frame.ClickAsync(".jw-icon-playback, .vjs-big-play-button, [aria-label='Play']",
                                new FrameClickOptions { Timeout = 2000 });
                        }
                        catch { }
                    }
                }
                catch { }

                using var cts2 = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts2.CancelAfter(TimeSpan.FromSeconds(30));

                try { return await tcs.Task.WaitAsync(cts2.Token); }
                catch (OperationCanceledException) { return null; }
            }
            finally
            {
                await page.CloseAsync();
                await context.CloseAsync();
            }
        }

        private async Task EnsureInitializedAsync()
        {
            if (_initialized) return;
            await _initLock.WaitAsync();
            try
            {
                if (_initialized) return;

                // Install Playwright browsers if not already present
                try { Microsoft.Playwright.Program.Main(new[] { "install", "chromium" }); } catch { }

                _playwright = await Playwright.CreateAsync();
                _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = true,
                    Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--disable-blink-features=AutomationControlled" }
                });
                _initialized = true;
            }
            finally { _initLock.Release(); }
        }

        public async ValueTask DisposeAsync()
        {
            if (_browser != null) await _browser.CloseAsync();
            _playwright?.Dispose();
        }
    }
}
