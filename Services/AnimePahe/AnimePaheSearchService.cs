using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NimeVault.Models;
using NimeVault.Services.Interfaces;

namespace NimeVault.Services.AnimePahe
{
    public class AnimePaheSearchService : IAnimeSearchService, IBrowseProvider
    {
        private readonly AnimePaheClient _client;
        private readonly IAnimeDetailsService _details;

        public AnimePaheSearchService(AnimePaheClient client, IAnimeDetailsService details)
        {
            _client = client;
            _details = details;
        }

        public async Task<List<Anime>> SearchAsync(string query, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<Anime>();

            using var doc = await _client.GetApiAsync("m=search&q=" + Uri.EscapeDataString(query.Trim()), cancellationToken);
            var results = new List<Anime>();
            if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
                return results;

            foreach (var item in data.EnumerateArray())
            {
                var session = AnimePaheClient.Str(item, "session");
                if (session.Length == 0) continue;

                var anime = new Anime
                {
                    Id = session,
                    Title = AnimePaheClient.Str(item, "title"),
                    PosterUrl = AnimePaheClient.Str(item, "poster"),
                    BackgroundUrl = AnimePaheClient.Str(item, "poster"),
                    Year = AnimePaheClient.Int(item, "year"),
                    Status = AnimePaheClient.Str(item, "status"),
                    Rating = AnimePaheClient.Dbl(item, "score"),
                    TotalEpisodes = AnimePaheClient.Int(item, "episodes"),
                    HasSub = true,
                    HasDub = false
                };
                var type = AnimePaheClient.Str(item, "type");
                if (type.Length > 0) anime.Genres.Add(type);
                _client.AnimeCache[session] = anime;
                results.Add(anime);
            }
            return results;
        }

        // Currently-airing releases, newest first. One entry per anime.
        internal async Task<List<Anime>> GetAiringAsync(int page, CancellationToken ct)
        {
            using var doc = await _client.GetApiAsync($"m=airing&page={page}", ct);
            var list = new List<Anime>();
            if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
                return list;

            var seen = new HashSet<string>();
            foreach (var item in data.EnumerateArray())
            {
                var session = AnimePaheClient.Str(item, "anime_session");
                if (session.Length == 0 || !seen.Add(session)) continue;

                if (!_client.AnimeCache.TryGetValue(session, out var anime))
                {
                    var snapshot = AnimePaheClient.Str(item, "snapshot");
                    anime = new Anime
                    {
                        Id = session,
                        Title = AnimePaheClient.Str(item, "anime_title"),
                        PosterUrl = snapshot,
                        BackgroundUrl = snapshot,
                        Status = "Currently Airing",
                        TotalEpisodes = AnimePaheClient.Int(item, "episode"),
                        HasSub = !AnimePaheClient.Str(item, "audio").Equals("eng", StringComparison.OrdinalIgnoreCase),
                        HasDub = AnimePaheClient.Str(item, "audio").Equals("eng", StringComparison.OrdinalIgnoreCase)
                    };
                    _client.AnimeCache[session] = anime;
                }
                list.Add(anime);
            }

            await EnrichPostersAsync(list, ct);
            return list;
        }

