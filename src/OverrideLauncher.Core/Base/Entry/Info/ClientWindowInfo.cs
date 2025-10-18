namespace OverrideLauncher.Core.Base.Entry.Info;

public class ClientWindowInfo
{
    public int Width { get; set; } = 800;
    public int Height { get; set; } = 480;

    public ClientWindowInfo GetWindowSize(WindowInfo info)
    {
        switch (info)
        {
            case WindowInfo.w800h480:
                Width = 800;
                Height = 480;
                break;
            case WindowInfo.w1920h1080:
                Width = 1920;
                Height = 1080;
                break;
        }

        return this;
    }
    
    public enum WindowInfo
    {
        w800h480,
        w1920h1080
    }
}