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
        int totalFiles = missingFiles.Count;
        int completedFiles = 0;
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = 512 // 设置最大并行度
        };

        await Parallel.ForEachAsync(missingFiles, parallelOptions, async (file, cancellationToken) =>
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
                            ParallelDownload = true
                        };
                        DownloadService dow = new DownloadService(downloadOpt);
                        await dow.DownloadFileTaskAsync(file.Url, file.Path);

                        if (!VerifyFileSize(file.Path, file.Size) &&
                            file.Type != FileIntegrityChecker.MissingFile.FileType.LoaderJar)
                        {
                            throw new Exception("Downloaded file size does not match.");
                        }

                        break;

                    case FileIntegrityChecker.MissingFile.FileType.Assets:
                        // 使用HttpClient进行普通下载
                        using (var httpClient = new HttpClient())
                        {
                            using (var response =
                                   await httpClient.GetAsync(file.Url, HttpCompletionOption.ResponseHeadersRead))
                            {
                                response.EnsureSuccessStatusCode();

                                using (var stream = await response.Content.ReadAsStreamAsync())
                                using (var fileStream = new FileStream(file.Path, FileMode.Create, FileAccess.Write,
                                           FileShare.None))
                                {
                                    await stream.CopyToAsync(fileStream);
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
                return; // 注意这里使用return而不是continue
            }

            // 使用Interlocked.Increment保证线程安全的计数
            int currentCount = Interlocked.Increment(ref completedFiles);
            double overallProgressPercentage = (double)currentCount / totalFiles * 100;
            ProgressCallback?.Invoke(DownloadStateEnum.CompletionSuccess, $"({currentCount}/{totalFiles})",
                overallProgressPercentage);
        });
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