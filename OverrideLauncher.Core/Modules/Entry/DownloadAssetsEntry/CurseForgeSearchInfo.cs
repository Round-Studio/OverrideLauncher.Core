namespace OverrideLauncher.Core.Modules.Entry.DownloadEntry.DownloadAssetsEntry;

public class CurseForgeSearchInfo
{
    public string ApiKey { get; set; }
    public string SearchName { get; set; }
    public string GameVersion { get; set; }
    public int GameID { get; set; }
    public int PageSize { get; set; } = 50;
    public int Index { get; set; } = 0;
}