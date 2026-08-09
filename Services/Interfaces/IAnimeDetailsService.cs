using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NimeVault.Models;

namespace NimeVault.Services.Interfaces
{
    public interface IAnimeDetailsService
    {
        Task<Anime?> GetDetailsAsync(string animeId, CancellationToken cancellationToken = default);
        Task<List<Episode>> GetEpisodesAsync(string animeId, CancellationToken cancellationToken = default);
    }
}
