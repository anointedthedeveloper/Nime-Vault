using System.Threading.Tasks;
using NimeVault.Models;

namespace NimeVault.Services.Interfaces
{
    public interface ISettingsService
    {
        AppSettings Settings { get; }
        Task SaveAsync();
        Task LoadAsync();
    }
}
