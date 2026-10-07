using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NimeVault.Models;
using NimeVault.Services.Interfaces;
using HtmlDoc = HtmlAgilityPack.HtmlDocument;
using HtmlEntity = HtmlAgilityPack.HtmlEntity;

namespace NimeVault.Services
{
    public class AniWavesDetailsService : IAnimeDetailsService
    {
        private static readonly HttpClient _http = new()
        {
            BaseAddress = new Uri("https://aniwaves.ru"),
            Timeout = TimeSpan.FromSeconds(20)
        };

        static AniWavesDetailsService()
        {
            _http.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124 Safari/537.36");
            _http.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        }

        public async Task<Anime?> GetDetailsAsync(string animeId, CancellationToken ct = default)
        {
            var slug = animeId.StartsWith("watch/") ? animeId[6..] : animeId;
            string html;
            try { html = await _http.GetStringAsync($"/watch/{slug}", ct); }
            catch { return null; }

            var doc = new HtmlDoc();
            doc.LoadHtml(html);

            // data-id from watch-main
            var mainNode = doc.DocumentNode.SelectSingleNode("//*[@id='watch-main']");
            var numericId = mainNode?.GetAttributeValue("data-id", "") ?? AniWavesSearchService.ExtractNumericId(slug);

            // Parse JSON-LD — contains all metadata reliably
            var ldNode = doc.DocumentNode.SelectSingleNode("//script[@type='application/ld+json']");
            var ld = ldNode != null ? TryParseJsonLd(ldNode.InnerText) : null;

            var title     = ld?.title     ?? HtmlEntity.DeEntitize(doc.DocumentNode.SelectSingleNode("//h1")?.InnerText.Trim() ?? slug);
            var altTitle  = ld?.altTitle  ?? "";
            // Full description from .film-description .text.content (JSON-LD is truncated)
            var descNode  = doc.DocumentNode.SelectSingleNode("//*[contains(@class,'film-description')]//*[contains(@class,'text') and contains(@class,'content')]");
            var desc      = descNode != null
                ? HtmlEntity.DeEntitize(descNode.InnerText.Trim())
                : (ld?.desc ?? "");
            var genres    = ld?.genres    ?? new List<string>();
            var studio    = ld?.studio    ?? "";
            var rating    = ld?.rating    ?? 0.0;
            var ageRating = ld?.ageRating ?? "";
            var aired     = ld?.startDate ?? "";
            var subCount  = ld?.subCount  ?? 0;
            var dubCount  = ld?.dubCount  ?? 0;

            int.TryParse(new string((aired).Where(char.IsDigit).Take(4).ToArray()), out var year);

            // Poster from img tag
            var poster = doc.DocumentNode
                .SelectSingleNode(".//div[contains(@class,'poster')]//img")
                ?.GetAttributeValue("src", "") ?? "";
            if (!string.IsNullOrEmpty(poster) && poster.StartsWith("/"))
                poster = "https://aniwaves.ru" + poster;

            // Type from og:title pattern e.g. "One Piece (1999) – Watch TV Anime"
            var ogTitle = doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']")?.GetAttributeValue("content", "") ?? "";
            var animeType = ogTitle.Contains("TV") ? "TV" : ogTitle.Contains("Movie") ? "Movie" : "";

            return new Anime
            {
                Id               = string.IsNullOrEmpty(numericId) ? slug : numericId,
                Slug             = slug,
                Title            = title,
                AlternativeTitle = altTitle,
                Description      = desc,
                PosterUrl        = poster,
                BackgroundUrl    = "",
                Genres           = genres,
                Year             = year,
                Status           = "",
                Rating           = rating,
                TotalEpisodes    = subCount > 0 ? subCount : dubCount,
                AgeRating        = ageRating,
                AnimeType        = animeType,
                Duration         = "",
                Aired            = aired,
                Studio           = studio,
                Quality          = "HD",
                HasSub           = subCount > 0,
                HasDub           = dubCount > 0
            };
        }

        private record LdData(string title, string altTitle, string desc, List<string> genres,
            string studio, double rating, string ageRating, string startDate, int subCount, int dubCount);

