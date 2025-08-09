using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace OverrideLauncher.Core.Classes.Utilities;

public class ZipUtil
{
    public static void UnZip(string zipFilePath, string outputPath)
    {
        // 确保目标目录存在
        Directory.CreateDirectory(outputPath);
    
        // 解压整个 ZIP 文件
        ZipFile.ExtractToDirectory(zipFilePath, outputPath, true);
    }
    public static void AddJsonToZip(string zipFilePath, string jsonEntryName, object data)
    {
        // 打开或创建 ZIP 文件
        using (ZipArchive archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Update))
        {
            // 创建或替换 ZIP 中的条目
            ZipArchiveEntry jsonEntry = archive.CreateEntry(jsonEntryName);
        
            // 将对象序列化为 JSON 并写入 ZIP 条目
            using (Stream stream = jsonEntry.Open())
            {
                JsonSerializer.Serialize(stream, data, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
            }
        }
    }
    public static string ReadFileContentFromZip(string zipFilePath, string filePathInZip)
    {
        using (ZipArchive archive = ZipFile.OpenRead(zipFilePath))
        {
            ZipArchiveEntry entry = archive.GetEntry(filePathInZip);

            if (entry == null)
                throw new FileNotFoundException($"文件 {filePathInZip} 不在 {Path.GetFileName(zipFilePath)} 包中");

            using (Stream stream = entry.Open())
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
    }
}