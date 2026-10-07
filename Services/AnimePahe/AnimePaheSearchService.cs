using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NimeVault.Models;
using NimeVault.Services.Interfaces;

namespace NimeVault.Services.AnimePahe
{
    public class AnimePaheSearchService : IAnimeSearchService
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
        private async Task<List<Anime>> GetAiringAsync(int page, CancellationToken ct)
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
                        HasSub = true
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
    }
}
