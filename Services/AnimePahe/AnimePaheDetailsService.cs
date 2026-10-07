using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NimeVault.Models;
using NimeVault.Services.Interfaces;

namespace NimeVault.Services.AnimePahe
{
    public class AnimePaheDetailsService : IAnimeDetailsService
    {
        private readonly AnimePaheClient _client;

        public AnimePaheDetailsService(AnimePaheClient client) => _client = client;

        public async Task<Anime?> GetDetailsAsync(string animeId, CancellationToken cancellationToken = default)
        {
            _client.AnimeCache.TryGetValue(animeId, out var cached);
            var anime = cached ?? new Anime { Id = animeId, HasSub = true };

            string html;
            try { html = await _client.GetStringAsync("/anime/" + animeId, cancellationToken); }
            catch (OperationCanceledException) { throw; }
            catch when (cached != null) { return cached; } // search data is enough to render the page

            var title = Clean(Grab(html, @"<h1[^>]*>\s*(?:<span[^>]*>)?(.*?)(?:</span>)?\s*</h1>"));
            if (title.Length > 0) anime.Title = title;

            var jp = Clean(Grab(html, @"<h2[^>]*class=""japanese""[^>]*>(.*?)</h2>"));
            if (jp.Length > 0) anime.AlternativeTitle = jp;

            var synopsis = Clean(Grab(html, @"<div[^>]*class=""anime-synopsis""[^>]*>(.*?)</div>"));
            if (synopsis.Length > 0) anime.Description = synopsis;

            var poster = Grab(html, @"<div[^>]*class=""anime-poster""[^>]*>.*?<a[^>]*href=""([^""]+)""");
            if (poster.Length == 0) poster = Grab(html, @"<meta[^>]*property=""og:image""[^>]*content=""([^""]+)""");
            if (poster.Length > 0 && anime.PosterUrl.Length == 0) anime.PosterUrl = poster;
            if (anime.BackgroundUrl.Length == 0) anime.BackgroundUrl = anime.PosterUrl;

            var genres = new List<string>();
            foreach (Match m in Regex.Matches(html, @"<a[^>]*href=""/anime/genre/[^""]*""[^>]*title=""([^""]+)""", RegexOptions.IgnoreCase))
                genres.Add(WebUtility.HtmlDecode(m.Groups[1].Value));
            if (genres.Count > 0) anime.Genres = genres;

            var status = Clean(Grab(html, @"<strong>Status:?</strong>\s*(?:<a[^>]*>)?([^<]+)"));
            if (status.Length > 0) anime.Status = status;

            var eps = Grab(html, @"<strong>Episodes:?</strong>\s*(\d+)");
            if (int.TryParse(eps, out var n) && n > 0) anime.TotalEpisodes = n;

            var season = Grab(html, @"<strong>Season:?</strong>.*?(\d{4})");
            if (int.TryParse(season, out var y)) anime.Year = y;

            // The page lists whether an English dub exists in the audio/"Dub" badge text.
            anime.HasSub = true;
            anime.HasDub = Regex.IsMatch(html, @"data-audio=""eng""|>\s*Dub\s*<", RegexOptions.IgnoreCase);

            _client.AnimeCache[animeId] = anime;
            return anime;
        }

        public async Task<List<Episode>> GetEpisodesAsync(string animeId, CancellationToken cancellationToken = default)
        {
            var episodes = new List<Episode>();
            int page = 1, lastPage = 1;
            do
            {
                using var doc = await _client.GetApiAsync(
                    $"m=release&id={animeId}&sort=episode_asc&page={page}", cancellationToken);
                var root = doc.RootElement;
                lastPage = AnimePaheClient.Int(root, "last_page");
                if (lastPage < 1) lastPage = 1;

                if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array) break;
                foreach (var e in data.EnumerateArray())
                {
                    var session = AnimePaheClient.Str(e, "session");
                    if (session.Length == 0) continue;
                    var number = AnimePaheClient.Int(e, "episode");
                    var title = AnimePaheClient.Str(e, "title");
                    var audio = AnimePaheClient.Str(e, "audio");
                    episodes.Add(new Episode
                    {
                        Id = session,
                        AnimeId = animeId,
                        Number = number,
                        Title = title.Length > 0 ? title : $"Episode {number}",
                        Duration = AnimePaheClient.Str(e, "duration"),
                        ThumbnailUrl = AnimePaheClient.Str(e, "snapshot"),
                        AirDate = AnimePaheClient.Str(e, "created_at"),
                        HasSub = !audio.Equals("eng", StringComparison.OrdinalIgnoreCase),
                        HasDub = audio.Equals("eng", StringComparison.OrdinalIgnoreCase)
                    });
                }
                page++;
            } while (page <= lastPage);

            return episodes;
        }

        private static string Grab(string input, string pattern)
        {
            var m = Regex.Match(input, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            return m.Success ? m.Groups[1].Value : string.Empty;
        }

        private static string Clean(string s)
        {
            if (s.Length == 0) return s;
            s = Regex.Replace(s, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, "<[^>]+>", "");
            return WebUtility.HtmlDecode(s).Trim();
        }
    }
}
