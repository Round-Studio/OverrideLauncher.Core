using System.Text.Json;
using System.Text.Json.Serialization;
using OverrideLauncher.Core.Modules.Entry.DownloadEntry.DownloadAssetsEntry;

namespace OverrideLauncher.Core.Modules.Classes.Download.Assets.CurseForge;

public class CurseForgeSearch
{
    public static async Task<CurseForgeSearchResponse> Search(CurseForgeSearchInfo Info)
    {
        string baseUrl = "https://api.curseforge.com";
        
        // 搜索参数
        var gameName = Info.GameVersion;
        string searchFilter = Info.SearchName;
        int pageSize = Info.PageSize;
        
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("x-api-key", Info.ApiKey);
            
            // 构建搜索URL
            string searchUrl = $"{baseUrl}/v1/mods/search?gameId=432&gameVersion={gameName}&searchFilter={searchFilter}&pageSize={pageSize}&index={Info.Index}&modLoader={Info.ModLoader}";
            
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
}