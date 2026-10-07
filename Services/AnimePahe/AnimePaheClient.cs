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

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            if (referer != null) req.Headers.Referrer = new Uri(referer);
            using var res = await Http.SendAsync(req, ct);

            if (res.StatusCode == HttpStatusCode.Forbidden || res.StatusCode == HttpStatusCode.ServiceUnavailable)
                throw new HttpRequestException(
                    "AnimePahe blocked the request (anti-bot challenge). Try again in a moment, or check the site opens in your browser.");

            res.EnsureSuccessStatusCode();
            return await res.Content.ReadAsStringAsync(ct);
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
