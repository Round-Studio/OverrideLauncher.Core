using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Downloader;
using OverrideLauncher.Core.Modules.Enum.Download;

public class GameFileCompleter
{
    private readonly HttpClient _httpClient = new HttpClient();
    public Action<DownloadStateEnum ,string, double> ProgressCallback { get; set; }

    public GameFileCompleter()
    {
    }
    public async Task DownloadMissingFilesAsync(List<FileIntegrityChecker.MissingFile> missingFiles)
    {
        int completedFiles = 0;
        int totalFiles = missingFiles.Count;

        foreach (var file in missingFiles)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file.Path)!);

                switch (file.Type)
                {
                    case FileIntegrityChecker.MissingFile.FileType.LoaderJar:
                    case FileIntegrityChecker.MissingFile.FileType.Jar:
                        // 使用Downloader库进行分片下载
                        var downloadOpt = new DownloadConfiguration()
                        {
                            ChunkCount = 8, // 分片数量，默认值为1
                            ParallelDownload = true // 是否并行下载，默认值为false
                        };
                        DownloadService dow = new DownloadService(downloadOpt);
                        dow.DownloadProgressChanged += (sender, args) =>
                            ProgressCallback?.Invoke(DownloadStateEnum.CompletionJar,
                                $"({completedFiles + 1}/{totalFiles}) {Path.GetFileName(file.Path)})",
                                args.ProgressPercentage);
                        await dow.DownloadFileTaskAsync(file.Url, file.Path);

                        if (!VerifyFileSize(file.Path, file.Size) && file.Type!=FileIntegrityChecker.MissingFile.FileType.LoaderJar)
                        {
                            throw new Exception("Downloaded file size does not match.");
                        }

                        break;

                    case FileIntegrityChecker.MissingFile.FileType.Assets:
                        // 使用HttpClient进行普通下载
                        using (var httpClient = new HttpClient())
                        {
                            ProgressCallback?.Invoke(DownloadStateEnum.CompletionAssets,
                                $"({completedFiles + 1}/{totalFiles})", 0);

                            using (var response =
                                   await httpClient.GetAsync(file.Url, HttpCompletionOption.ResponseHeadersRead))
                            {
                                response.EnsureSuccessStatusCode();

                                long totalBytes = response.Content.Headers.ContentLength ?? 0;
                                long downloadedBytes = 0;

                                using (var stream = await response.Content.ReadAsStreamAsync())
                                using (var fileStream = new FileStream(file.Path, FileMode.Create, FileAccess.Write,
                                           FileShare.None))
                                {
                                    byte[] buffer = new byte[8192];
                                    int bytesRead;
                                    var lastProgressUpdate = DateTime.Now;
                                    double lastPercentage = 0;

                                    while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                                    {
                                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                                        downloadedBytes += bytesRead;

                                        // 更新进度（避免频繁更新UI）
                                        if (DateTime.Now - lastProgressUpdate > TimeSpan.FromMilliseconds(100) ||
                                            downloadedBytes == totalBytes)
                                        {
                                            double percentage =
                                                totalBytes > 0 ? (double)downloadedBytes / totalBytes * 100 : 0;

                                            // 只有当进度变化较大时才更新
                                            if (Math.Abs(percentage - lastPercentage) >= 1 ||
                                                downloadedBytes == totalBytes)
                                            {
                                                ProgressCallback?.Invoke(DownloadStateEnum.CompletionAssets,
                                                    $"({completedFiles + 1}/{totalFiles})",
                                                    percentage
                                                );
                                                lastPercentage = percentage;
                                                lastProgressUpdate = DateTime.Now;
                                            }
                                        }
                                    }
                                }
                            }

                            if (!VerifyFileSize(file.Path, file.Size))
                            {
                                throw new Exception("Downloaded file size does not match.");
                            }
                        }

                        break;

                    default:
                        throw new NotSupportedException($"File type {file.Type} is not supported.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to download {file.Path}: {ex.Message}");
                continue;
            }

            completedFiles++;
            double overallProgressPercentage = (double)completedFiles / totalFiles * 100;
            ProgressCallback?.Invoke(DownloadStateEnum.CompletionSuccess,$"({completedFiles}/{totalFiles})", overallProgressPercentage);
        }
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
}