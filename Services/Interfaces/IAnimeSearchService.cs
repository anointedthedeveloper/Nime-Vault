using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NimeVault.Models;

namespace NimeVault.Services.Interfaces
{
    public interface IAnimeSearchService
    {
        Task<List<Anime>> SearchAsync(string query, CancellationToken cancellationToken = default);
        Task<List<Anime>> GetPopularAsync(CancellationToken cancellationToken = default);
        Task<List<Anime>> GetRecentlyAddedAsync(CancellationToken cancellationToken = default);
        Task<Anime?> GetFeaturedAsync(CancellationToken cancellationToken = default);
    }
}
