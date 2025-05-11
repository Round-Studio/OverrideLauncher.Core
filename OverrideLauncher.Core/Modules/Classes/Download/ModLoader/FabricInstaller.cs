using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.IO;
using Newtonsoft.Json;
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
                return JsonConvert.DeserializeObject<List<FabricLoaderVersion>>(content);
            }
        }



        private FabricInstallInfo _installInfo;
        public async Task InstallFabricAsync(FabricInstallInfo InstallInfo)
        {
            _installInfo = InstallInfo;
            _installInfo.FabricVersion = GetBestIntermediaryVersion(InstallInfo.FabricVersion.Intermediary.Version,
                InstallInfo.FabricVersion.Loader.Version).Result;
            // 获取最佳中间版本
            string intermediaryVersion = InstallInfo.FabricVersion.Loader.Version;
            if (string.IsNullOrEmpty(intermediaryVersion))
            {
                Console.WriteLine("Failed to get intermediary version.");
                return;
            }

            // 获取启动器元数据
            JObject launcherMeta = await GetLauncherMeta(new VersionParse(InstallInfo.GameInfo).GameJson.Id,
                InstallInfo.FabricVersion.Loader.Version);
            if (launcherMeta == null)
            {
                Console.WriteLine("Failed to get launcher metadata.");
                return;
            }

            // 下载必要的库
            await DownloadLibraries(launcherMeta);

            // 下载Fabric加载器
            await DownloadFabricLoader(InstallInfo.FabricVersion.Loader);

            // 下载中间版本
            await DownloadIntermediary(InstallInfo.FabricVersion.Intermediary);

            var entry = new VersionParse(_installInfo.GameInfo).GameJson;
            JArray libraries = launcherMeta["libraries"]["common"] as JArray;
            foreach (JObject library in libraries)
            {
                string url = library["url"].ToString();
                string name = library["name"].ToString();
                
                entry.Libraries.Add(new Library()
                {
                    Url = url,
                    Name = name
                });
            }
            
            var mainClass = _installInfo.FabricVersion.LauncherMeta.MainClass;
            if (mainClass is string)
            {
                entry.MainClass = mainClass.ToString();
            }
            else
            {
                var mainClassObj = mainClass as JObject;
                var client = mainClassObj["client"]?.ToString();
                entry.MainClass = client;
            }
            entry.Libraries.Add(new Library()
            {
                Name = _installInfo.FabricVersion.Intermediary.Maven,
                Url = $"https://maven.fabricmc.net/"
            });
            entry.Libraries.Add(new Library()
            {
                Name = _installInfo.FabricVersion.Loader.Maven,
                Url = $"https://maven.fabricmc.net/"
            });
            
            string jsonString = JsonConvert.SerializeObject(entry);
            File.WriteAllText(
                Path.Combine(_installInfo.GameInfo.GameCatalog, "versions", _installInfo.GameInfo.GameName,
                    $"{_installInfo.GameInfo.GameName}.json"), jsonString);
            
            Console.WriteLine("Installation completed successfully.");
        }

        // 获取最佳中间版本
        private static async Task<FabricLoaderVersion> GetBestIntermediaryVersion(string gameVersion, string loaderVersion)
        {
            string url = $"https://meta.fabricmc.net/v2/versions/loader/{gameVersion}/{loaderVersion}";
            HttpResponseMessage response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            string responseBody = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<FabricLoaderVersion>(responseBody);
        }

        // 获取启动器元数据
        private async Task<JObject> GetLauncherMeta(string gameVersion, string loaderVersion)
        {
            string url = $"https://meta.fabricmc.net/v2/versions/loader/{gameVersion}/{loaderVersion}";
            HttpResponseMessage response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            string responseBody = await response.Content.ReadAsStringAsync();
            JObject responseJson = JObject.Parse(responseBody);
            return responseJson["launcherMeta"] as JObject;
        }

        // 下载必要的库
        private async Task DownloadLibraries(JObject launcherMeta)
        {
            JArray libraries = launcherMeta["libraries"]["common"] as JArray;
            foreach (JObject library in libraries)
            {
                string url = library["url"].ToString();
                string name = library["name"].ToString();

                Console.WriteLine($"Downloading library: {name}");
                await DownloadFile(url,
                    Path.Combine(_installInfo.GameInfo.GameCatalog,"libraries", FileHelper.GetJarFilePath(name)));
            }
        }

        // 下载Fabric加载器
        private async Task DownloadFabricLoader(FabricLoaderVersionInfo loaderVersion)
        {
            string url = $"https://maven.fabricmc.net/net/fabricmc/fabric-loader/{loaderVersion.Version}/fabric-loader-{loaderVersion.Version}.jar";
            Console.WriteLine($"Downloading Fabric Loader: {loaderVersion.Maven}");
            await DownloadFile(url,
                Path.Combine(_installInfo.GameInfo.GameCatalog, "libraries",FileHelper.GetJarFilePath(loaderVersion.Maven)));
        }
        
        private async Task DownloadIntermediary(FabricIntermediaryVersion loaderVersion)
        {
            string url =
                $"https://maven.fabricmc.net/net/fabricmc/intermediary/{loaderVersion.Version}/intermediary-{loaderVersion.Version}.jar";
            Console.WriteLine($"Downloading Fabric Loader: {loaderVersion.Maven}");
            await DownloadFile(url,
                Path.Combine(_installInfo.GameInfo.GameCatalog, "libraries",FileHelper.GetJarFilePath(loaderVersion.Maven)));
        }

        // 下载文件
        private async Task DownloadFile(string url, string fileName)
        {
            HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to download {fileName}");
                return;
            }

            // 确保目标目录存在
            string directory = Path.GetDirectoryName(fileName);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 下载文件并保存到指定路径
            using (FileStream fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await response.Content.CopyToAsync(fileStream);
            }

            Console.WriteLine($"Downloaded {fileName} to {fileName}");
        }
    }
}