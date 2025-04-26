using System.Text.Json;
using OverrideLauncher.Core.Modules.Entry.DownloadEntry;

namespace OverrideLauncher.Core.Modules.Classes.Download;

public class InstallGame
{
    private static HttpClient _httpClient = new HttpClient();

    public static async Task<DownloadVersionEntry> LoadVersionManifestAsync()
    {
        _httpClient = new HttpClient();
        string json = await _httpClient.GetStringAsync("https://piston-meta.mojang.com/mc/game/version_manifest.json");
            
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        return JsonSerializer.Deserialize<DownloadVersionEntry>(json, options);
    }
    
    public InstallGame()
    {
        
    }
}