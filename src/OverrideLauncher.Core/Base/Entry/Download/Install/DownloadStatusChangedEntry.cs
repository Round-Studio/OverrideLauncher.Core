using OverrideLauncher.Core.Base.Enum;
using OverrideLauncher.Core.Base.Enum.Download;

namespace OverrideLauncher.Core.Base.Entry.Download.Install;

public class DownloadStatusChangedEntry
{
    public DownloadStatusType Status { get; set; }
    public double Progress { get; set; }
    public FileType FileType { get; set; }
    public string CurrentFileName { get; set; }
    public int CompletedFiles { get; set; }
    public int TotalFiles { get; set; }
}