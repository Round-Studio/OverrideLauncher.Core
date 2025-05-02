namespace OverrideLauncher.Core.Modules.Classes;

public class FileHelper
{
    public static string GetJarFilePath(string name)
    {
        // 分隔依赖字符串
        string[] parts = name.Split(':');
        if (parts.Length < 3 || parts.Length > 4)
        {
            throw new ArgumentException("Invalid Maven dependency format. Expected format: groupId:artifactId:version[:classifier]");
        }

        string groupId = parts[0];
        string artifactId = parts[1];
        string version = parts[2];
        string classifier = parts.Length == 4 ? parts[3] : "jar"; // 默认为 jar

        // 替换 groupId 中的 . 为 /
        groupId = groupId.Replace('.', '/');

        // 构造路径
        string path = $"{groupId}/{artifactId}/{version}/{artifactId}-{version}";
        if (!string.IsNullOrEmpty(classifier) && classifier != "jar")
        {
            path += $"-{classifier}";
        }
        path += ".jar";

        return path;
    }
}