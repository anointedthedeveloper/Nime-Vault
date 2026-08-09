using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NimeVault.Models;
using NimeVault.Services.Interfaces;

namespace NimeVault.Services.Mock
{
    public class MockAnimeSearchService : IAnimeSearchService
    {
        public async Task<List<Anime>> SearchAsync(string query, CancellationToken cancellationToken = default)
        {
            // Simulate network delay
            await Task.Delay(800, cancellationToken);

            if (string.IsNullOrWhiteSpace(query))
                return new List<Anime>();

            var lower = query.ToLowerInvariant();
            return MockData.Animes
                .Where(a =>
                    a.Title.Contains(lower, StringComparison.OrdinalIgnoreCase) ||
                    a.AlternativeTitle.Contains(lower, StringComparison.OrdinalIgnoreCase) ||
                    a.Genres.Any(g => g.Contains(lower, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        public async Task<List<Anime>> GetPopularAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(300, cancellationToken);
            return MockData.Animes.Where(a => a.IsPopular).ToList();
        }

        public async Task<List<Anime>> GetRecentlyAddedAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(300, cancellationToken);
            return MockData.Animes.Where(a => a.IsRecentlyAdded).ToList();
        }

        public async Task<Anime?> GetFeaturedAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(200, cancellationToken);
            return MockData.Animes.FirstOrDefault(a => a.IsFeatured);
        }
    }
}
