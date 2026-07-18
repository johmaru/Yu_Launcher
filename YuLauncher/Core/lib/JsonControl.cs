using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace YuLauncher.Core.lib;

public static class JsonControl
{
    public struct ApplicationJsonData : IEquatable<ApplicationJsonData>
    {
        public string FilePath { get; set; }
        
        public string JsonPath { get; set; }
        
        public string Name { get; set; }
        public string? FileExtension { get; set; }
        public string Memo { get; set; }
        public bool? IsWebView { get; set; }
        public bool? IsUseLog { get; set; }
        
        public string Url { get; set;}
        
        public string[] MultipleLaunch { get; set; }
        
        public bool IsMute { get; set; }
        
        public double? Volume { get; set; }
        
        public string[] Genre { get; set; }
        
        public Dictionary<string,string> WikiData { get; set;}

        public bool Equals(ApplicationJsonData other)
        {
            return FilePath == other.FilePath && 
                   JsonPath == other.JsonPath && 
                   Name == other.Name && 
                   FileExtension == other.FileExtension && 
                   Memo == other.Memo && 
                   IsWebView == other.IsWebView && 
                   IsUseLog == other.IsUseLog && 
                   Url == other.Url && 
                   IsMute == other.IsMute &&
                   Genre == other.Genre &&
                   WikiData == other.WikiData &&
                   Volume.Equals(other.Volume) &&
                   MultipleLaunch.Equals(other.MultipleLaunch);
        }

        public override bool Equals(object? obj)
        {
            return obj is ApplicationJsonData other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(FilePath);
            hashCode.Add(JsonPath);
            hashCode.Add(Name);
            hashCode.Add(FileExtension);
            hashCode.Add(Memo);
            hashCode.Add(IsWebView);
            hashCode.Add(IsUseLog);
            hashCode.Add(Url);
            hashCode.Add(IsMute);
            hashCode.Add(MultipleLaunch);
            hashCode.Add(Volume);
            hashCode.Add(Genre);
            hashCode.Add(WikiData);
            return hashCode.ToHashCode();
        }
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
    
    
    
    public static ApplicationJsonData LoadJson(string path)
    {
        if (path == ".json")
        {
            LoggerController.LogWarn("This is not exist an application error. this is not critical error");
        }
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ApplicationJsonData>(json);
    }

    public static ValueTask<bool> CheckAppDataContent(string[] content, string genre)
    {
        foreach (var item in content)
        {
            if (item == genre)
            {
                return new ValueTask<bool>(true);
            }
        }

        return new ValueTask<bool>(false);
    }

    public static async ValueTask CheckJsonData(string jsonPath, ApplicationJsonData data)
    {
        data = data with
        {
            FilePath       = data.FilePath ?? "",
            JsonPath       = data.JsonPath ?? "",
            Name           = data.Name ?? "",
            FileExtension  = data.FileExtension ?? "Unknown",
            Memo           = data.Memo ?? "",
            IsWebView      = data.IsWebView ?? false,
            IsUseLog       = data.IsUseLog ?? false,
            Url            = data.Url ?? "",
            MultipleLaunch = data.MultipleLaunch ?? [],
            WikiData       = data.WikiData ?? new(),
            Genre          = data.Genre ?? (data.FileExtension switch {
                                "exe"      => ["Application"],
                                "web"      => ["WebSite"],
                                "WebGame"  => ["WebGame"],
                                "WebSaver" => ["WebSaver"],
                                _          => ["Unknown"],
                            }),
        };
        await CreateExeJson(jsonPath, data);
    }

}