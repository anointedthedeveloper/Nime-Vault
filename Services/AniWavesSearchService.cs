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
    public class AniWavesSearchService : IAnimeSearchService
    {
        private static readonly HttpClient _http = new(new HttpClientHandler { AllowAutoRedirect = true })
        {
            BaseAddress = new Uri("https://aniwaves.ru"),
            Timeout = TimeSpan.FromSeconds(20)
        };

        static AniWavesSearchService()
        {
            _http.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124 Safari/537.36");
            _http.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        }

        // ── IAnimeSearchService ──────────────────────────────────────────────

        public async Task<List<Anime>> SearchAsync(string query, CancellationToken ct = default)
        {
            // /ajax/anime/search only returns 5 results — use /filter?keyword= for full results
            var html = await _http.GetStringAsync($"/filter?keyword={Uri.EscapeDataString(query)}", ct);
            return ParseFilterPage(html);
        }

        public async Task<List<Anime>> GetPopularAsync(CancellationToken ct = default)
            => await GetLatestEpisodesAsync("updated", ct);

        public async Task<List<Anime>> GetRecentlyAddedAsync(CancellationToken ct = default)
            => await GetPageCardsAsync("/updated", ct);

        public async Task<Anime?> GetFeaturedAsync(CancellationToken ct = default) => null;

        // ── Extended endpoints ───────────────────────────────────────────────

        public async Task<List<Anime>> GetSpotlightAsync(CancellationToken ct = default)
        {
            var html = await _http.GetStringAsync("/home", ct);
            return ParseSpotlightSlides(html);
        }

        /// tab: "updated" | "subbed" | "dubbed" | "china" | "trending" | "random"
        public async Task<List<Anime>> GetLatestEpisodesAsync(string tab = "updated", CancellationToken ct = default)
        {
            var html = await _http.GetStringAsync("/home", ct);
            return ParseLatestEpisodeCards(html);
        }

        /// period: "today" | "week" | "month"
        public async Task<List<Anime>> GetTopAnimeAsync(string period = "today", CancellationToken ct = default)
        {
            var url = period switch
            {
                "week"  => "/top-anime?period=week",
                "month" => "/top-anime?period=month",
                _       => "/top-anime"
            };
            try
            {
                var html = await _http.GetStringAsync(url, ct);
                return ParseFilterPage(html);
            }
            catch { return new List<Anime>(); }
        }

        public async Task<List<Anime>> GetNewReleaseAsync(CancellationToken ct = default)
            => await GetPageCardsAsync("/newest", ct);

        public async Task<List<Anime>> GetNewAddedAsync(CancellationToken ct = default)
            => await GetPageCardsAsync("/added", ct);

        public async Task<List<Anime>> GetJustCompletedAsync(CancellationToken ct = default)
        {
            try
            {
                var html = await _http.GetStringAsync("/filter?status=completed&sort=recently_updated", ct);
                return ParseFilterPage(html);
            }
            catch { return new List<Anime>(); }
        }

        public async Task<List<Anime>> GetAZListAsync(string letter = "A", CancellationToken ct = default)
        {
            try
            {
                var html = await _http.GetStringAsync($"/az-list/{letter.ToLower()}", ct);
                return ParseFilterPage(html);
            }
            catch { return new List<Anime>(); }
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private async Task<List<Anime>> GetPageCardsAsync(string path, CancellationToken ct)
        {
            try
            {
                var html = await _http.GetStringAsync(path, ct);
                return ParseFilterPage(html);
            }
            catch { return new List<Anime>(); }
        }

        // ── Parsers ──────────────────────────────────────────────────────────

        /// Parses full-page filter/AZ/search results: <a class="item" href="/watch/slug">
        /// Meta structure: span.dot.score (rating), span.dot (type), span.dot (year)
        public static List<Anime> ParseFilterPage(string html)
        {
            var doc = new HtmlDoc();
            doc.LoadHtml(html);
            var results = new List<Anime>();

            foreach (var card in doc.DocumentNode.SelectNodes("//a[contains(@class,'item') and @href]") 
                                 ?? new HtmlAgilityPack.HtmlNodeCollection(null))
            {
                var href = card.GetAttributeValue("href", "");
                if (!href.Contains("/watch/")) continue;
                var slug = href.TrimStart('/').Replace("watch/", "");

                var img = card.SelectSingleNode(".//img");
                var poster = img?.GetAttributeValue("src", "") ?? img?.GetAttributeValue("data-src", "") ?? "";
                if (!string.IsNullOrEmpty(poster) && poster.StartsWith("/"))
                    poster = "https://aniwaves.ru" + poster;

                var titleNode = card.SelectSingleNode(".//*[contains(@class,'name')]");
                var title = titleNode?.GetAttributeValue("data-jp", "");
                if (string.IsNullOrWhiteSpace(title))
                    title = HtmlEntity.DeEntitize(titleNode?.InnerText.Trim() ?? slug);

                // Meta spans: [0]=rating (score fw-bold), [1]=type, [2]=year
                var metaSpans = card.SelectNodes(".//div[contains(@class,'meta')]/span");
                var ratingSpan = card.SelectSingleNode(".//span[contains(@class,'score')]");
                var ratingRaw = ratingSpan?.InnerText.Trim() ?? (metaSpans?.Count > 0 ? metaSpans[0].InnerText.Trim() : "0");
                double.TryParse(new string(ratingRaw.Where(c => char.IsDigit(c) || c == '.').ToArray()), out var rating);

                var typeText = metaSpans?.Count > 1 ? metaSpans[1].InnerText.Trim() : "";
                var dateText = metaSpans?.Count > 2 ? metaSpans[2].InnerText.Trim() : "";
                int.TryParse(new string(dateText.Where(char.IsDigit).Take(4).ToArray()), out var year);

                results.Add(new Anime
                {
                    Id = slug,
                    Slug = slug,
                    Title = title ?? slug,
                    PosterUrl = poster,
                    Rating = rating,
                    Year = year,
                    AnimeType = typeText,
                    HasSub = true,
                    IsPopular = true
                });
            }
            return results;
        }

        /// Legacy — kept for spotlight/home cards that use a different structure
        public static List<Anime> ParseSearchCards(string html) => ParseFilterPage(html);

        /// Latest episode cards from /home section#recent-update
        /// Structure: div.item > div.inner > div.ani.poster > a > img + div.meta (sub/total/type)
        ///                                 > div.info > a.name + div.genre + div.meta
        public static List<Anime> ParseLatestEpisodeCards(string html)
        {
            var doc = new HtmlDoc();
            doc.LoadHtml(html);
            var results = new List<Anime>();

            var items = doc.DocumentNode.SelectNodes(
                "//section[@id='recent-update']//div[contains(@class,'item')] | //div[contains(@class,'ani') and contains(@class,'items')]//div[contains(@class,'item')]")
                ?? new HtmlAgilityPack.HtmlNodeCollection(null);

            foreach (var item in items)
            {
                var a = item.SelectSingleNode(".//div[contains(@class,'poster')]//a[@href]");
                if (a == null) continue;

                var href = a.GetAttributeValue("href", "");
                var slug = href.TrimStart('/').Replace("watch/", "");

                var img = a.SelectSingleNode(".//img");
                var poster = img?.GetAttributeValue("src", "") ?? img?.GetAttributeValue("data-src", "") ?? "";
                if (!string.IsNullOrEmpty(poster) && poster.StartsWith("/"))
                    poster = "https://aniwaves.ru" + poster;

                var nameNode = item.SelectSingleNode(".//*[contains(@class,'name')]");
                var title = nameNode?.GetAttributeValue("data-jp", "");
                if (string.IsNullOrWhiteSpace(title))
                    title = HtmlEntity.DeEntitize(nameNode?.InnerText.Trim() ?? slug);

                // Sub/total from poster meta: span.ep-status.sub > span, span.ep-status.total > span
                var subSpan  = item.SelectSingleNode(".//*[contains(@class,'ep-status') and contains(@class,'sub')]/span");
                var totalSpan = item.SelectSingleNode(".//*[contains(@class,'ep-status') and contains(@class,'total')]/span");
                var dubSpan  = item.SelectSingleNode(".//*[contains(@class,'ep-status') and contains(@class,'dub')]/span");
                int.TryParse(subSpan?.InnerText.Trim(), out var subCount);
                int.TryParse(totalSpan?.InnerText.Trim(), out var total);
                int.TryParse(dubSpan?.InnerText.Trim(), out var dubCount);

                // Type from right div inside poster meta
                var typeDiv = item.SelectSingleNode(".//*[contains(@class,'meta')]//*[contains(@class,'right')]");
                var animeType = typeDiv?.InnerText.Trim() ?? "";

                // Genres
                var genreNodes = item.SelectNodes(".//div[contains(@class,'genre')]//a");
                var genres = genreNodes?.Select(n => HtmlEntity.DeEntitize(n.InnerText.Trim()))
                                        .Where(g => !string.IsNullOrWhiteSpace(g)).ToList()
                          ?? new List<string>();

                results.Add(new Anime
                {
                    Id = slug,
                    Slug = slug,
                    Title = title ?? slug,
                    PosterUrl = poster,
                    SubCount = subCount,
                    DubCount = dubCount,
                    TotalEpisodes = total > 0 ? total : subCount,
                    AnimeType = animeType,
                    Genres = genres,
                    HasSub = subCount > 0,
                    HasDub = dubCount > 0,
                    Quality = "HD",
                    IsPopular = true,
                    IsRecentlyAdded = true
                });
            }
            return results;
        }

        /// Spotlight swiper slides from #hotest
        public static List<Anime> ParseSpotlightSlides(string html)
        {
            var doc = new HtmlDoc();
            doc.LoadHtml(html);
            var results = new List<Anime>();

            var slides = doc.DocumentNode.SelectNodes(
                "//div[@id='hotest']//div[contains(@class,'swiper-slide') and contains(@class,'item')]")
                ?? new HtmlAgilityPack.HtmlNodeCollection(null);

            foreach (var slide in slides)
            {
                var playLink = slide.SelectSingleNode(".//a[contains(@class,'play')]");
                var href = playLink?.GetAttributeValue("href", "") ?? "";
                var slug = href.TrimStart('/').Replace("watch/", "");
                var id = ExtractNumericId(slug);

                var titleNode = slide.SelectSingleNode(".//*[contains(@class,'title')]");
                var title = titleNode?.GetAttributeValue("data-jp", "");
                if (string.IsNullOrWhiteSpace(title))
                    title = HtmlEntity.DeEntitize(titleNode?.InnerText.Trim() ?? "");

                var synopsis = slide.SelectSingleNode(".//*[contains(@class,'synopsis')]")?.InnerText.Trim() ?? "";

                var imgDiv = slide.SelectSingleNode(".//div[@class='image']//div[@style]") 
                           ?? slide.SelectSingleNode(".//*[@style[contains(.,'url(')]]");
                var bgUrl = ExtractUrlFromStyle(imgDiv?.GetAttributeValue("style", "") ?? "");
                if (!string.IsNullOrEmpty(bgUrl) && bgUrl.StartsWith("/"))
                    bgUrl = "https://aniwaves.ru" + bgUrl;

                // Try to get a separate poster image
                var posterImg = slide.SelectSingleNode(".//img[@src]");
                var posterUrl = posterImg?.GetAttributeValue("src", "") ?? bgUrl;
                if (!string.IsNullOrEmpty(posterUrl) && posterUrl.StartsWith("/"))
                    posterUrl = "https://aniwaves.ru" + posterUrl;

                var ageRating = slide.SelectSingleNode(".//*[contains(@class,'rating')]")?.InnerText.Trim() ?? "";
                var hasSub = slide.InnerHtml.Contains("fa-closed-captioning");
                var quality = slide.InnerHtml.Contains("class=\"quality\"") ? "HD" : "";

                results.Add(new Anime
                {
                    Id = slug,
                    Slug = slug,
                    Title = title ?? "",
                    Description = HtmlEntity.DeEntitize(synopsis),
                    BackgroundUrl = bgUrl,
                    PosterUrl = posterUrl,
                    AgeRating = ageRating,
                    Quality = quality,
                    HasSub = hasSub,
                    IsFeatured = true,
                    IsPopular = true
                });
            }
            return results;
        }

        // ── Static helpers ───────────────────────────────────────────────────

        public static string ExtractNumericId(string slug)
        {
            var parts = slug.Split('-');
            if (parts.Length > 0 && int.TryParse(parts[^1], out _))
                return parts[^1];
            return new string(slug.Where(char.IsDigit).ToArray());
        }

        private static string ExtractUrlFromStyle(string style)
        {
            // Handle url('...'), url("..."), url(...)
            var idx = style.IndexOf("url(", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return "";
            var start = idx + 4;
            char quote = start < style.Length ? style[start] : '\0';
            if (quote == '\'' || quote == '"') start++;
            else quote = '\0';

            int end;
            if (quote != '\0')
                end = style.IndexOf(quote, start);
            else
                end = style.IndexOf(')', start);

            return end < 0 ? "" : style.Substring(start, end - start).Trim();
        }
    }
}
