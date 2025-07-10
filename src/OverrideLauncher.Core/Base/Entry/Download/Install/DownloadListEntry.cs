using OverrideLauncher.Core.Base.Enum;

namespace OverrideLauncher.Core.Base.Entry.Download.Install;

public class DownloadListEntry
{
    public List<DownloadFileItem> Files { get; set; } = new();

    public class DownloadFileItem
    {
        public DownloadFileInfo FileInfo { get; set; }
        public FileType Type { get; set; }
    }
}