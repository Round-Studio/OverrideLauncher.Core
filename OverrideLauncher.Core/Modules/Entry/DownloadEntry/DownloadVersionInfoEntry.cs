namespace OverrideLauncher.Core.Modules.Entry.DownloadEntry;

public class DownloadVersionInfoEntry
{
    public GameJsonEntry GameJsonEntry { get; set; }
    public GameVersion Version { get; set; }
    public string InstallName { get; set; } = string.Empty;
    public string VersionAssetsJsonURL { get; set; }
}