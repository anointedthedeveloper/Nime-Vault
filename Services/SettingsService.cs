using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using NimeVault.Models;
using NimeVault.Services.Interfaces;

namespace NimeVault.Services
{
    public class SettingsService : ISettingsService
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NimeVault", "settings.json");

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public AppSettings Settings { get; private set; } = new AppSettings();

        public async Task SaveAsync()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                var json = JsonSerializer.Serialize(Settings, JsonOpts);
                await File.WriteAllTextAsync(SettingsPath, json);
            }
            catch { /* silently ignore — settings save is non-critical */ }
        }

        public async Task LoadAsync()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return;
                var json = await File.ReadAllTextAsync(SettingsPath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOpts);
                if (loaded != null) Settings = loaded;
            }
            catch
            {
                Settings = new AppSettings();
            }
        }
    }
}
