using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NimeVault.Models;
using NimeVault.Services.Interfaces;

namespace NimeVault.Services.Mock
{
    public class MockAnimeDetailsService : IAnimeDetailsService
    {
        public async Task<Anime?> GetDetailsAsync(string animeId, CancellationToken cancellationToken = default)
        {
            await Task.Delay(400, cancellationToken);
            return MockData.Animes.FirstOrDefault(a => a.Id == animeId);
        }

        public async Task<List<Episode>> GetEpisodesAsync(string animeId, CancellationToken cancellationToken = default)
        {
            await Task.Delay(500, cancellationToken);
            var anime = MockData.Animes.FirstOrDefault(a => a.Id == animeId);
            int count = anime?.TotalEpisodes ?? 12;
            // Cap at 24 for UI performance in mock
            count = System.Math.Min(count, 24);
            return MockData.GetEpisodes(animeId, count);
        }
    }
}
