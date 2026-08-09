using System.Threading.Tasks;
using NimeVault.Models;
using NimeVault.Services.Interfaces;

namespace NimeVault.Services
{
    public class SettingsService : ISettingsService
    {
        public AppSettings Settings { get; private set; } = new AppSettings();

        public Task SaveAsync()
        {
            // In a real implementation, serialize to JSON and write to disk
            return Task.CompletedTask;
        }

        public Task LoadAsync()
        {
            // In a real implementation, deserialize from JSON file
            return Task.CompletedTask;
        }
    }
}
