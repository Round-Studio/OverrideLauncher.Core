using OverrideLauncher.Core.Base.Dictionary;
using OverrideLauncher.Core.Base.Entry.Download.Install;
using OverrideLauncher.Core.Base.Entry.Download.Install.Client;
using OverrideLauncher.Core.Base.Entry.Download.Install.LiteLoader;
using OverrideLauncher.Core.Base.Entry.Download.Install.Manifest;
using OverrideLauncher.Core.Classes.Install.Manifest;
using OverrideLauncher.Core.Interface.Download;
using OverrideLauncher.Core.Base.Entry.Download;
using OverrideLauncher.Core.Base.Enum;
using OverrideLauncher.Core.Base.Enum.Download;

namespace OverrideLauncher.Core.Classes.Install.Installer;

public class InstallerLiteLoader : IDownload
{
    private readonly LiteLoaderManifest.VersionData _versionData;
    private DownloadListEntry downloadList { get; set; } = new();
    private ClientRootInfo _rootInfo { get; set; }

    // 按文件类型分别统计
    private readonly Dictionary<FileType, FileTypeStats> _fileTypeStats = new();

    private class FileTypeStats
    {
        public int TotalFiles { get; set; }
        public int CompletedFiles { get; set; }
        public int FailedFiles { get; set; }
        public int SkippedFiles { get; set; }
        public int NeedDownloadFiles { get; set; }
    }

    public InstallerLiteLoader(LiteLoaderManifest.VersionData versionData)
    {
        if (versionData == null) throw new NullReferenceException();
        _versionData = versionData;
    }

    public async Task Install(ClientRootInfo rootInfo)
    {
        _rootInfo = rootInfo;
        var valjson = InstallHelper.GetClientJsonEntry(rootInfo);
        // Get version from snapshots or artefacts
        string liteLoaderVersion = null;
        if (_versionData.Snapshots?.LiteLoader?.ContainsKey("latest") == true)
        {
            liteLoaderVersion = _versionData.Snapshots.LiteLoader["latest"].Version;
        }
        else if (_versionData.Artefacts?.LiteLoader?.ContainsKey("latest") == true)
        {
            liteLoaderVersion = _versionData.Artefacts.LiteLoader["latest"].Version;
        }

        valjson.ModLoader.Add(new ModLoaderInfo()
        {
            Name = "liteloader",
            Version = liteLoaderVersion ?? _versionData.Dev?.FgVersion ?? "unknown"
        });

        valjson.MainClass = "net.minecraft.launchwrapper.Launch";
        if (valjson.Arguments == null) valjson.Arguments = new();
        if (valjson.Arguments.Game == null) valjson.Arguments.Game = new();

        // Get tweakClass from the artifact data
        string tweakClass = "com.mumfrey.liteloader.launch.LiteLoaderTweaker"; // default fallback
        if (_versionData.Snapshots?.LiteLoader?.ContainsKey("latest") == true)
        {
            tweakClass = _versionData.Snapshots.LiteLoader["latest"].TweakClass ?? tweakClass;
        }
        else if (_versionData.Artefacts?.LiteLoader?.ContainsKey("latest") == true)
        {
            tweakClass = _versionData.Artefacts.LiteLoader["latest"].TweakClass ?? tweakClass;
        }

        valjson.Arguments.Game.Add("--tweakClass");
        valjson.Arguments.Game.Add(tweakClass);

        // Get libraries from snapshots.com.mumfrey:liteloader.latest.libraries
        if (_versionData.Snapshots?.LiteLoader?.ContainsKey("latest") == true)
        {
            var latestSnapshot = _versionData.Snapshots.LiteLoader["latest"];
            if (latestSnapshot.Libraries != null)
            {
                latestSnapshot.Libraries.ForEach(x =>
                {
                    var url = string.IsNullOrEmpty(x.Url) ? DictionaryDownloadHost.Sources.LibrariesHost : x.Url;
                    valjson.Libraries.Add(new ManifestClientJson.Library()
                    {
                        Name = x.Name,
                        Url = url
                    });
                });
            }

            // Add the LiteLoader jar itself
            valjson.Libraries.Add(new ManifestClientJson.Library()
            {
                Name = $"com.mumfrey:liteloader:{latestSnapshot.Version}",
                Url = _versionData.Repo?.Url ?? "https://dl.liteloader.com/versions/"
            });
        }
        // Fallback to artefacts if snapshots are not available
        else if (_versionData.Artefacts?.LiteLoader?.ContainsKey("latest") == true)
        {
            var latestArtefact = _versionData.Artefacts.LiteLoader["latest"];
            if (latestArtefact.Libraries != null)
            {
                latestArtefact.Libraries.ForEach(x =>
                {
                    var url = string.IsNullOrEmpty(x.Url) ? DictionaryDownloadHost.Sources.LibrariesHost : x.Url;
                    valjson.Libraries.Add(new ManifestClientJson.Library()
                    {
                        Name = x.Name,
                        Url = url
                    });
                });
            }

            // Add the LiteLoader jar itself
            valjson.Libraries.Add(new ManifestClientJson.Library()
            {
                Name = $"com.mumfrey:liteloader:{latestArtefact.Version}",
                Url = _versionData.Repo?.Url ?? "https://dl.liteloader.com/versions/"
            });
        }

        InstallHelper.SaveClientJson(valjson, _rootInfo);

        // 准备下载文件并执行下载
        PrepareFiles(valjson.Libraries.Where(lib => lib.Name.Contains("liteloader") ||
                                                   lib.Name.Contains("launchwrapper") ||
                                                   lib.Name.Contains("asm-all")).ToList());
        FileCount = (ulong)(downloadList.Files.Count > 0 ? downloadList.Files.Count - 1 : 0);
        if (downloadList.Files.Count > 0)
        {
            await Download();
        }
    }

