using OverrideLauncher.Core.Base.Entry.Download;

namespace OverrideLauncher.Core.Interface.Download;

public class Download
{
    #region Static
    public static int DownloadCount = 512;
    #endregion

    #region Public

    public ulong FileCount { get; set; } = 0;
    public ulong DownloadedFileCount { get; set; } = 0;
    
    public async void DownloadFile(DownloadFileInfo info)
    {
        if (info.Size <= 41943040) // 5MB
        {
            DownloadSmallFile(info);
        }else DownloadLargeFile(info);
    }
    #endregion
    
    #region Private

    private async void DownloadSmallFile(DownloadFileInfo info)
    {
        
    }

    private async void DownloadLargeFile(DownloadFileInfo info)
    {
        
    }
    #endregion
}