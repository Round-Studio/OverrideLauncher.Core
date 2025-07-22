using System.Text.Json;
using System.Text.Json.Serialization;
using OverrideLauncher.Core.Base.Dictionary;
using OverrideLauncher.Core.Base.Entry.Download.Install;
using OverrideLauncher.Core.Base.Entry.Download.Install.Client;
using OverrideLauncher.Core.Base.Entry.Download.Install.Manifest;
using OverrideLauncher.Core.Base.Enum;
using OverrideLauncher.Core.Base.Enum.Download;
using OverrideLauncher.Core.Classes.Install.Manifest;
using OverrideLauncher.Core.Interface.Download;

namespace OverrideLauncher.Core.Classes.Install;

public class InstallClient : Download
{
    #region Public
    public ManifestClientJson ManifestClientJson { get; set; }
    public ManifestClientAssetsJson ManifestClientAssetsJson { get; set; }
    public EventHandler<DownloadStatusChangedEntry> DownloadStatusChanged;
    public InstallClient(ManifestMojang.ManifestVersion manifestVersion)
    {
        _installName = manifestVersion.Id;
        ManifestClientJson = InstallHelper.TryingGetClientJson(manifestVersion).Result;
        ManifestClientAssetsJson = InstallHelper.TryingGetClientAssetsJson(ManifestClientJson).Result;
    }

    public async Task Install(InstallClientInfo info)
    {
        _installClientInfo = info;
        if(!string.IsNullOrEmpty(info.InstallName)) _installName = info.InstallName;
        
        SaveJson();
        PrepareFiles();

        FileCount = (ulong)(downloadList.Files.Count - 1);
        await Download();
    }

    #endregion
    
    #region Private
    private string _installName { get; set; }
    private InstallClientInfo _installClientInfo { get; set; }
    private DownloadListEntry downloadList { get; set; } = new();

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
    private void SaveJson()
    {
        var versionJsonPath = Path.Combine(_installClientInfo.InstallPath, DictionaryGameRoot.VersionsPath,
            _installName, $"{_installName}.json");
        var assetsJsonPath = Path.Combine(_installClientInfo.InstallPath, DictionaryGameRoot.AssetsIndexPath,
            $"{ManifestClientJson.AssetIndex.Id}.json");
        
        Directory.CreateDirectory(Path.GetDirectoryName(versionJsonPath));
        Directory.CreateDirectory(Path.GetDirectoryName(assetsJsonPath));
        
        File.WriteAllText(versionJsonPath,JsonSerializer.Serialize(ManifestClientJson));
        File.WriteAllText(assetsJsonPath,JsonSerializer.Serialize(ManifestClientAssetsJson));
    }

    private void PrepareFiles()
    {
        downloadList.Files.Add(new DownloadListEntry.DownloadFileItem()
        {
            Type = FileType.BaseGame,
            FileInfo = new()
            {
                Size = (ulong)ManifestClientJson.Downloads.Client.Size,
                Url = ManifestClientJson.Downloads.Client.Url,
                FileName = Path.Combine(_installClientInfo.InstallPath, DictionaryGameRoot.VersionsPath, _installName,
                    $"{_installName}.jar")
            }
        }); // 添加本体文件

        downloadList.Files.AddRange(GetArtifacts()); // 添加所有 Artifact 文件
        downloadList.Files.AddRange(GetAssets()); // 添加所有 Assets 文件
    }

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

        // 发送最终完成状态
        var hasErrors = _fileTypeStats.Values.Any(s => s.FailedFiles > 0);
        var finalStatus = hasErrors ? DownloadStatusType.Error : DownloadStatusType.CompletionSuccess;
        DownloadStatusChanged?.Invoke(this, new DownloadStatusChangedEntry()
        {
            Progress = 100.0,
            Status = finalStatus,
            FileType = FileType.BaseGame,
            CompletedFiles = _fileTypeStats.Values.Sum(s => s.CompletedFiles),
            TotalFiles = totalCount
        });
    }

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
                else
                {
                    Console.WriteLine($"重新下载: {Path.GetFileName(file.FileInfo.FileName)} (大小不匹配: 本地={FormatFileSize(fileInfo.Length)}, 期望={FormatFileSize((long)file.FileInfo.Size)})");
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
                        CurrentFileName = Path.GetFileName(file.FileInfo.FileName) + (skipped ? " (跳过)" : ""),
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

    private List<DownloadListEntry.DownloadFileItem> GetArtifacts()
    {
        var list = new List<DownloadListEntry.DownloadFileItem>();
        ManifestClientJson.Libraries.ForEach(x =>
        {
            if (x.Downloads != null && x.Downloads.Artifact != null)
            {
                list.Add(new DownloadListEntry.DownloadFileItem()
                {
                    Type = FileType.JarFile,
                    FileInfo = new()
                    {
                        FileName = Path.Combine(_installClientInfo.InstallPath, DictionaryGameRoot.LibrariesPath,x.Downloads.Artifact.Path),
                        Url = x.Downloads.Artifact.Url,
                        Size = (ulong)x.Downloads.Artifact.Size,
                        Hash = x.Downloads.Artifact.Sha1
                    }
                });
            }
        });

        return list;
    }

    private List<DownloadListEntry.DownloadFileItem> GetAssets()
    {
        var list = new List<DownloadListEntry.DownloadFileItem>();

        if (ManifestClientAssetsJson?.Objects != null)
        {
            foreach (var asset in ManifestClientAssetsJson.Objects)
            {
                var hash = asset.Value.Hash;
                var hashPrefix = hash.Substring(0, 2);
                var url = $"{DictionaryDownloadHost.RootHost}/{hashPrefix}/{hash}";
                var fileName = Path.Combine(_installClientInfo.InstallPath, DictionaryGameRoot.AssetsObjectPath, hashPrefix, hash);

                list.Add(new DownloadListEntry.DownloadFileItem()
                {
                    Type = FileType.AssetFile,
                    FileInfo = new()
                    {
                        FileName = fileName,
                        Url = url,
                        Size = (ulong)asset.Value.Size,
                        Hash = hash
                    }
                });
            }
        }

        return list;
    }

    private DownloadStatusType GetStatusForFileType(FileType fileType)
    {
        return fileType switch
        {
            FileType.BaseGame => DownloadStatusType.DownloadClient,
            FileType.JarFile => DownloadStatusType.DownloadLibrary,
            FileType.AssetFile => DownloadStatusType.DownloadAssets,
            _ => DownloadStatusType.DownloadJson
        };
    }

    private DownloadStatusType GetCompletionStatusForFileType(FileType fileType)
    {
        return fileType switch
        {
            FileType.BaseGame => DownloadStatusType.CompletionJar,
            FileType.JarFile => DownloadStatusType.DownloadLibrarySuccess,
            FileType.AssetFile => DownloadStatusType.DownloadAssetsSuccess,
            _ => DownloadStatusType.CompletionSuccess
        };
    }

    /// <summary>
    /// 格式化文件大小显示
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB" };
        int counter = 0;
        decimal number = bytes;

        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }

        return $"{number:n1} {suffixes[counter]}";
    }

    #endregion
}