using System.Text.Json;
using System.Text.Json.Serialization;
using OverrideLauncher.Core.Modules.Entry.DownloadEntry.DownloadAssetsEntry;

namespace OverrideLauncher.Core.Modules.Classes.Download.Assets.CurseForge;

public class CurseForgeSearch
{
    public static async Task<CurseForgeSearchResponse> Search(CurseForgeSearchInfo Info)
    {
        Dictionary<string, dynamic> Args = new Dictionary<string, dynamic>()
        {
            ["&gameVersion"] = Info.GameVersion,
            ["&searchFilter"] = Info.SearchName,
            ["&pageSize"] = Info.PageSize,
            ["&index"] = Info.Index,
            ["&modLoader"] = Info.ModLoader,
            ["&classId"] = Info.ClassID
        };
        var bodyUrl = "";
        foreach (var keyValuePair in Args)
        {
            if (keyValuePair.Value != null)
            {
                bodyUrl += $"{keyValuePair.Key}={keyValuePair.Value}";
            }
        }
        
        string root = "https://api.curseforge.com";
        
        // 搜索参数
        var baseUrl = $"{root}/v1/mods/search?gameId=432";
        
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("x-api-key", Info.ApiKey);
            
            // 构建搜索URL
            string searchUrl = $"{baseUrl}{bodyUrl}";
            
            // 发送GET请求
            HttpResponseMessage response = await client.GetAsync(searchUrl);
            
            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
                    PropertyNameCaseInsensitive = true
                };

                // 反序列化 JSON 响应
                var body = JsonSerializer.Deserialize<CurseForgeSearchResponse>(responseBody, options);
                // 这里可以反序列化为对象进行处理
                return body;
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}");
                return null;
            }
        }
    }
    public static async Task<CurseForgeFeaturedResponse> GetFeatured(string ApiKey)
    {
        string root = "https://api.curseforge.com";
        
        // 搜索参数
        var baseUrl = $"{root}/v1/mods/featured?gameId=432";
        
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("x-api-key", ApiKey);
            
            // 发送GET请求
            HttpResponseMessage response = await client.GetAsync(baseUrl);
            
            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
                    PropertyNameCaseInsensitive = true
                };

                // 反序列化 JSON 响应
                var body = JsonSerializer.Deserialize<CurseForgeFeaturedResponse>(responseBody, options);
                // 这里可以反序列化为对象进行处理
                return body;
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}");
                return null;
            }
        }
    }
}