using System.Threading;
using System.Threading.Tasks;

namespace NimeVault.Services.Interfaces
{
    public interface IStreamService
    {
        /// <summary>
        /// Resolves the direct .m3u8 HLS stream URL for a given anime episode.
        /// Uses Playwright headless browser to bypass VRF token protection.
        /// </summary>
        Task<string?> ResolveStreamUrlAsync(string animeId, int episodeNumber, string language = "sub", CancellationToken ct = default);
    }
}
