using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Downloader;
using OverrideLauncher.Core.Base.Entry.Download;
using OverrideLauncher.Core.Base.Entry.Download.Install;
using OverrideLauncher.Core.Base.Enum;
using OverrideLauncher.Core.Base.Enum.Download;

namespace OverrideLauncher.Core.Interface.Download
{
    public class IDownload : IDisposable
    {
        #region Configuration
        // Configurable settings with default values
        public int MaxParallelDownloads { get; set; } = 512;
        public int SmallFileThreshold { get; set; } = 2 * 1024 * 1024; // 1MB
        public int MaxChunksPerFile { get; set; } = 8;
        public int RetryAttempts { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(500);
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
        #endregion

        #region Public Properties
        public ulong FileCount { get; set; } = 0;
        public EventHandler<DownloadStatusChangedEntry> DownloadStatusChanged;
        public DownloadListEntry downloadList { get; set; } = new();
        #endregion

        #region Private Fields
        public readonly Dictionary<FileType, FileTypeStats> _fileTypeStats = new();
        private readonly HttpClient _httpClient;
        private bool _disposed;
        #endregion

        #region Constructor
        public IDownload()
        {
            _httpClient = new HttpClient(new HttpClientHandler()
            {
                UseProxy = false,
                Proxy = null
            })
            {
                Timeout = Timeout
            };
        }
        #endregion

        #region Public Methods
        public DownloadStatusType GetStatusForFileType(FileType fileType)
        {
            return fileType switch
            {
                FileType.BaseGame => DownloadStatusType.DownloadClient,
                FileType.JarFile => DownloadStatusType.DownloadLibrary,
                FileType.AssetFile => DownloadStatusType.DownloadAssets,
                _ => DownloadStatusType.DownloadJson
            };
        }

        public async Task DownloadFileAsync(DownloadFileInfo info, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
        {
            EnsureDirectoryExists(info.FileName);

            if (info.Size <= (ulong)SmallFileThreshold)
            {
                await DownloadSmallFileWithHttpClient(info, progress, cancellationToken);
            }
            else
            {
                await DownloadLargeFileWithDownloader(info, progress, cancellationToken);
            }
        }

        public async Task Download(CancellationToken cancellationToken = default)
        {
            InitializeDownloadStatistics();
            LogInitialStatistics();

            var semaphore = new SemaphoreSlim(MaxParallelDownloads);
            var lockObj = new object();
            var totalCount = downloadList.Files.Count;

            Console.WriteLine($"Starting download: {totalCount} total files with {MaxParallelDownloads} max concurrent downloads");

            var sortedFiles = downloadList.Files.OrderByDescending(f => f.FileInfo.Size).ToList();
            var downloadTasks = sortedFiles.Select(x => DownloadFileWithRetry(x, semaphore, lockObj, cancellationToken));

            await Task.WhenAll(downloadTasks);
            LogFinalStatistics();

            ReportFinalStatus(totalCount);
        }
        #endregion

        #region Private Core Methods
        private async Task DownloadFileWithRetry(
            DownloadListEntry.DownloadFileItem file,
            SemaphoreSlim semaphore,
            object lockObj,
            CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var stats = _fileTypeStats[file.Type];
                var isSingleFile = stats.TotalFiles == 1;
                var (success, skipped, lastException) = await TryDownloadFile(file, isSingleFile, cancellationToken);

                lock (lockObj)
                {
                    UpdateFileStatistics(file.Type, success, skipped);
                    ReportFileProgress(file, isSingleFile, success, lastException);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task<(bool success, bool skipped, Exception? lastException)> TryDownloadFile(
            DownloadListEntry.DownloadFileItem file,
            bool isSingleFile,
            CancellationToken cancellationToken)
        {
            var success = false;
            var skipped = false;
            Exception? lastException = null;

            if (FileExistsWithCorrectSize(file.FileInfo))
            {
                return (true, true, null);
            }

            for (int attempt = 0; attempt < RetryAttempts; attempt++)
            {
                try
                {
                    if (isSingleFile)
                    {
                        var progress = new Progress<double>(percentage => ReportSingleFileProgress(file, percentage));
                        await DownloadFileAsync(file.FileInfo, progress, cancellationToken);
                    }
                    else
                    {
                        await DownloadFileAsync(file.FileInfo, null, cancellationToken);
                    }
                    return (true, false, null);
                }
                catch (Exception ex) when (attempt < RetryAttempts - 1 && !cancellationToken.IsCancellationRequested)
                {
                    lastException = ex;
                    await Task.Delay(RetryDelay, cancellationToken);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
            }

            return (false, false, lastException);
        }

        private async Task DownloadSmallFileWithHttpClient(
            DownloadFileInfo info,
            IProgress<double>? progress,
            CancellationToken cancellationToken)
        {
            using var response = await _httpClient.GetAsync(info.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? (long)info.Size;
            var receivedBytes = 0L;
            var buffer = new byte[8192];

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var fileStream = new FileStream(info.FileName, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            int bytesRead;
            while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                receivedBytes += bytesRead;
                progress?.Report(receivedBytes * 100.0 / totalBytes);
            }
        }

        private async Task DownloadLargeFileWithDownloader(
            DownloadFileInfo info,
            IProgress<double>? progress,
            CancellationToken cancellationToken)
        {
            var downloadOpt = new DownloadConfiguration()
            {
                ChunkCount = MaxChunksPerFile,
                Timeout = (int)Timeout.TotalMilliseconds,
                RequestConfiguration = new RequestConfiguration()
                {
                    UserAgent = "OverrideLauncher/2.0 (Advanced-Algorithm)",
                    KeepAlive = true,
                    Accept = "*/*"
                },
                ClearPackageOnCompletionWithFailure = true
            };

            using var downloader = new DownloadService(downloadOpt);

            if (progress != null)
            {
                downloader.DownloadProgressChanged += (_, e) => progress.Report(e.ProgressPercentage);
            }

            await downloader.DownloadFileTaskAsync(info.Url, info.FileName, cancellationToken);
        }
        #endregion

        #region Helper Methods
        private void EnsureDirectoryExists(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private bool FileExistsWithCorrectSize(DownloadFileInfo info)
        {
            if (!File.Exists(info.FileName))
                return false;

            var fileInfo = new FileInfo(info.FileName);
            return fileInfo.Length == (long)info.Size;
        }

        private void InitializeDownloadStatistics()
        {
            var filesByType = downloadList.Files.GroupBy(f => f.Type).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var kvp in filesByType)
            {
                var files = kvp.Value;
                var stats = new FileTypeStats { TotalFiles = files.Count };

                foreach (var file in files)
                {
                    if (FileExistsWithCorrectSize(file.FileInfo))
                    {
                        stats.SkippedFiles++;
                    }
                    else
                    {
                        stats.NeedDownloadFiles++;
                    }
                }

                _fileTypeStats[kvp.Key] = stats;
            }
        }

        private void UpdateFileStatistics(FileType fileType, bool success, bool skipped)
        {
            var stats = _fileTypeStats[fileType];
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
            }
        }

        private void ReportFileProgress(
            DownloadListEntry.DownloadFileItem file,
            bool isSingleFile,
            bool success,
            Exception? lastException)
        {
            var stats = _fileTypeStats[file.Type];
            var typeProgress = (double)stats.CompletedFiles / stats.TotalFiles * 100;
            var status = success ? GetStatusForFileType(file.Type) : DownloadStatusType.Error;

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

            if (!success)
            {
                Console.WriteLine($"Download failed: {Path.GetFileName(file.FileInfo.FileName)} - {lastException?.Message}");
            }
        }

        private void ReportSingleFileProgress(DownloadListEntry.DownloadFileItem file, double percentage)
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
        }

        private void LogInitialStatistics()
        {
            foreach (var kvp in _fileTypeStats)
            {
                var stats = kvp.Value;
                Console.WriteLine($"[{kvp.Key}] Total: {stats.TotalFiles}, Need download: {stats.NeedDownloadFiles}, Skipped: {stats.SkippedFiles}");
            }
        }

        private void LogFinalStatistics()
        {
            Console.WriteLine("\n=== Download Statistics ===");
            foreach (var kvp in _fileTypeStats)
            {
                var stats = kvp.Value;
                Console.WriteLine($"[{kvp.Key}] Success: {stats.CompletedFiles}/{stats.TotalFiles} " +
                                $"(Downloaded: {stats.CompletedFiles - stats.SkippedFiles}, " +
                                $"Skipped: {stats.SkippedFiles}), Failed: {stats.FailedFiles}");
            }
        }

        private void ReportFinalStatus(int totalCount)
        {
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
        #endregion

        #region FileTypeStats Class
        public class FileTypeStats
        {
            public int TotalFiles { get; set; }
            public int CompletedFiles { get; set; }
            public int FailedFiles { get; set; }
            public int SkippedFiles { get; set; }
            public int NeedDownloadFiles { get; set; }
        }
        #endregion

        #region IDisposable Implementation
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _httpClient?.Dispose();
            }

            _disposed = true;
        }
        #endregion
    }
}