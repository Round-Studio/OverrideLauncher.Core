using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes; // .NET 6+ 特性
using System.Collections.Generic;
using System.Linq;
using OverrideLauncher.Core.Modules.Classes.Version;
using OverrideLauncher.Core.Modules.Entry.DownloadEntry.ModloaderEntry;

namespace OverrideLauncher.Core.Modules.Classes.Download.ModLoader
{
    public class FabricInstaller
    {
        public const string BaseUrl = "https://meta.fabricmc.net/v2/";
        private static readonly HttpClient client = new HttpClient();
        
        public static async Task<List<FabricLoaderVersion>> GetLoaderVersionsAsync(string minecraftVersion)
        {
            using var httpClient = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
            httpClient.DefaultRequestHeaders.Add("User-Agent", "C# FabricMC API Client");

            var response = await httpClient.GetAsync($"versions/loader/{minecraftVersion}");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<FabricLoaderVersion>>(content) ?? new List<FabricLoaderVersion>();
        }

        private FabricInstallInfo _installInfo;
        
        public async Task InstallFabricAsync(FabricInstallInfo installInfo)
        {
            _installInfo = installInfo;
            _installInfo.FabricVersion = await GetBestIntermediaryVersion(
                installInfo.FabricVersion.Intermediary.Version,
                installInfo.FabricVersion.Loader.Version);

            if (string.IsNullOrEmpty(_installInfo.FabricVersion.Loader.Version))
            {
                Console.WriteLine("Failed to get intermediary version.");
                return;
            }

            // 使用 JsonNode 替代 JObject
            var launcherMeta = await GetLauncherMeta(
                new VersionParse(_installInfo.GameInfo).GameJson.Id,
                _installInfo.FabricVersion.Loader.Version);

            if (launcherMeta == null)
            {
                Console.WriteLine("Failed to get launcher metadata.");
                return;
            }

            await DownloadLibraries(launcherMeta);
            await DownloadFabricLoader(_installInfo.FabricVersion.Loader);
            await DownloadIntermediary(_installInfo.FabricVersion.Intermediary);

            var entry = new VersionParse(_installInfo.GameInfo).GameJson;
            
            // 处理 libraries
            if (launcherMeta["libraries"]?["common"] is JsonArray libraries)
            {
                foreach (var library in libraries.OfType<JsonObject>())
                {
                    entry.Libraries.Add(new Library
                    {
                        Url = library["url"]?.GetValue<string>() ?? string.Empty,
                        Name = library["name"]?.GetValue<string>() ?? string.Empty
                    });
                }
            }

            // 处理 mainClass
            var mainClass = _installInfo.FabricVersion.LauncherMeta.MainClass;
            entry.MainClass = mainClass switch
            {
                string s => s,
                JsonObject obj => obj["client"]?.GetValue<string>(),
                _ => entry.MainClass
            };

            // 添加固定库
            entry.Libraries.AddRange(new[]
            {
                new Library { Name = _installInfo.FabricVersion.Intermediary.Maven, Url = "https://maven.fabricmc.net/" },
                new Library { Name = _installInfo.FabricVersion.Loader.Maven, Url = "https://maven.fabricmc.net/" }
            });

            // 序列化并保存
            var jsonString = JsonSerializer.Serialize(entry, new JsonSerializerOptions { WriteIndented = true });
            var path = Path.Combine(
                _installInfo.GameInfo.GameCatalog, 
                "versions", 
                _installInfo.GameInfo.GameName,
                $"{_installInfo.GameInfo.GameName}.json");
            
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            await File.WriteAllTextAsync(path, jsonString);
            
            Console.WriteLine("Installation completed successfully.");
        }

        private static async Task<FabricLoaderVersion> GetBestIntermediaryVersion(string gameVersion, string loaderVersion)
        {
            var response = await client.GetAsync($"versions/loader/{gameVersion}/{loaderVersion}");
            if (!response.IsSuccessStatusCode) return null;
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<FabricLoaderVersion>(content);
        }

        private async Task<JsonObject> GetLauncherMeta(string gameVersion, string loaderVersion)
        {
            var response = await client.GetAsync($"versions/loader/{gameVersion}/{loaderVersion}");
            if (!response.IsSuccessStatusCode) return null;
            
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonNode.Parse(content)?.AsObject();
            return json?["launcherMeta"]?.AsObject();
        }

        private async Task DownloadLibraries(JsonObject launcherMeta)
        {
            if (launcherMeta["libraries"]?["common"] is not JsonArray libraries) return;
            
            foreach (var library in libraries.OfType<JsonObject>())
            {
                var url = library["url"]?.GetValue<string>();
                var name = library["name"]?.GetValue<string>();
                if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(name)) continue;

                Console.WriteLine($"Downloading library: {name}");
                await DownloadFile(
                    url + FileHelper.GetJarFilePath(name),
                    Path.Combine(_installInfo.GameInfo.GameCatalog, "libraries", FileHelper.GetJarFilePath(name)));
            }
        }

        private async Task DownloadFabricLoader(FabricLoaderVersionInfo loaderVersion)
        {
            var url = $"https://maven.fabricmc.net/net/fabricmc/fabric-loader/{loaderVersion.Version}/fabric-loader-{loaderVersion.Version}.jar";
            Console.WriteLine($"Downloading Fabric Loader: {loaderVersion.Maven}");
            await DownloadFile(
                url,
                Path.Combine(_installInfo.GameInfo.GameCatalog, "libraries", FileHelper.GetJarFilePath(loaderVersion.Maven)));
        }
        
        private async Task DownloadIntermediary(FabricIntermediaryVersion loaderVersion)
        {
            var url = $"https://maven.fabricmc.net/net/fabricmc/intermediary/{loaderVersion.Version}/intermediary-{loaderVersion.Version}.jar";
            Console.WriteLine($"Downloading Intermediary: {loaderVersion.Maven}");
            await DownloadFile(
                url,
                Path.Combine(_installInfo.GameInfo.GameCatalog, "libraries", FileHelper.GetJarFilePath(loaderVersion.Maven)));
        }

        private async Task DownloadFile(string url, string fileName)
        {
            if (File.Exists(fileName)) return;
            
            Console.WriteLine(url);
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to download {fileName}");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            await using var fileStream = File.Create(fileName);
            await response.Content.CopyToAsync(fileStream);
            
            Console.WriteLine($"Downloaded {fileName}");
        }
    }
}