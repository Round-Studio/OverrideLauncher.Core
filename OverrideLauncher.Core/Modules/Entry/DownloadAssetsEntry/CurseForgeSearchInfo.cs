namespace OverrideLauncher.Core.Modules.Entry.DownloadEntry.DownloadAssetsEntry;

public class CurseForgeSearchInfo
{
    public string ApiKey { get; set; }
    public string SearchName { get; set; }
    public string GameVersion { get; set; }
    public int GameID { get; set; }
    public int PageSize { get; set; } = 50;
    public int Index { get; set; } = 0;
    public int ModLoader { get; set; } = 0;
    public int ClassID { get; set; }
}

public static class CurseForgeSearchClassID
{
    public const int Mod = 6;
    public const int Modpacks = 4471;
    public const int ResourcePacks = 12;
    public const int LightAndShadowPacks = 6552;
}