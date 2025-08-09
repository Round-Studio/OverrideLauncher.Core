using System.Net;
using System.Text.Json;
using System.Xml.Serialization;
using OverrideLauncher.Core.Base.Dictionary;
using OverrideLauncher.Core.Base.Entry.Download.Install.Client;
using OverrideLauncher.Core.Base.Entry.Download.Install.Fabric;
using OverrideLauncher.Core.Base.Entry.Download.Install.Forge;
using OverrideLauncher.Core.Base.Entry.Download.Install.LiteLoader;
using OverrideLauncher.Core.Base.Entry.Download.Install.Manifest;

namespace OverrideLauncher.Core.Classes.Install.Manifest;

public class InstallHelper
{
    private static List<FabricApiEntry.FabricApiVersion> fabricApiVersions;
    private static ForgeMetaDataEntry.ForgeMetaDataRoot forgeMetaDataRoot;

    public static bool IsThisSystemFile(string path)
    {
        var isthisSystem = true;
        
        GetOtherSystem().ForEach(x =>
        {
            if (path.Contains(x))
            {
                isthisSystem = false;
                return;
            }
        });
        
        return isthisSystem;
    }
    public static  List<string> GetOtherSystem()
    {
        var res = new List<string>();
        
        if (OperatingSystem.IsWindows())
        {
            res.Add("linux");
            res.Add("osx");
            res.Add("macos");
        }
        else if (OperatingSystem.IsLinux())
        {
            res.Add("windows");
            res.Add("osx");
            res.Add("macos");
        }
        else if (OperatingSystem.IsMacOS())
        {
            res.Add("windows");
            res.Add("linux");
        }

        return res;
    }
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
            
        string url = DictionaryDownloadHost.Sources.ManifestHost; // ManifestHost
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
    public static async Task<List<FabricLoaderVersion>> GetVersionFabricManifest(string id)
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
            
        string url = $"{DictionaryDownloadHost.Sources.FabricHost}/versions/loader/{id}"; // FabricMan
        // 获取JSON内容
        string jsonContent = await GetJsonContentAsync(url);

        // 反序列化为FabricProfile对象
        List<FabricLoaderVersion> fabricProfile = JsonSerializer.Deserialize<List<FabricLoaderVersion>>(jsonContent);
        return fabricProfile;
    }
    public static ManifestClientJson? GetClientJsonEntry(ClientRootInfo rootInfo)
    {
        if (rootInfo == null) throw new NullReferenceException();
        
        var path = Path.Combine(rootInfo.ClientRootPath,
            DictionaryGameRoot.VersionsPath, rootInfo.ClientName, $"{rootInfo.ClientName}.json");
        
        if (!File.Exists(path)) throw new FileNotFoundException($"未找到 {rootInfo.ClientName}.json");
        if (string.IsNullOrEmpty(File.ReadAllText(path))) throw new NullReferenceException();
        
        return JsonSerializer.Deserialize<ManifestClientJson>(File.ReadAllText(path));
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
    public static void SaveClientJson(ManifestClientJson json, ClientRootInfo rootInfo)
    {
        if (rootInfo == null) throw new NullReferenceException();
        
        var path = Path.Combine(rootInfo.ClientRootPath,
            DictionaryGameRoot.VersionsPath, rootInfo.ClientName, $"{rootInfo.ClientName}.json");
        
        File.WriteAllText(path, JsonSerializer.Serialize(json));
    }
    public static async Task<List<FabricApiEntry.FabricApiVersion>> GetFabricApiVersionsManifest()
    {
        if (fabricApiVersions != null) return fabricApiVersions;
        
        HttpClient client = new HttpClient();
        string projectId = "P7dR8mSH";
        string apiUrl = $"https://api.modrinth.com/v2/project/{projectId}/version";
            
        // 设置User-Agent头(Modrinth API要求)
        client.DefaultRequestHeaders.Add("User-Agent", "ModrinthApp");
            
        var response = await client.GetAsync(apiUrl);
        response.EnsureSuccessStatusCode();
            
        var responseBody = await response.Content.ReadAsStringAsync();
            
        // 反序列化JSON到Version对象列表
        fabricApiVersions = JsonSerializer.Deserialize<List<FabricApiEntry.FabricApiVersion>>(responseBody);
        return fabricApiVersions;
    }
    public static async Task<List<FabricApiEntry.FabricApiVersion>> TryGetFabricApiVersions(string id)
    {
        var lst = await GetFabricApiVersionsManifest();
        var res = new List<FabricApiEntry.FabricApiVersion>();
        lst.ForEach(x =>
        {
            if (x.GameVersions.Contains(id)) res.Add(x);
        });
        return res;
    }
    public static string ConvertToMavenPath(string mavenCoordinate, string extension = "jar")
    {
        // 使用 Span 和范围语法处理分割
        ReadOnlySpan<char> coordinateSpan = mavenCoordinate.AsSpan();
        int firstColon = coordinateSpan.IndexOf(':');
        int lastColon = coordinateSpan.LastIndexOf(':');

        // 提取各部分
        var groupId = coordinateSpan[..firstColon].ToString();
        var artifactId = coordinateSpan[(firstColon + 1)..lastColon].ToString();
        var version = coordinateSpan[(lastColon + 1)..].ToString();

        string groupPath = groupId.Replace('.', '/');
        string fileName = $"{artifactId}-{version}.{extension}";
        return $"{groupPath}/{artifactId}/{version}/{fileName}";
    }
    public static async Task<ForgeMetaDataEntry.ForgeMetaDataRoot> TryGetForgeManifest()
    {
        if (forgeMetaDataRoot != null) return forgeMetaDataRoot;
    
        using (var httpClient = new HttpClient())
        {
            string xmlContent = await httpClient.GetStringAsync(DictionaryDownloadHost.Sources.ForgeManufestHost);
        
            var serializer = new XmlSerializer(typeof(ForgeMetaDataEntry.ForgeMetaDataRoot));
            using (var reader = new StringReader(xmlContent))
            {
                forgeMetaDataRoot = (ForgeMetaDataEntry.ForgeMetaDataRoot)serializer.Deserialize(reader);
                return forgeMetaDataRoot;
            }
        }
    }
    public static async Task<List<string>> TryGetInstallForgeMeta(string id)
    {
        var ent = await TryGetForgeManifest();
        var res = new List<string>();
        ent.Versioning.Versions.Version.ForEach(x =>
        {
            if (x.Split('-')[0] == id) res.Add(x);
        });

        return res;
    }
    public static async Task<LiteLoaderManifest> TryGetLiteLoaderManifest()
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

        string url = DictionaryDownloadHost.Sources.LiteLoaderManufestHost;
        try
        {
            // 获取JSON内容
            string jsonContent = await GetJsonContentAsync(url);

            // 反序列化为ManifestClientAssetsJson对象
            LiteLoaderManifest liteLoaderManifest = JsonSerializer.Deserialize<LiteLoaderManifest>(jsonContent);
            return liteLoaderManifest;
        }
        catch (Exception ex)
        {
            return null;
        }
    }
    public static async Task<List<LiteLoaderManifest.VersionData>> TryGetVersionLiteLoaderVersions(string id)
    {
        var lst = await TryGetLiteLoaderManifest();
        var res = new List<LiteLoaderManifest.VersionData>();

        foreach (var keyValuePair in lst.Versions)
        {
            if (keyValuePair.Key == id)
            {
                res.Add(keyValuePair.Value);
            }
        }

        if (res.Count == 0) return null;
        
        return res;
    }
}