using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

public class GameFileCompleter
{
    private readonly HttpClient _httpClient = new HttpClient();
    public Action<string, double> ProgressCallback { get; set; }

    public GameFileCompleter()
    {
    }

    public async Task CompleteFilesAsync(List<FileIntegrityChecker.MissingFile> missingFiles)
    {
        if (missingFiles == null || missingFiles.Count == 0)
        {
            ProgressCallback?.Invoke("No files to complete.", 100);
            return;
        }

        int totalFiles = missingFiles.Count;
        int completedFiles = 0;

        foreach (var file in missingFiles)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file.Path)!);

                using (var response = await _httpClient.GetAsync(file.Url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    long totalBytes = response.Content.Headers.ContentLength ?? 0;
                    long downloadedBytes = 0;

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(file.Path, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        byte[] buffer = new byte[8192];
                        int bytesRead;
                        while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                        {
                            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                            downloadedBytes += bytesRead;

                            // Update progress for the current file
                            double fileProgressPercentage = totalBytes > 0 ? (double)downloadedBytes / totalBytes * 100 : 0;
                            ProgressCallback?.Invoke($"Downloading {Path.GetFileName(file.Path)}: {fileProgressPercentage:0.##}%", fileProgressPercentage);
                        }
                    }
                }

                if (!VerifyFileSize(file.Path, file.Size))
                {
                    throw new Exception("Downloaded file size does not match.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to download {file.Path}: {ex.Message}");
                continue;
            }

            completedFiles++;
            double overallProgressPercentage = (double)completedFiles / totalFiles * 100;
            ProgressCallback?.Invoke($"Completing files ({completedFiles}/{totalFiles})", overallProgressPercentage);
        }

        ProgressCallback?.Invoke("Files completion complete.", 100);
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