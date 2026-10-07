using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NimeVault.Models;

namespace NimeVault.Services.AnimePahe
{
    /// <summary>Passes an anti-bot challenge in a real browser and copies the resulting cookies into the jar.</summary>
    public interface IChallengeSolver
    {
        /// <returns>The browser's User-Agent on success (cookies are bound to it), or null if not solved.</returns>
        Task<string?> SolveAsync(string url, CookieContainer jar, CancellationToken ct);
    }

    /// <summary>
    /// Shared HTTP plumbing for the AnimePahe provider: one cookie-aware HttpClient,
    /// the site's JSON API, and a cache of anime metadata seen in search/airing results.
    /// </summary>
    public class AnimePaheClient
    {
        // animepahe.com redirects to whichever domain is currently live; redirects are followed
        // and the final host is remembered so API/play calls hit it directly.
        public const string DefaultBaseUrl = "https://animepahe.com";
        public const string UserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36";

        private readonly CookieContainer _cookies = new();
        private readonly SemaphoreSlim _hostLock = new(1, 1);
        private string _baseUrl = Environment.GetEnvironmentVariable("ANIMEPAHE_BASE_URL")?.TrimEnd('/') ?? DefaultBaseUrl;
        private bool _hostResolved;

        private readonly SemaphoreSlim _solveLock = new(1, 1);
        private DateTime _lastSolve = DateTime.MinValue;

        public IChallengeSolver? ChallengeSolver { get; set; }
        public string CurrentUserAgent { get; private set; } = UserAgent;

        public HttpClient Http { get; }
        public CookieContainer Cookies => _cookies;

        /// <summary>Metadata cached from search / airing results, keyed by anime session id.</summary>
        public ConcurrentDictionary<string, Anime> AnimeCache { get; } = new();

