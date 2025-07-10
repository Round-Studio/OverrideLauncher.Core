using System.Text.Json;
using OverrideLauncher.Core.Base.Dictionary;
using OverrideLauncher.Core.Base.Entry.Download.Install.Manifest;

namespace OverrideLauncher.Core.Classes.Install.Manifest;

public class InstallHelper
{
    public static async Task<ManifestMojang.ManifestVersion> TryingFindVersion(string id)
    {
        var versionManifest = await GetVersionManifest();
        if (versionManifest == null)
        {
            return null;
        }

        var version = versionManifest.Versions.FirstOrDefault(v => v.Id == id);
        if (version == null)
        {
            return null;
        }

        return new ManifestMojang.ManifestVersion { Id = version.Id, Url = version.Url };
    }
    public static async Task<ManifestMojang> GetVersionManifest()
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
            
        string url = DictionaryDownloadHost.ManifestHost; // ManifestHost
        try
        {
            // 获取JSON内容
            string jsonContent = await GetJsonContentAsync(url);

            // 反序列化为VersionManifest对象
            ManifestMojang versionManifest = JsonSerializer.Deserialize<ManifestMojang>(jsonContent);
            return versionManifest;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public static async Task<ManifestClientJson> TryingGetClientJson(ManifestMojang.ManifestVersion version)
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

        string url = version.Url;
        try
        {
            // 获取JSON内容
            string jsonContent = await GetJsonContentAsync(url);

            // 反序列化为ManifestClientJson对象
            ManifestClientJson clientJson = JsonSerializer.Deserialize<ManifestClientJson>(jsonContent);
            return clientJson;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public static async Task<ManifestClientAssetsJson> TryingGetClientAssetsJson(ManifestClientJson version)
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

        string url = version.AssetIndex.Url;
        try
        {
            // 获取JSON内容
            string jsonContent = await GetJsonContentAsync(url);

            // 反序列化为ManifestClientAssetsJson对象
            ManifestClientAssetsJson manifestClientAssetsJson = JsonSerializer.Deserialize<ManifestClientAssetsJson>(jsonContent);
            return manifestClientAssetsJson;
        }
        catch (Exception ex)
        {
            return null;
        }
    }
}