using OverrideLauncher.Core.Base.Enum.Download;

namespace OverrideLauncher.Core.Base.Entry.Download.Install;

public class DownloadStatusChangedEntry
{
    public DownloadStatusType Status { get; set; }
    public double Progress { get; set; }
}