using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
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
        public string FileExtension { get; set; }
        public string Memo { get; set; }
        public bool IsWebView { get; set; }
        public bool IsUseLog { get; set; }
        
        public string Url { get; set;}
    }
    
    public static async ValueTask CreateExeJson(string path,ApplicationJsonData applicationJsonData)
    {
        JsonSerializerOptions options = new() { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };
        string json = JsonSerializer.Serialize(applicationJsonData, options);
        await File.WriteAllTextAsync(path, json);
    }
    
    public static async ValueTask<ApplicationJsonData> ReadExeJson(string path)
    {
        string json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<ApplicationJsonData>(json);
    }

}