    /// <summary>
    /// 将 Maven 坐标转换为文件路径
    /// </summary>
    /// <param name="mavenCoordinate">Maven 坐标，格式：group:artifact:version</param>
    /// <returns>相对于 libraries 目录的文件路径</returns>
    private string MavenCoordinateToPath(string mavenCoordinate)
    {
        var parts = mavenCoordinate.Split(':');
        if (parts.Length < 3)
        {
            throw new ArgumentException($"Invalid Maven coordinate: {mavenCoordinate}");
        }

        var group = parts[0].Replace('.', '/');
        var artifact = parts[1];
        var version = parts[2];
        var fileName = $"{artifact}-{version}.jar";

        return Path.Combine(group, artifact, version, fileName);
    }

    /// <summary>
    /// 准备下载文件列表
    /// </summary>
    /// <param name="liteLoaderLibraries">LiteLoader 库列表</param>
    private void PrepareFiles(List<ManifestClientJson.Library> liteLoaderLibraries)
    {
        foreach (var library in liteLoaderLibraries)
        {
            var artifactPath = MavenCoordinateToPath(library.Name);
            var fullUrl = $"{library.Url.TrimEnd('/')}/{artifactPath}";
            var fileName = Path.Combine(_rootInfo.InstallPath, DictionaryGameRoot.LibrariesPath, artifactPath);

            downloadList.Files.Add(new DownloadListEntry.DownloadFileItem()
            {
                Type = FileType.JarFile,
                FileInfo = new()
                {
                    FileName = fileName,
                    Url = fullUrl,
                    Size = library.Size,
                    Hash = "" // LiteLoader 库通常没有提供 hash
                }
            });
        }
    }

    /// <summary>
    /// 执行下载任务
    /// </summary>
    private async Task Download()
    {
        var totalCount = downloadList.Files.Count;
        var lockObj = new object();

        // 按文件类型分组并预检查
        var filesByType = downloadList.Files.GroupBy(f => f.Type).ToDictionary(g => g.Key, g => g.ToList());

        // 初始化每种文件类型的统计
        foreach (var kvp in filesByType)
        {
            var fileType = kvp.Key;
            var files = kvp.Value;

            var stats = new FileTypeStats { TotalFiles = files.Count };

            // 预检查每个文件
            foreach (var file in files)
            {
                if (File.Exists(file.FileInfo.FileName))
                {
                    var fileInfo = new FileInfo(file.FileInfo.FileName);
                    if (fileInfo.Length == (long)file.FileInfo.Size)
                    {
                        stats.SkippedFiles++;
                    }
                    else
                    {
                        stats.NeedDownloadFiles++;
                    }
                }
                else
                {
                    stats.NeedDownloadFiles++;
                }
            }

            _fileTypeStats[fileType] = stats;
            Console.WriteLine($"[{fileType}] 总文件 {stats.TotalFiles} 个，需要下载 {stats.NeedDownloadFiles} 个，跳过 {stats.SkippedFiles} 个");
        }

        Console.WriteLine($"开始高速下载: 总文件 {totalCount} 个，使用 {MaxParallelDownloads} 并发");

        // 使用固定512并发处理所有文件
        var semaphore = new SemaphoreSlim(MaxParallelDownloads);
        var downloadTasks = downloadList.Files.Select(file => DownloadFileWithRetry(file, semaphore, lockObj));

        // 等待所有任务完成
        await Task.WhenAll(downloadTasks);

        // 输出最终统计
        Console.WriteLine("\n=== 下载完成统计 ===");
        foreach (var kvp in _fileTypeStats)
        {
            var fileType = kvp.Key;
            var stats = kvp.Value;
            Console.WriteLine($"[{fileType}] 成功 {stats.CompletedFiles}/{stats.TotalFiles} 个 (下载 {stats.CompletedFiles - stats.SkippedFiles} 个, 跳过 {stats.SkippedFiles} 个), 失败 {stats.FailedFiles} 个");
        }

        Console.WriteLine("LiteLoader 库下载完成！");
    }

