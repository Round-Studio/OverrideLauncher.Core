using OverrideLauncher.Core.Modules.Entry.LaunchEntry;

namespace OverrideLauncher.Core.Modules.Enum.Launch;

public static class ClientWindowSizeEnum
{
    public static readonly GameWindowInfo Default = new GameWindowInfo()
    {
        Height = 520,
        Width = 900
    };
    
    public static readonly GameWindowInfo Fullscreen = new GameWindowInfo()
    {
        Height = 1080,
        Width = 1920
    };
}