        // Airing entries only carry a landscape screenshot; the search API returns the real poster.
        private async Task EnrichPostersAsync(List<Anime> animes, CancellationToken ct)
        {
            using var gate = new SemaphoreSlim(4);
            await Task.WhenAll(animes.Select(async a =>
            {
                if (a.PosterUrl.Contains("/posters/", StringComparison.OrdinalIgnoreCase)) return;
                await gate.WaitAsync(ct);
                try
                {
                    using var doc = await _client.GetApiAsync("m=search&q=" + Uri.EscapeDataString(a.Title), ct);
                    if (!doc.RootElement.TryGetProperty("data", out var data)) return;
                    foreach (var item in data.EnumerateArray())
                    {
                        if (AnimePaheClient.Str(item, "session") != a.Id) continue;
                        var poster = AnimePaheClient.Str(item, "poster");
                        if (poster.Length > 0) { a.PosterUrl = poster; a.BackgroundUrl = poster; }
                        a.Year = AnimePaheClient.Int(item, "year");
                        a.Rating = AnimePaheClient.Dbl(item, "score");
                        var eps = AnimePaheClient.Int(item, "episodes");
                        if (eps > 0) a.TotalEpisodes = eps;
                        break;
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch { /* keep the screenshot fallback */ }
                finally { gate.Release(); }
            }));
        }

        public async Task<List<Anime>> GetPopularAsync(CancellationToken cancellationToken = default)
        {
            // AnimePahe has no "popular" endpoint; show what is airing on the following pages.
            var list = await GetAiringAsync(2, cancellationToken);
            foreach (var a in list) a.IsPopular = true;
            return list;
        }

        public async Task<List<Anime>> GetRecentlyAddedAsync(CancellationToken cancellationToken = default)
        {
            var list = await GetAiringAsync(1, cancellationToken);
            foreach (var a in list) a.IsRecentlyAdded = true;
            return list;
        }

        public async Task<Anime?> GetFeaturedAsync(CancellationToken cancellationToken = default)
        {
            var recent = await GetAiringAsync(1, cancellationToken);
            var first = recent.FirstOrDefault();
            if (first == null) return null;

            var full = await _details.GetDetailsAsync(first.Id, cancellationToken) ?? first;
            full.IsFeatured = true;
            return full;
        }

        // ---- IBrowseProvider: AnimePahe has no dedicated endpoints, so these are built from the airing feed ----

        public async Task<List<Anime>> GetSpotlightAsync(CancellationToken ct = default)
        {
            var top = (await GetAiringAsync(1, ct)).Take(5).ToList();
            var full = await Task.WhenAll(top.Select(async a =>
            {
                try { return await _details.GetDetailsAsync(a.Id, ct) ?? a; }
                catch (OperationCanceledException) { throw; }
                catch { return a; }
            }));
            foreach (var a in full) a.IsFeatured = true;
            return full.ToList();
        }

        public async Task<List<Anime>> GetLatestEpisodesAsync(string tab = "updated", CancellationToken ct = default)
        {
            switch (tab)
            {
                case "subbed": return (await GetAiringAsync(1, ct)).Where(a => !a.HasDub).ToList();
                case "dubbed":
                    var both = (await GetAiringAsync(1, ct)).Concat(await GetAiringAsync(2, ct));
                    return both.Where(a => a.HasDub).DistinctBy(a => a.Id).ToList();
                case "trending": return await GetAiringAsync(2, ct);
                default: return await GetAiringAsync(1, ct);
            }
        }

        public Task<List<Anime>> GetTopAnimeAsync(string period = "today", CancellationToken ct = default)
            => GetAiringAsync(period == "month" ? 3 : period == "week" ? 2 : 1, ct);

        public Task<List<Anime>> GetNewReleaseAsync(CancellationToken ct = default) => GetAiringAsync(1, ct);
        public Task<List<Anime>> GetNewAddedAsync(CancellationToken ct = default) => GetAiringAsync(2, ct);
        public Task<List<Anime>> GetJustCompletedAsync(CancellationToken ct = default) => GetAiringAsync(3, ct);

        private List<(string Session, string Title)>? _index;

        public async Task<List<Anime>> GetAZListAsync(string letter = "A", CancellationToken ct = default)
        {
            // /anime lists every title; the markup is parsed loosely (any /anime/<uuid> link with a title).
            _index ??= await LoadIndexAsync(ct);
            bool Matches(string title)
            {
                var c = title.TrimStart().FirstOrDefault();
                return letter == "#" || letter == "0-9" ? !char.IsLetter(c)
                    : char.ToUpperInvariant(c) == char.ToUpperInvariant(letter[0]);
            }

            var list = _index.Where(i => Matches(i.Title)).Take(60).Select(i =>
                _client.AnimeCache.TryGetValue(i.Session, out var cached)
                    ? cached
                    : new Anime { Id = i.Session, Title = i.Title, HasSub = true }).ToList();
            foreach (var a in list) _client.AnimeCache.TryAdd(a.Id, a);
            await EnrichPostersAsync(list.Take(24).ToList(), ct);
            return list;
        }

        private async Task<List<(string, string)>> LoadIndexAsync(CancellationToken ct)
        {
            var html = await _client.GetStringAsync("/anime", ct);
            var seen = new HashSet<string>();
            var items = new List<(string, string)>();
            foreach (Match m in Regex.Matches(html,
                @"<a[^>]+href=""/anime/([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})""[^>]*>\s*([^<]+?)\s*</a>",
                RegexOptions.IgnoreCase))
            {
                var title = WebUtility.HtmlDecode(m.Groups[2].Value);
                if (title.Length > 0 && seen.Add(m.Groups[1].Value)) items.Add((m.Groups[1].Value, title));
            }
            return items.OrderBy(i => i.Item2, StringComparer.OrdinalIgnoreCase).ToList();
        }
    }
}