        public AnimePaheClient()
        {
            var handler = new HttpClientHandler
            {
                CookieContainer = _cookies,
                UseCookies = true,
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.All
            };
            Http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(60) };
            Http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            Http.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/json,*/*");
            Http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
            // Bypass token for the DDoS-Guard setup AnimePahe has historically used.
            Http.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", "__ddg1_=;__ddg2_=;");
        }

        public async Task<string> GetBaseUrlAsync(CancellationToken ct)
        {
            if (_hostResolved) return _baseUrl;
            await _hostLock.WaitAsync(ct);
            try
            {
                if (_hostResolved) return _baseUrl;
                try
                {
                    using var res = await Http.GetAsync(_baseUrl + "/", HttpCompletionOption.ResponseHeadersRead, ct);
                    var final = res.RequestMessage?.RequestUri;
                    if (final != null) _baseUrl = $"{final.Scheme}://{final.Host}";
                }
                catch (HttpRequestException) { /* keep default; real calls will surface the error */ }
                _hostResolved = true;
                return _baseUrl;
            }
            finally { _hostLock.Release(); }
        }

        public async Task<string> GetStringAsync(string pathOrUrl, CancellationToken ct, string? referer = null)
        {
            var url = pathOrUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? pathOrUrl
                : await GetBaseUrlAsync(ct) + pathOrUrl;

            for (int attempt = 0; ; attempt++)
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                if (referer != null) req.Headers.Referrer = new Uri(referer);
                using var res = await Http.SendAsync(req, ct);

                if (await IsChallengeAsync(res))
                {
                    if (attempt == 0 && await TrySolveChallengeAsync(url, ct)) continue;
                    var why = await DescribeAsync(res);
                    throw new HttpRequestException(ChallengeSolver == null
                        ? $"AnimePahe refused the request ({why}). A browser session is needed to pass its anti-bot check."
                        : $"AnimePahe's anti-bot check was not passed ({why}). Complete the check in the browser window and try again.");
                }

                if (!res.IsSuccessStatusCode)
                    throw new HttpRequestException($"AnimePahe request failed ({await DescribeAsync(res)}).");
                return await res.Content.ReadAsStringAsync(ct);
            }
        }

        // A 403/503 from AnimePahe is almost always an anti-bot wall (Cloudflare / DDoS-Guard) that a real
        // browser session can clear, so both are treated as a challenge.
        public static Task<bool> IsChallengeAsync(HttpResponseMessage res)
            => Task.FromResult(res.StatusCode == HttpStatusCode.Forbidden || res.StatusCode == HttpStatusCode.ServiceUnavailable);

        /// <summary>Short human-readable summary of a failed response: status, server, page title.</summary>
        public static async Task<string> DescribeAsync(HttpResponseMessage res)
        {
            var server = res.Headers.TryGetValues("Server", out var v) ? string.Join(",", v) : "unknown server";
            var mitigated = res.Headers.TryGetValues("cf-mitigated", out var m) ? $", cf-mitigated={string.Join(",", m)}" : "";
            string title = "";
            try
            {
                var body = await res.Content.ReadAsStringAsync();
                var t = System.Text.RegularExpressions.Regex.Match(body, @"<title>(.*?)</title>",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
                if (t.Success) title = $", page \"{t.Groups[1].Value.Trim()}\"";
            }
            catch { }
            return $"HTTP {(int)res.StatusCode} from {res.RequestMessage?.RequestUri?.Host}, {server}{mitigated}{title}";
        }

        /// <summary>
        /// Adopts a session cleared in a real browser (copied from its dev tools): the Cookie header
        /// value and the exact User-Agent, since clearance cookies are bound to both.
        /// </summary>
        public void ImportBrowserSession(string url, string cookieHeader, string? userAgent)
        {
            var uri = new Uri(url);
            foreach (var part in cookieHeader.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var eq = part.IndexOf('=');
                if (eq <= 0) continue;
                try { _cookies.Add(uri, new Cookie(part[..eq].Trim(), part[(eq + 1)..].Trim(), "/", uri.Host)); }
                catch (CookieException) { }
            }
            if (!string.IsNullOrWhiteSpace(userAgent))
            {
                CurrentUserAgent = userAgent;
                Http.DefaultRequestHeaders.UserAgent.Clear();
                Http.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
            }
        }

        /// <summary>Opens the browser solver (once at a time) and adopts its cookies + User-Agent.</summary>
        public async Task<bool> TrySolveChallengeAsync(string url, CancellationToken ct)
        {
            var solver = ChallengeSolver;
            if (solver == null) return false;

            var started = DateTime.UtcNow;
            await _solveLock.WaitAsync(ct);
            try
            {
                // Another caller solved while we waited for the lock: just retry with the new cookies.
                if (_lastSolve > started) return true;

                var ua = await solver.SolveAsync(url, _cookies, ct);
                if (ua == null) return false;

                CurrentUserAgent = ua;
                Http.DefaultRequestHeaders.UserAgent.Clear();
                Http.DefaultRequestHeaders.UserAgent.ParseAdd(ua);
                _lastSolve = DateTime.UtcNow;
                return true;
            }
            finally { _solveLock.Release(); }
        }

        public async Task<JsonDocument> GetApiAsync(string query, CancellationToken ct)
        {
            var json = await GetStringAsync("/api?" + query, ct);
            try { return JsonDocument.Parse(json); }
            catch (JsonException)
            {
                throw new HttpRequestException("AnimePahe returned an unexpected response (not JSON).");
            }
        }

        public async Task<string> AnimeUrlAsync(string animeId, CancellationToken ct)
            => $"{await GetBaseUrlAsync(ct)}/anime/{animeId}";

        public async Task<string> PlayUrlAsync(string animeId, string episodeId, CancellationToken ct)
            => $"{await GetBaseUrlAsync(ct)}/play/{animeId}/{episodeId}";

        // ---- JSON helpers ----
        public static string Str(JsonElement e, string name)
        {
            if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(name, out var p)) return string.Empty;
            return p.ValueKind switch
            {
                JsonValueKind.String => p.GetString() ?? string.Empty,
                JsonValueKind.Number => p.GetRawText(),
                _ => string.Empty
            };
        }

        public static int Int(JsonElement e, string name)
        {
            if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(name, out var p)) return 0;
            if (p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var i)) return i;
            if (p.ValueKind == JsonValueKind.Number && p.TryGetDouble(out var d)) return (int)d;
            if (p.ValueKind == JsonValueKind.String &&
                double.TryParse(p.GetString(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var s)) return (int)s;
            return 0;
        }

        public static double Dbl(JsonElement e, string name)
        {
            if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(name, out var p)) return 0;
            if (p.ValueKind == JsonValueKind.Number) return p.GetDouble();
            if (p.ValueKind == JsonValueKind.String &&
                double.TryParse(p.GetString(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var s)) return s;
            return 0;
        }
    }
}
