using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OverrideLauncher.Core.Modules.Entry.DownloadEntry;
using OverrideLauncher.Core.Modules.Enum.Download;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace OverrideLauncher.Core.Modules.Classes.Download
{
    public class InstallClient
    {
        private static HttpClient _httpClient = new HttpClient();
        public AssetsEntry.RootObject Assets { get; set; }
        public int DownloadThreadsCount { get; set; } = 64;
        public Action<DownloadStateEnum,string, double> ProgressCallback { get; set; }
        public DownloadVersionInfoEntry VersionInfo { get; private set; }
        private string ID = null;

        public InstallClient(GameVersion GameVersion,string InstallName = null)
        {
            VersionInfo = new DownloadVersionInfoEntry { Version = GameVersion,InstallName = InstallName};
            if (!string.IsNullOrEmpty(InstallName))
            {
                ID = InstallName;
            }
            else
            {
                ID = VersionInfo.Version.Id;
            }
        }

        public async Task<ulong> GetThePreInstalledSize()
        {
            await LoadGameJsonAsync();
            await GetAssetsJson(VersionInfo.GameJsonEntry.AssetIndex.Url);
            var size = 0;
            
            var os = RuntimeInformation.OSDescription.ToLower();
            bool isWindows = os.Contains("windows");
            bool isMacOS = os.Contains("macos") || os.Contains("darwin");
            bool isLinux = os.Contains("linux");

            VersionInfo.GameJsonEntry.Libraries.ForEach(x =>
            {
                if(x.Downloads is LibraryDownloads lib)
                {
                    if(lib.Artifact is Artifact art)
                        size += art.Size;
                    if (lib.Classifiers is Dictionary<string, Artifact> cla)
                    {
                        var nativeKey = "";
                        if (isWindows && lib.Classifiers.ContainsKey("natives-windows"))
                        {
                            nativeKey = "natives-windows";
                        }
                        else if (isMacOS && lib.Classifiers.ContainsKey("natives-macos"))
                        {
                            nativeKey = "natives-macos";
                        }
                        else if (isLinux && lib.Classifiers.ContainsKey("natives-linux"))
                        {
                            nativeKey = "natives-linux";
                        }

                        try
                        {
                            size += cla[nativeKey].Size;
                        }catch{ }
                    }
                }
            });
            size += VersionInfo.GameJsonEntry.AssetIndex.Size;
            size += VersionInfo.GameJsonEntry.Downloads.Client.Size;
            foreach (var ass in Assets.Objects)
            {
                size += ass.Value.Size;
            }
            
            return (ulong)size;
        }
        
        public async Task<string> LoadGameJsonAsync()
        {
            if (VersionInfo.GameJsonEntry == null)
            {
                string json = await _httpClient.GetStringAsync(VersionInfo.Version.Url);
                VersionInfo.GameJsonEntry = JsonConvert.DeserializeObject<GameJsonEntry>(json);
                if (VersionInfo.GameJsonEntry == null)
                {
                    throw new InvalidOperationException("Failed to deserialize game JSON.");
                }   
                return json;
            }
            else
            {
                return JsonSerializer.Serialize(VersionInfo.GameJsonEntry);
            }
        }

        public async Task Install(string GamePath)
        {
            if (!Directory.Exists(GamePath)) Directory.CreateDirectory(GamePath);
            if (!Directory.Exists(Path.Combine(GamePath, "versions"))) Directory.CreateDirectory(Path.Combine(GamePath, "versions"));
            if (!Directory.Exists(Path.Combine(GamePath, "versions", ID)))
                Directory.CreateDirectory(Path.Combine(GamePath, "versions", ID));

            ProgressCallback?.Invoke(DownloadStateEnum.DownloadJson,"", 5);
            var versionjson = await LoadGameJsonAsync();
            File.WriteAllText(Path.Combine(GamePath, "versions", ID, $"{ID}.json"), versionjson);

            ProgressCallback?.Invoke(DownloadStateEnum.DownloadJson,"", 10);
            VersionInfo.VersionAssetsJsonURL = VersionInfo.GameJsonEntry.AssetIndex.Url;
            Console.WriteLine(VersionInfo.VersionAssetsJsonURL);
            _httpClient = new HttpClient();
            var assetsjson = await GetAssetsJson(VersionInfo.VersionAssetsJsonURL);

            Directory.CreateDirectory(Path.Combine(GamePath, "assets", "indexes"));
            File.WriteAllText(Path.Combine(GamePath, "assets", "indexes", $"{VersionInfo.GameJsonEntry.Assets}.json"), assetsjson);

            ProgressCallback?.Invoke(DownloadStateEnum.DownloadAssets,"", 15);
            Assets = ParseAssetsJson(assetsjson); 
            DownloadAssets(GamePath).Wait();
            DownloadLibraries(GamePath).Wait();
            DownloadSubstance(GamePath).Wait();
        }

        public async Task<string> GetAssetsJson(string url)
        {
            if (Assets != null)
            {
                return JsonSerializer.Serialize(Assets);
            }
            else
            {
                var json = await _httpClient.GetStringAsync(url);
                Assets = ParseAssetsJson(json);
                return json;
            }
        }

        public async Task DownloadAssets(string GamePath)
        {
            if (Assets == null || Assets.Objects.Count == 0)
            {
                ProgressCallback?.Invoke(DownloadStateEnum.DownloadAssets,"(0/0)", 100);
                return;
            }

            var assetsPath = Path.Combine(GamePath, "assets", "objects");
            Directory.CreateDirectory(assetsPath);

            var semaphore = new SemaphoreSlim(DownloadThreadsCount);
            var tasks = new List<Task>();
            int totalFiles = Assets.Objects.Count;
            int completedFiles = 0;
            long totalBytes = Assets.Objects.Sum(a => a.Value.Size);
            long downloadedBytes = 0;
            object progressLock = new object();

            // Retry mechanism
            var retryQueue = new ConcurrentQueue<KeyValuePair<string, AssetsEntry.FileInfo>>();
            int maxRetries = 3;
            int currentRetry = 0;

            // First pass download
            foreach (var asset in Assets.Objects)
            {
                await semaphore.WaitAsync();
                tasks.Add(Task.Run(async () =>
                {
                    int attempt = 0;
                    bool success = false;

                    while (attempt < maxRetries && !success)
                    {
                        try
                        {
                            string hash = asset.Value.Hash;
                            string url = $"https://resources.download.minecraft.net/{hash[..2]}/{hash}";
                            string filePath = Path.Combine(assetsPath, hash[..2], hash);

                            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                            if (!File.Exists(filePath) || !VerifyFileSize(filePath, asset.Value.Size))
                            {
                                using (var response =
                                       await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                                {
                                    response.EnsureSuccessStatusCode();

                                    using (var stream = await response.Content.ReadAsStreamAsync())
                                    using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write,
                                               FileShare.None))
                                    {
                                        byte[] buffer = new byte[8192];
                                        int bytesRead;
                                        while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                                        {
                                            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));

                                            lock (progressLock)
                                            {
                                                downloadedBytes += bytesRead;
                                            }
                                        }
                                    }
                                }
                            }

                            if (!VerifyFileSize(filePath, asset.Value.Size))
                            {
                                throw new Exception("Downloaded file size does not match.");
                            }

                            success = true;
                        }
                        catch (Exception ex)
                        {
                            attempt++;
                            if (attempt >= maxRetries)
                            {
                                Console.WriteLine(
                                    $"Failed to download {asset.Key} after {maxRetries} attempts: {ex.Message}");
                                retryQueue.Enqueue(asset);
                            }
                            else
                            {
                                await Task.Delay(1000 * attempt); // Exponential backoff
                            }
                        }
                    }

                    lock (progressLock)
                    {
                        if (success)
                        {
                            completedFiles++;
                        }

                        double progressPercentage = (double)completedFiles / totalFiles * 100;
                        ProgressCallback?.Invoke(DownloadStateEnum.DownloadAssets,$"({completedFiles}/{totalFiles})",
                            progressPercentage);
                    }

                    semaphore.Release();
                }));
            }

            await Task.WhenAll(tasks);

            // Retry failed downloads
            while (currentRetry < maxRetries && !retryQueue.IsEmpty)
            {
                currentRetry++;
                Console.WriteLine($"Starting retry attempt {currentRetry} for {retryQueue.Count} assets");

                var retryTasks = new List<Task>();
                int retryCount = retryQueue.Count;
                int retryCompleted = 0;

                while (retryQueue.TryDequeue(out var asset))
                {
                    await semaphore.WaitAsync();
                    retryTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            string hash = asset.Value.Hash;
                            string url = $"https://resources.download.minecraft.net/{hash[..2]}/{hash}";
                            string filePath = Path.Combine(assetsPath, hash[..2], hash);

                            using (var response =
                                   await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                            {
                                response.EnsureSuccessStatusCode();

                                using (var stream = await response.Content.ReadAsStreamAsync())
                                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write,
                                           FileShare.None))
                                {
                                    byte[] buffer = new byte[8192];
                                    int bytesRead;
                                    while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                                    {
                                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                                    }
                                }
                            }

                            if (!VerifyFileSize(filePath, asset.Value.Size))
                            {
                                throw new Exception("Downloaded file size does not match.");
                            }

                            lock (progressLock)
                            {
                                completedFiles++;
                                retryCompleted++;
                                double progressPercentage = (double)completedFiles / totalFiles * 100;
                                ProgressCallback?.Invoke(
                                    DownloadStateEnum.DownloadAssets,$"({retryCompleted}/{retryCount})",
                                    progressPercentage);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Retry attempt {currentRetry} failed for {asset.Key}: {ex.Message}");
                            retryQueue.Enqueue(asset); // Add back to queue for next retry
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                }

                await Task.WhenAll(retryTasks);
            }

            ProgressCallback?.Invoke(DownloadStateEnum.DownloadAssetsSuccess,"OK", 100);
        }

        private bool VerifyFileSize(string filePath, long expectedSize)
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            FileInfo fileInfo = new FileInfo(filePath);
            return fileInfo.Length == expectedSize;
        }

        public async Task DownloadLibraries(string GamePath)
        {
            var libraries = Path.Combine(GamePath, "libraries");
            Directory.CreateDirectory(libraries);

            // Prepare all download items
            var items = new Dictionary<string, string>();
            var os = RuntimeInformation.OSDescription.ToLower();
            bool isWindows = os.Contains("windows");
            bool isMacOS = os.Contains("macos") || os.Contains("darwin");
            bool isLinux = os.Contains("linux");

            foreach (var library in VersionInfo.GameJsonEntry.Libraries)
            {
                if (library.Downloads?.Artifact != null)
                {
                    items.TryAdd(library.Downloads.Artifact.Path, library.Downloads.Artifact.Url);
                }

                if (library.Downloads?.Classifiers != null)
                {
                    string nativeKey = null;
                    if (isWindows && library.Downloads.Classifiers.ContainsKey("natives-windows"))
                    {
                        nativeKey = "natives-windows";
                    }
                    else if (isMacOS && library.Downloads.Classifiers.ContainsKey("natives-macos"))
                    {
                        nativeKey = "natives-macos";
                    }
                    else if (isLinux && library.Downloads.Classifiers.ContainsKey("natives-linux"))
                    {
                        nativeKey = "natives-linux";
                    }

                    if (nativeKey != null && library.Downloads.Classifiers[nativeKey] != null)
                    {
                        var native = library.Downloads.Classifiers[nativeKey];
                        items.TryAdd(native.Path, native.Url);
                    }
                }
            }

            if (items.Count == 0)
            {
                ProgressCallback?.Invoke(DownloadStateEnum.DownloadLibrary,"(0/0)", 100);
                return;
            }

            var semaphore = new SemaphoreSlim(DownloadThreadsCount);
            var tasks = new List<Task>();
            int totalFiles = items.Count;
            int completedFiles = 0;
            long downloadedBytes = 0;
            object progressLock = new object();

            // Retry mechanism
            var retryQueue = new ConcurrentQueue<KeyValuePair<string, string>>();
            int maxRetries = 3;
            int currentRetry = 0;

            // First pass download
            foreach (var library in items)
            {
                await semaphore.WaitAsync();
                tasks.Add(Task.Run(async () =>
                {
                    int attempt = 0;
                    bool success = false;

                    while (attempt < maxRetries && !success)
                    {
                        try
                        {
                            string path = Path.Combine(libraries, library.Key);
                            Directory.CreateDirectory(Path.GetDirectoryName(path));

                            if (!File.Exists(path))
                            {
                                using (var response = await _httpClient.GetAsync(library.Value,
                                           HttpCompletionOption.ResponseHeadersRead))
                                {
                                    response.EnsureSuccessStatusCode();

                                    using (var stream = await response.Content.ReadAsStreamAsync())
                                    using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write,
                                               FileShare.None))
                                    {
                                        byte[] buffer = new byte[8192];
                                        int bytesRead;
                                        while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                                        {
                                            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));

                                            lock (progressLock)
                                            {
                                                downloadedBytes += bytesRead;
                                            }
                                        }
                                    }
                                }
                            }

                            success = true;
                        }
                        catch (Exception ex)
                        {
                            attempt++;
                            if (attempt >= maxRetries)
                            {
                                Console.WriteLine(
                                    $"Failed to download {library.Key} after {maxRetries} attempts: {ex.Message}");
                                retryQueue.Enqueue(library);
                            }
                        }
                    }

                    lock (progressLock)
                    {
                        if (success)
                        {
                            completedFiles++;
                        }

                        double progressPercentage = (double)completedFiles / totalFiles * 100;
                        ProgressCallback?.Invoke(DownloadStateEnum.DownloadLibrary,
                            $"({completedFiles}/{totalFiles})",
                            progressPercentage
                        );
                    }

                    semaphore.Release();
                }));
            }

            await Task.WhenAll(tasks);

            // Retry failed downloads
            while (currentRetry < maxRetries && !retryQueue.IsEmpty)
            {
                currentRetry++;
                Console.WriteLine($"Starting retry attempt {currentRetry} for {retryQueue.Count} libraries");

                var retryTasks = new List<Task>();
                int retryCount = retryQueue.Count;
                int retryCompleted = 0;

                while (retryQueue.TryDequeue(out var library))
                {
                    await semaphore.WaitAsync();
                    retryTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            string path = Path.Combine(libraries, library.Key);
                            using (var response = await _httpClient.GetAsync(library.Value,
                                       HttpCompletionOption.ResponseHeadersRead))
                            {
                                response.EnsureSuccessStatusCode();

                                using (var stream = await response.Content.ReadAsStreamAsync())
                                using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write,
                                           FileShare.None))
                                {
                                    byte[] buffer = new byte[8192];
                                    int bytesRead;
                                    while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                                    {
                                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                                    }
                                }
                            }

                            lock (progressLock)
                            {
                                completedFiles++;
                                retryCompleted++;
                                double progressPercentage = (double)completedFiles / totalFiles * 100;
                                ProgressCallback?.Invoke(DownloadStateEnum.DownloadLibrary,
                                    $"({retryCompleted}/{retryCount})",
                                    progressPercentage
                                );
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Retry attempt {currentRetry} failed for {library.Key}: {ex.Message}");
                            retryQueue.Enqueue(library); // Add back to queue for next retry
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                }

                await Task.WhenAll(retryTasks);
            }

            ProgressCallback?.Invoke(DownloadStateEnum.DownloadLibrarySuccess,"OK", 100);
        }

        public async Task DownloadSubstance(string GamePath)
        {
            var substancePath = Path.Combine(GamePath, "versions", ID,
                $"{ID}.jar");
            var url = VersionInfo.GameJsonEntry.Downloads.Client.Url;

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(substancePath));

            // Skip if already downloaded
            if (File.Exists(substancePath))
            {
                ProgressCallback?.Invoke(DownloadStateEnum.DownloadSuccess,"OK", 100);
                return;
            }

            ProgressCallback?.Invoke(DownloadStateEnum.DownloadClient,"Client...", 0);

            try
            {
                using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    // Get total file size for progress calculation
                    long totalBytes = response.Content.Headers.ContentLength ?? 0;
                    long downloadedBytes = 0;

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream =
                           new FileStream(substancePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        byte[] buffer = new byte[8192];
                        int bytesRead;
                        var lastProgressUpdate = DateTime.Now;
                        double lastPercentage = 0;

                        while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                        {
                            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                            downloadedBytes += bytesRead;

                            // Update progress (throttle updates to avoid UI spam)
                            if (DateTime.Now - lastProgressUpdate > TimeSpan.FromMilliseconds(100) ||
                                downloadedBytes == totalBytes)
                            {
                                double percentage = totalBytes > 0 ? (double)downloadedBytes / totalBytes * 100 : 0;

                                // Only update if percentage changed significantly
                                if (Math.Abs(percentage - lastPercentage) >= 1 || downloadedBytes == totalBytes)
                                {
                                    ProgressCallback?.Invoke(DownloadStateEnum.DownloadClient,
                                        $"{FormatBytes(downloadedBytes)}/{FormatBytes(totalBytes)}",
                                        percentage
                                    );
                                    lastPercentage = percentage;
                                    lastProgressUpdate = DateTime.Now;
                                }
                            }
                        }
                    }
                }

                ProgressCallback?.Invoke(DownloadStateEnum.DownloadSuccess,"OK", 100);
            }
            catch (Exception ex)
            {
                // Clean up partially downloaded file on error
                if (File.Exists(substancePath))
                {
                    File.Delete(substancePath);
                }

                ProgressCallback?.Invoke(DownloadStateEnum.Error,$"Error", 100);
                throw;
            }
        }

        // Helper method to format bytes
        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int suffixIndex = 0;
            double dblBytes = bytes;

            while (dblBytes >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                dblBytes /= 1024;
                suffixIndex++;
            }

            return $"{dblBytes:0.##} {suffixes[suffixIndex]}";
        }

        private AssetsEntry.RootObject ParseAssetsJson(string jsonString)
        {
            try
            {
                AssetsEntry.RootObject root = JsonConvert.DeserializeObject<AssetsEntry.RootObject>(jsonString);

                if (root?.Objects == null)
                {
                    return new AssetsEntry.RootObject();
                }

                // 返回解析后的结果
                return root;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing JSON: {ex.Message}");
                return new AssetsEntry.RootObject();
            }
        }
    }
}