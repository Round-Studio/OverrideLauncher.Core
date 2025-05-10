using Newtonsoft.Json;
using OverrideLauncher.Core.Modules.Entry.DownloadEntry;

namespace OverrideLauncher.Core.Modules.Classes.Download;

public class DownloadVersionHelper
{
    public static async Task<GameVersion> TryingFindVersion(string ID)
    {
        var versionManifest = await GetVersionManifest();
        if (versionManifest == null)
        {
            return null;
        }

        var version = versionManifest.Versions.FirstOrDefault(v => v.Id == ID);
        if (version == null)
        {
            return null;
        }

        return new GameVersion { Id = version.Id, Url = version.Url };
    }
    public static async Task<VersionManifestEntry.VersionManifest> GetVersionManifest()
    {
        async Task<string> GetJsonContentAsync(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }
            
        string url = "https://piston-meta.mojang.com/mc/game/version_manifest.json";
        try
        {
            // 获取JSON内容
            string jsonContent = await GetJsonContentAsync(url);

            // 反序列化为VersionManifest对象
            VersionManifestEntry.VersionManifest versionManifest = JsonConvert.DeserializeObject<VersionManifestEntry.VersionManifest>(jsonContent);
            return versionManifest;
        }
        catch (Exception ex)
        {
            return null;
        }
    }
}