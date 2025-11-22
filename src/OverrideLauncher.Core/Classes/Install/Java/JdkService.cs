using System.Text.Json;
using OverrideLauncher.Core.Base.Entry.Info.Java;

namespace OverrideLauncher.Core.Classes.Install.Java;

public class JdkService
{
    private static readonly HttpClient _httpClient = new HttpClient();
    
    public async Task<JdkItem> GetJdksAsync()
    {
        try
        {
            string url = "https://download.jetbrains.com/jdk/feed/v1/jdks.json";
            
            // 设置 User-Agent 头（有些服务器需要）
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; OverrideLauncher.Core/2.x; JavaService)");
            
            // 获取 JSON 数据
            string json = await _httpClient.GetStringAsync(url);
            
            // 配置 JSON 反序列化选项
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
            
            // 反序列化 JSON 到对象列表
            JdkItem jdks = JsonSerializer.Deserialize<JdkItem>(json, options);
            
            return jdks;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching JDKs: {ex.Message}");
            throw;
        }
    }
}