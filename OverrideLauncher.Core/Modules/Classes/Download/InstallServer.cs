
using System.Text.Json;
using OverrideLauncher.Core.Modules.Entry.DownloadEntry;
using OverrideLauncher.Core.Modules.Entry.ServerEntry;

namespace OverrideLauncher.Core.Modules.Classes.Download;

public class InstallServer
{
    public int DownloadThreadsCount { get; set; } = 64;
    public Action<string, double> ProgressCallback { get; set; }
    private static HttpClient _httpServer = new HttpClient();
    public DownloadServerInfoEntry VersionInfo { get; private set; } = new();
    private string ID = null;
    
    public InstallServer(GameVersion GameVersion)
    {
        VersionInfo = new DownloadServerInfoEntry { Version = GameVersion };
        ID = GameVersion.Id;
    }

    public async Task Install(ServerInstancesInfo ServerInfo)
    {
        if (!Directory.Exists(ServerInfo.InstallPath)) Directory.CreateDirectory(ServerInfo.InstallPath);
        
        ProgressCallback?.Invoke("LoadConfigs...", 5);
        var versionjson = await LoadGameJsonAsync();
        File.WriteAllText(Path.Combine(ServerInfo.InstallPath, $"{ID}.json"), versionjson);
        ProgressCallback?.Invoke("DownloadFiles", 20);
        
        var serverurl = VersionInfo.GameJsonEntry.Downloads.Server.Url;
        Console.WriteLine(serverurl);

        DownloadSubstance(ServerInfo.InstallPath).Wait();
    }

    public async Task DownloadSubstance(string DownloadPath)
    {
        var substancePath = Path.Combine(DownloadPath, $"server.jar");
        var url = VersionInfo.GameJsonEntry.Downloads.Server.Url;

        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(substancePath));

        // Skip if already downloaded
        if (File.Exists(substancePath))
        {
            ProgressCallback?.Invoke("Game Server already downloaded.", 100);
            return;
        }

        ProgressCallback?.Invoke("Downloading game Server...", 0);

        try
        {
            using (var response = await _httpServer.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
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
                                ProgressCallback?.Invoke(
                                    $"Downloading game Server: {FormatBytes(downloadedBytes)}/{FormatBytes(totalBytes)}",
                                    percentage
                                );
                                lastPercentage = percentage;
                                lastProgressUpdate = DateTime.Now;
                            }
                        }
                    }
                }
            }

            ProgressCallback?.Invoke("Game Server downloaded successfully.", 100);
        }
        catch (Exception ex)
        {
            // Clean up partially downloaded file on error
            if (File.Exists(substancePath))
            {
                File.Delete(substancePath);
            }

            ProgressCallback?.Invoke($"Error downloading game Server: {ex.Message}", 0);
            throw;
        }
    }
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
    public async Task<string> LoadGameJsonAsync()
    {
        string json = await _httpServer.GetStringAsync(VersionInfo.Version.Url);
        VersionInfo.GameJsonEntry = JsonSerializer.Deserialize<GameJsonEntry>(json);
        if (VersionInfo.GameJsonEntry == null)
        {
            throw new InvalidOperationException("Failed to deserialize game JSON.");
        }
        return json;
    }
}