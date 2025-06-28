

using System.Text.Json;

namespace OverrideLauncher.Core.Modules.Classes.Download.ModLoader;

public class QuiltInstaller
{
    public const string BaseUrl = "https://meta.fabricmc.net/v2/";
    private static readonly HttpClient client = new HttpClient();
    public static async Task<List<FabricLoaderVersion>> GetLoaderVersionsAsync(string minecraftVersion)
    {
        using (var httpClient = new HttpClient())
        {
            httpClient.BaseAddress = new Uri(BaseUrl);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "C# FabricMC API Client");
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var response = await httpClient.GetAsync($"versions/loader/{minecraftVersion}");
            
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to get loader versions: {response.StatusCode}");
            }
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<FabricLoaderVersion>>(content);
        }
    }
}