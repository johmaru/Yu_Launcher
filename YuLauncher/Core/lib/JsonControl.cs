using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace YuLauncher.Core.lib;

public static class JsonControl
{
    public struct ApplicationJsonData
    {
        public string FilePath { get; set; }
        
        public string JsonPath { get; set; }
        
        public string Name { get; set; }
        public string? FileExtension { get; set; }
        public string Memo { get; set; }
        public bool IsWebView { get; set; }
        public bool IsUseLog { get; set; }
        
        public string Url { get; set;}
        
        public string[] MultipleLaunch { get; set; }
    }
    
    public static async ValueTask CreateExeJson(string path,ApplicationJsonData applicationJsonData)
    {
        JsonSerializerOptions options = new() { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)};
        string json = JsonSerializer.Serialize(applicationJsonData, options);
        await File.WriteAllTextAsync(path, json);
    }
    
    public static async ValueTask<ApplicationJsonData> ReadExeJson(string path)
    {
        if (path == ".json")
        {
            LoggerController.LogWarn("This is not exist an application error. this is not critical error");
        }
        string json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<ApplicationJsonData>(json);
    }

    public static async ValueTask CheckJsonData(string jsonPath,ApplicationJsonData data)
    {
        if (data.MultipleLaunch == null)
        {
            data = data with { MultipleLaunch = new string[0] };
            await CreateExeJson(jsonPath,data);
        }
    }

}