    /// <summary>
    /// 带重试的文件下载
    /// </summary>
    private async Task DownloadFileWithRetry(DownloadListEntry.DownloadFileItem file, SemaphoreSlim semaphore,
        object lockObj)
    {
        await semaphore.WaitAsync();
        try
        {
            var success = false;
            var skipped = false;
            Exception? lastException = null;

            // 检查文件是否已存在且大小正确
            if (File.Exists(file.FileInfo.FileName))
            {
                var fileInfo = new FileInfo(file.FileInfo.FileName);
                if (fileInfo.Length == (long)file.FileInfo.Size)
                {
                    // 文件已存在且大小正确，跳过下载
                    success = true;
                    skipped = true;
                }
            }

            // 检查该文件类型是否只有一个文件（需要实时进度）
            var stats = _fileTypeStats[file.Type];
            var isSingleFile = stats.TotalFiles == 1;

            // 如果文件不存在或大小不匹配，则下载
            if (!skipped)
            {
                // 重试机制
                for (int attempt = 0; attempt < RetryAttempts; attempt++)
                {
                    try
                    {
                        if (isSingleFile)
                        {
                            // 单文件类型，提供实时进度回调
                            var progress = new Progress<double>(percentage =>
                            {
                                DownloadStatusChanged?.Invoke(this, new DownloadStatusChangedEntry()
                                {
                                    Progress = percentage,
                                    Status = GetStatusForFileType(file.Type),
                                    FileType = file.Type,
                                    CurrentFileName = Path.GetFileName(file.FileInfo.FileName),
                                    CompletedFiles = percentage >= 100 ? 1 : 0,
                                    TotalFiles = 1
                                });
                            });
                            await DownloadFileAsync(file.FileInfo, progress);
                        }
                        else
                        {
                            // 多文件类型，不提供实时进度
                            await DownloadFileAsync(file.FileInfo);
                        }
                        success = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        if (attempt < RetryAttempts - 1)
                        {
                            await Task.Delay(RetryDelay);
                        }
                    }
                }
            }

            lock (lockObj)
            {
                // 更新该文件类型的统计
                if (success)
                {
                    stats.CompletedFiles++;
                    if (skipped)
                    {
                        stats.SkippedFiles++;
                    }
                }
                else
                {
                    stats.FailedFiles++;
                    Console.WriteLine($"下载失败: {Path.GetFileName(file.FileInfo.FileName)} - {lastException?.Message}");
                }

                // 计算该文件类型的进度
                var typeProgress = (double)stats.CompletedFiles / stats.TotalFiles * 100;
                var status = success ? GetStatusForFileType(file.Type) : DownloadStatusType.Error;

                // 只有在多文件类型或文件完成时才触发回调（单文件类型的实时进度已在上面处理）
                if (!isSingleFile || typeProgress >= 100 || !success)
                {
                    DownloadStatusChanged?.Invoke(this, new DownloadStatusChangedEntry()
                    {
                        Progress = typeProgress,
                        Status = status,
                        FileType = file.Type,
                        CurrentFileName = Path.GetFileName(file.FileInfo.FileName),
                        CompletedFiles = stats.CompletedFiles,
                        TotalFiles = stats.TotalFiles
                    });
                }
            }
        }
        finally
        {
            semaphore.Release();
        }
    }
}