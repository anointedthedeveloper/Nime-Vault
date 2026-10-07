using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NimeVault.Services.AnimePahe
{
    public record DownloadOption(string PaheUrl, int Resolution, bool IsDub, string Label, long SizeMb);

    /// <summary>
    /// Turns an AnimePahe episode into a direct .mp4 URL:
    /// play page -> pahe.win link -> kwik.si /f/ page (packed JS form) -> POST -> redirect to the file.
    /// </summary>
    public class KwikResolver
    {
        private readonly AnimePaheClient _client;
        private readonly HttpClient _noRedirect;

        public KwikResolver(AnimePaheClient client)
        {
            _client = client;
            var handler = new HttpClientHandler
            {
                CookieContainer = client.Cookies,
                UseCookies = true,
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.All
            };
            _noRedirect = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(60) };
        }

        public async Task<List<DownloadOption>> GetOptionsAsync(string animeId, string episodeId, CancellationToken ct)
        {
            var playUrl = await _client.PlayUrlAsync(animeId, episodeId, ct);
            var html = await _client.GetStringAsync(playUrl, ct);

            var options = new List<DownloadOption>();
            foreach (Match m in Regex.Matches(html,
                @"<a[^>]+href=""(https?://pahe\.[a-z]+/[^""]+)""[^>]*>(.*?)</a>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline))
            {
                var label = WebUtility.HtmlDecode(Regex.Replace(m.Groups[2].Value, "<[^>]+>", " ")).Trim();
                label = Regex.Replace(label, @"\s+", " ");
                var res = Regex.Match(label, @"(\d{3,4})p");
                var size = Regex.Match(label, @"\((\d+)\s*MB\)", RegexOptions.IgnoreCase);
                options.Add(new DownloadOption(
                    m.Groups[1].Value,
                    res.Success ? int.Parse(res.Groups[1].Value) : 0,
                    Regex.IsMatch(label, @"\beng\b", RegexOptions.IgnoreCase),
                    label,
                    size.Success ? long.Parse(size.Groups[1].Value) : 0));
            }
            return options;
        }

        public static DownloadOption? Pick(List<DownloadOption> options, bool wantDub)
        {
            var pool = options.Where(o => o.IsDub == wantDub).ToList();
            if (pool.Count == 0) pool = options; // requested language not offered -> best available
            return pool.OrderByDescending(o => o.Resolution).FirstOrDefault();
        }

        public async Task<string> ResolveDirectUrlAsync(DownloadOption option, CancellationToken ct)
        {
            Exception? last = null;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                try { return await ResolveOnceAsync(option, ct); }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { last = ex; await Task.Delay(800 * (attempt + 1), ct); }
            }
            throw new InvalidOperationException("Could not resolve the download link: " + last?.Message, last);
        }

        private async Task<string> ResolveOnceAsync(DownloadOption option, CancellationToken ct)
        {
            // 1. pahe.win page -> kwik /f/ url (sometimes delivered as a redirect)
            string kwikUrl;
            for (int attempt = 0; ; attempt++)
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, option.PaheUrl);
                req.Headers.UserAgent.ParseAdd(_client.CurrentUserAgent);
                using var res = await _noRedirect.SendAsync(req, ct);

                if (await AnimePaheClient.IsChallengeAsync(res))
                {
                    if (attempt == 0 && await _client.TrySolveChallengeAsync(option.PaheUrl, ct)) continue;
                    throw new InvalidOperationException("pahe.win anti-bot check was not passed.");
                }

                var loc = res.Headers.Location?.ToString();
                if (loc != null && Regex.IsMatch(loc, @"kwik\.[a-z]+/f/")) { kwikUrl = loc; break; }
                var body = await res.Content.ReadAsStringAsync(ct);
                var m = Regex.Match(body, @"https?://kwik\.[a-z]+/f/[\w\-]+");
                if (!m.Success) throw new InvalidOperationException("kwik link not found on pahe page.");
                kwikUrl = m.Value;
                break;
            }

            // 2. kwik page -> form action + token (packed JS)
            var kwikHtml = await _client.GetStringAsync(kwikUrl, ct, referer: option.PaheUrl);
            var (action, token) = ExtractForm(kwikHtml);

            // 3. POST the form; the 302 Location is the file
            using var post = new HttpRequestMessage(HttpMethod.Post, action)
            {
                Content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("_token", token) })
            };
            post.Headers.Referrer = new Uri(kwikUrl);
            post.Headers.UserAgent.ParseAdd(_client.CurrentUserAgent);
            using var postRes = await _noRedirect.SendAsync(post, ct);
            var file = postRes.Headers.Location?.ToString();
            if (string.IsNullOrEmpty(file))
                throw new InvalidOperationException($"kwik did not return a file link (HTTP {(int)postRes.StatusCode}).");
            return file;
        }

        public static (string Action, string Token) ExtractForm(string kwikHtml)
        {
            var text = kwikHtml;
            var packed = Regex.Match(kwikHtml, @"\(""(\w+)"",\d+,""(\w+)"",(\d+),(\d+),\d+\)");
            if (packed.Success)
            {
                text = Decode(packed.Groups[1].Value, packed.Groups[2].Value,
                    int.Parse(packed.Groups[3].Value), int.Parse(packed.Groups[4].Value));
            }

            var action = Regex.Match(text, @"action=""([^""]+)""");
            var token = Regex.Match(text, @"name=""_token""[^>]*value=""([^""]+)""");
            if (!token.Success) token = Regex.Match(text, @"value=""([^""]+)""[^>]*name=""_token""");
            if (!action.Success || !token.Success)
                throw new InvalidOperationException("kwik form not found (page layout changed?).");
            return (WebUtility.HtmlDecode(action.Groups[1].Value), token.Groups[1].Value);
        }

        // Port of kwik's obfuscation routine: each char is a base-`radix` number written with
        // the symbols of `key`, terminated by key[radix], offset by `shift`.
        public static string Decode(string data, string key, int shift, int radix)
        {
            var sb = new StringBuilder();
            int i = 0;
            while (i < data.Length)
            {
                var s = new StringBuilder();
                while (i < data.Length && data[i] != key[radix]) { s.Append(data[i]); i++; }
                var digits = s.ToString();
                for (int j = 0; j < key.Length; j++)
                    digits = digits.Replace(key[j].ToString(), j.ToString());

                long value = 0;
                foreach (var c in digits) value = value * radix + (c - '0');
                sb.Append((char)(value - shift));
                i++;
            }
            return sb.ToString();
        }
    }
}
