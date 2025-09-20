namespace OverrideLauncher.Core.Base.Entry.Info.Java;

public class JavaInfo
{
    public string Path { get; set; }
    public bool IsGC { get; set; } = true;
    public int MemorySize { get; set; } = 1024;
}