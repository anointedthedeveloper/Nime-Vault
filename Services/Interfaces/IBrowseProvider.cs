using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NimeVault.Models;

namespace NimeVault.Services.Interfaces
{
    /// <summary>Extra listings used by the Home spotlight, Browse and A-Z screens.</summary>
    public interface IBrowseProvider
    {
        Task<List<Anime>> GetSpotlightAsync(CancellationToken ct = default);
        Task<List<Anime>> GetLatestEpisodesAsync(string tab = "updated", CancellationToken ct = default);
        Task<List<Anime>> GetTopAnimeAsync(string period = "today", CancellationToken ct = default);
        Task<List<Anime>> GetNewReleaseAsync(CancellationToken ct = default);
        Task<List<Anime>> GetNewAddedAsync(CancellationToken ct = default);
        Task<List<Anime>> GetJustCompletedAsync(CancellationToken ct = default);
        Task<List<Anime>> GetAZListAsync(string letter = "A", CancellationToken ct = default);
    }
}
