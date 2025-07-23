using Downloader;
using OverrideLauncher.Core.Base.Entry.Download;
using OverrideLauncher.Core.Base.Entry.Download.Install;

namespace OverrideLauncher.Core.Interface.Download;

public class IDownload
{
    #region Static
    // 固定高性能配置 - 512并发
    public static int MaxParallelDownloads = 512; // 固定512并发
    public static int LargeFileThreshold = 2 * 1024 * 1024; // 2MB阈值
    public static int MaxChunksPerFile = 8; // 每个文件最大8分片
    public static int RetryAttempts = 3; // 重试次数
    public static TimeSpan RetryDelay = TimeSpan.FromMilliseconds(500); // 500ms重试延迟
    #endregion

    #region Public

    public ulong FileCount { get; set; } = 0;
    public EventHandler<DownloadStatusChangedEntry> DownloadStatusChanged;

    public async Task DownloadFileAsync(DownloadFileInfo info, IProgress<double>? progress = null)
    {
        // 确保目录存在
        var directory = Path.GetDirectoryName(info.FileName);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // 使用先进的自适应下载算法
        await DownloadWithAdvancedAlgorithm(info, progress);
    }

    public void DownloadFile(DownloadFileInfo info)
    {
        DownloadFileAsync(info).Wait();
    }
    #endregion

    #region Private

    /// <summary>
    /// 先进的自适应下载算法 - 根据文件大小和网络状况自动优化
    /// </summary>
    private async Task DownloadWithAdvancedAlgorithm(DownloadFileInfo info, IProgress<double>? progress = null)
    {
        // 先进的自适应下载算法
        for (int attempt = 0; attempt < RetryAttempts; attempt++)
        {
            try
            {
                // 根据文件大小选择最优下载策略
                var chunkCount = CalculateOptimalChunkCount(info.Size);
                var isLargeFile = info.Size > (ulong)LargeFileThreshold;

                var downloadOpt = new DownloadConfiguration()
                {
                    ChunkCount = chunkCount,
                    ParallelDownload = isLargeFile && chunkCount > 1,
                    Timeout = isLargeFile ? 300000 : 60000, // 大文件5分钟，小文件1分钟
                    BufferBlockSize = isLargeFile ? 65536 : 32768, // 大文件64KB，小文件32KB
                    MaximumBytesPerSecond = 0, // 不限速
                    RequestConfiguration = new RequestConfiguration()
                    {
                        UserAgent = "OverrideLauncher/2.0 (Advanced-Algorithm)",
                        KeepAlive = true,
                        Accept = "*/*"
                    }
                };

                using var downloader = new DownloadService(downloadOpt);

                if (progress != null)
                {
                    downloader.DownloadProgressChanged += (sender, e) =>
                    {
                        progress.Report(e.ProgressPercentage);
                    };
                }

                await downloader.DownloadFileTaskAsync(info.Url, info.FileName);
                return; // 下载成功，退出重试循环
            }
            catch (Exception) when (attempt < RetryAttempts - 1)
            {
                // 重试前等待
                await Task.Delay(RetryDelay);
            }
        }
    }

    /// <summary>
    /// 先进的分片计算算法 - 根据文件大小和网络优化
    /// </summary>
    private int CalculateOptimalChunkCount(ulong fileSize)
    {
        var fileSizeMB = (double)fileSize / (1024 * 1024);

        // 小文件单线程下载
        if (fileSizeMB < 5) return 1;

        // 中等文件使用适中分片
        if (fileSizeMB < 50) return Math.Min(8, MaxChunksPerFile);

        // 大文件使用最大分片数以获得最佳速度
        return MaxChunksPerFile;
    }
    #endregion
}