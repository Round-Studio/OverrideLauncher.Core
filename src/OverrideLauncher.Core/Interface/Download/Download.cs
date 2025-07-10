using Downloader;
using OverrideLauncher.Core.Base.Entry.Download;

namespace OverrideLauncher.Core.Interface.Download;

public class Download
{
    #region Static
    public static int DownloadThreadCount = 512;
    #endregion

    #region Public

    public ulong FileCount { get; set; } = 0;
    public ulong DownloadedFileCount { get; set; } = 0;
    
    public void DownloadFile(DownloadFileInfo info)
    {
        if (info.Size <= 41943040) // 5MB
        {
            DownloadSmallFile(info);
        }else DownloadLargeFile(info);
    }
    #endregion
    
    #region Private

    private void DownloadSmallFile(DownloadFileInfo info)
    {
        DownloadService download = new DownloadService();
        download.DownloadFileTaskAsync(info.Url, info.FileName).Wait();
    }

    private void DownloadLargeFile(DownloadFileInfo info)
    {
        DownloadService download = new DownloadService();
        download.DownloadFileTaskAsync(info.Url, info.FileName).Wait();
    }
    #endregion
}