        private static LdData? TryParseJsonLd(string raw)
        {
            try
            {
                // The page has multiple ld+json blocks; find the TVSeries/Movie one
                using var jdoc = JsonDocument.Parse(raw.Trim().TrimEnd('\0'));
                var root = jdoc.RootElement;
                if (!root.TryGetProperty("@type", out var typeProp)) return null;
                var type = typeProp.GetString() ?? "";
                if (type != "TVSeries" && type != "Movie" && type != "TVEpisode") return null;

                var title    = root.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                var altTitle = root.TryGetProperty("alternateName", out var an) ? an.GetString() ?? "" : "";
                var desc     = root.TryGetProperty("description", out var d) ? HtmlEntity.DeEntitize(d.GetString() ?? "") : "";
                var ageRating = root.TryGetProperty("contentRating", out var cr) ? cr.GetString() ?? "" : "";
                // Trim verbose age rating
                if (ageRating.Contains("-")) ageRating = ageRating.Split('-')[0].Trim();

                var startDate = root.TryGetProperty("startDate", out var sd) ? sd.GetString() ?? "" : "";
                if (string.IsNullOrEmpty(startDate) && root.TryGetProperty("datePublished", out var dp))
                    startDate = dp.GetString() ?? "";

                var genres = new List<string>();
                if (root.TryGetProperty("genre", out var gArr) && gArr.ValueKind == JsonValueKind.Array)
                    foreach (var g in gArr.EnumerateArray())
                        if (g.GetString() is string gs) genres.Add(gs);

                var studio = "";
                if (root.TryGetProperty("productionCompany", out var pcArr) && pcArr.ValueKind == JsonValueKind.Array)
                    foreach (var pc in pcArr.EnumerateArray())
                        if (pc.TryGetProperty("name", out var pn)) { studio = pn.GetString() ?? ""; break; }

                double rating = 0;
                if (root.TryGetProperty("aggregateRating", out var ar) && ar.TryGetProperty("ratingValue", out var rv))
                    rating = rv.ValueKind == JsonValueKind.Number ? rv.GetDouble() : 0;

                int subCount = 0, dubCount = 0;
                if (root.TryGetProperty("additionalProperty", out var props) && props.ValueKind == JsonValueKind.Array)
                    foreach (var prop in props.EnumerateArray())
                    {
                        var propName = prop.TryGetProperty("name", out var pname) ? pname.GetString() ?? "" : "";
                        var propVal  = prop.TryGetProperty("value", out var pval) && pval.ValueKind == JsonValueKind.Number ? pval.GetInt32() : 0;
                        if (propName.Contains("Subbed")) subCount = propVal;
                        else if (propName.Contains("Dubbed")) dubCount = propVal;
                    }

                return new LdData(title, altTitle, desc, genres, studio, rating, ageRating, startDate, subCount, dubCount);
            }
            catch { return null; }
        }

        public async Task<List<Episode>> GetEpisodesAsync(string animeId, CancellationToken ct = default)
        {
            // animeId may be numeric or a slug — extract numeric part
            var numericId = AniWavesSearchService.ExtractNumericId(animeId);
            // If still empty, try fetching the page to get the data-id
            if (string.IsNullOrEmpty(numericId))
            {
                var slug = animeId.StartsWith("watch/") ? animeId[6..] : animeId;
                try
                {
                    var pageHtml = await _http.GetStringAsync($"/watch/{slug}", ct);
                    var tmpDoc = new HtmlDoc();
                    tmpDoc.LoadHtml(pageHtml);
                    var mainNode = tmpDoc.DocumentNode.SelectSingleNode("//*[@id='watch-main']");
                    numericId = mainNode?.GetAttributeValue("data-id", "") ?? "";
                    if (string.IsNullOrEmpty(numericId))
                        numericId = AniWavesSearchService.ExtractNumericId(slug);
                }
                catch { return new List<Episode>(); }
            }
            if (string.IsNullOrEmpty(numericId)) return new List<Episode>();

            string json;
            try { json = await _http.GetStringAsync($"/ajax/episode/list/{numericId}", ct); }
            catch { return new List<Episode>(); }

            string html;
            try
            {
                using var jdoc = JsonDocument.Parse(json);
                html = jdoc.RootElement.GetProperty("result").GetString() ?? "";
            }
            catch { html = json; }

            var doc = new HtmlDoc();
            doc.LoadHtml(html);

            var episodes = new List<Episode>();
            var epNodes = doc.DocumentNode.SelectNodes("//a[@data-num]");
            if (epNodes == null) return episodes;

            foreach (var node in epNodes)
            {
                if (!int.TryParse(node.GetAttributeValue("data-num", "0"), out var num)) continue;

                var hasSub = node.GetAttributeValue("data-sub", "0") == "1";
                var hasDub = node.GetAttributeValue("data-dub", "0") == "1";
                var durationSec = node.GetAttributeValue("data-duration", "0");
                var aired = node.GetAttributeValue("data-aired", "");
                var isFiller = node.GetAttributeValue("data-filler", "0") == "1";

                var liTitle = node.ParentNode?.GetAttributeValue("title", "") ?? "";
                var epTitle = liTitle.Contains(" - ")
                    ? liTitle.Split(new[] { " - " }, 2, StringSplitOptions.None)[1]
                    : $"Episode {num}";

                episodes.Add(new Episode
                {
                    Id = $"{numericId}-ep-{num}",
                    AnimeId = numericId,
                    Number = num,
                    Title = HtmlEntity.DeEntitize(epTitle.Trim()),
                    Duration = FormatDuration(durationSec),
                    HasSub = hasSub,
                    HasDub = hasDub,
                    AirDate = aired
                });
            }

            return episodes.OrderBy(e => e.Number).ToList();
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static string FormatDuration(string rawSeconds)
        {
            if (int.TryParse(rawSeconds, out var secs) && secs > 0)
            {
                var m = secs / 60;
                var s = secs % 60;
                return s > 0 ? $"{m}m {s}s" : $"{m}m";
            }
            return rawSeconds;
        }
    }
}
