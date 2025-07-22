namespace OverrideLauncher.Core.Classes.Install;

/// <summary>
/// 下载配置类，允许用户自定义下载参数
/// </summary>
public class DownloadSettings
{
    /// <summary>
    /// 最大并行下载数（默认基于CPU核心数）
    /// </summary>
    public int MaxParallelDownloads { get; set; } = Math.Max(16, Environment.ProcessorCount * 8);

    /// <summary>
    /// 小文件最大并行数
    /// </summary>
    public int MaxSmallFileParallel { get; set; } = Math.Max(32, Environment.ProcessorCount * 16);

    /// <summary>
    /// 大文件最大并行数
    /// </summary>
    public int MaxLargeFileParallel { get; set; } = Math.Max(8, Environment.ProcessorCount * 2);

    /// <summary>
    /// 大文件阈值（字节）
    /// </summary>
    public int LargeFileThreshold { get; set; } = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// 每个大文件的最大分片数
    /// </summary>
    public int MaxChunksPerFile { get; set; } = 16;

    /// <summary>
    /// 重试次数
    /// </summary>
    public int RetryAttempts { get; set; } = 3;

    /// <summary>
    /// 重试延迟（毫秒）
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// 小文件超时时间（毫秒）
    /// </summary>
    public int SmallFileTimeoutMs { get; set; } = 30000; // 30秒

    /// <summary>
    /// 大文件超时时间（毫秒）
    /// </summary>
    public int LargeFileTimeoutMs { get; set; } = 180000; // 3分钟

    /// <summary>
    /// 缓冲区大小（字节）
    /// </summary>
    public int BufferSize { get; set; } = 32768; // 32KB

    /// <summary>
    /// 是否启用性能监控
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = true;

    /// <summary>
    /// 是否启用详细日志
    /// </summary>
    public bool EnableVerboseLogging { get; set; } = false;

    /// <summary>
    /// 创建默认配置
    /// </summary>
    public static DownloadSettings Default => new();

    /// <summary>
    /// 创建高性能配置（适合高速网络和强劲硬件）
    /// </summary>
    public static DownloadSettings HighPerformance => new()
    {
        MaxParallelDownloads = Environment.ProcessorCount * 16,
        MaxSmallFileParallel = Environment.ProcessorCount * 32,
        MaxLargeFileParallel = Environment.ProcessorCount * 4,
        MaxChunksPerFile = 32,
        BufferSize = 65536, // 64KB
        RetryAttempts = 5
    };

    /// <summary>
    /// 创建保守配置（适合较慢网络或较弱硬件）
    /// </summary>
    public static DownloadSettings Conservative => new()
    {
        MaxParallelDownloads = Math.Max(4, Environment.ProcessorCount * 2),
        MaxSmallFileParallel = Math.Max(8, Environment.ProcessorCount * 4),
        MaxLargeFileParallel = Math.Max(2, Environment.ProcessorCount),
        MaxChunksPerFile = 4,
        BufferSize = 16384, // 16KB
        RetryAttempts = 2,
        SmallFileTimeoutMs = 60000, // 1分钟
        LargeFileTimeoutMs = 300000 // 5分钟
    };

    /// <summary>
    /// 验证配置的合理性
    /// </summary>
    public void Validate()
    {
        if (MaxParallelDownloads <= 0)
            throw new ArgumentException("MaxParallelDownloads must be greater than 0");
        
        if (MaxSmallFileParallel <= 0)
            throw new ArgumentException("MaxSmallFileParallel must be greater than 0");
        
        if (MaxLargeFileParallel <= 0)
            throw new ArgumentException("MaxLargeFileParallel must be greater than 0");
        
        if (LargeFileThreshold <= 0)
            throw new ArgumentException("LargeFileThreshold must be greater than 0");
        
        if (MaxChunksPerFile <= 0)
            throw new ArgumentException("MaxChunksPerFile must be greater than 0");
        
        if (RetryAttempts < 0)
            throw new ArgumentException("RetryAttempts must be non-negative");
        
        if (RetryDelayMs < 0)
            throw new ArgumentException("RetryDelayMs must be non-negative");
        
        if (SmallFileTimeoutMs <= 0)
            throw new ArgumentException("SmallFileTimeoutMs must be greater than 0");
        
        if (LargeFileTimeoutMs <= 0)
            throw new ArgumentException("LargeFileTimeoutMs must be greater than 0");
        
        if (BufferSize <= 0)
            throw new ArgumentException("BufferSize must be greater than 0");
    }

    /// <summary>
    /// 应用配置到下载系统
    /// </summary>
    public void Apply()
    {
        Validate();
        
        var downloadClass = typeof(OverrideLauncher.Core.Interface.Download.Download);
        
        // 使用反射设置静态属性
        downloadClass.GetField("MaxParallelDownloads")?.SetValue(null, MaxParallelDownloads);
        downloadClass.GetField("MaxSmallFileParallel")?.SetValue(null, MaxSmallFileParallel);
        downloadClass.GetField("MaxLargeFileParallel")?.SetValue(null, MaxLargeFileParallel);
        downloadClass.GetField("LargeFileThreshold")?.SetValue(null, LargeFileThreshold);
        downloadClass.GetField("MaxChunksPerFile")?.SetValue(null, MaxChunksPerFile);
        downloadClass.GetField("RetryAttempts")?.SetValue(null, RetryAttempts);
        downloadClass.GetField("RetryDelay")?.SetValue(null, TimeSpan.FromMilliseconds(RetryDelayMs));
    }

    public override string ToString()
    {
        return $"DownloadSettings: MaxParallel={MaxParallelDownloads}, SmallFile={MaxSmallFileParallel}, LargeFile={MaxLargeFileParallel}, Chunks={MaxChunksPerFile}";
    }
}
