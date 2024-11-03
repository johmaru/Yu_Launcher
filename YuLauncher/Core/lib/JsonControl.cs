using System;
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
            return hashCode.ToHashCode();
        }
    }
    
    public static async ValueTask CreateExeJson(string path,ApplicationJsonData applicationJsonData)
    {
        JsonSerializerOptions options = new() { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)};
        string json = JsonSerializer.Serialize(applicationJsonData, options);
        await File.WriteAllTextAsync(path, json);
    }
    
    public static async Task<ApplicationJsonData> ReadExeJson(string path)
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

    public static async ValueTask CheckJsonData(string jsonPath,ApplicationJsonData data)
    {
        switch (data)
        {
            case { FilePath: null }:
                data = data with { FilePath = "" };
                    await CreateExeJson(jsonPath, data);
                    break;
            
            case {JsonPath: null}:
                data = data with { JsonPath = "" };
                await CreateExeJson(jsonPath, data);
                break;

            case {Name:  null}:
                data = data with { Name = "" };
                await CreateExeJson(jsonPath, data);
                break;

            case { FileExtension: null }:
                data = data with { FileExtension = "Unknown" };
                await CreateExeJson(jsonPath, data);
                break;

            case { Memo: null }:
                data = data with { Memo = "" };
                await CreateExeJson(jsonPath, data);
                break;

            case { IsWebView: null }:
                data = data with { IsWebView = false };
                await CreateExeJson(jsonPath, data);
                break;

            case { IsUseLog: null }:
                data = data with { IsUseLog = false };
                await CreateExeJson(jsonPath, data);
                break;

            case { Url: null }:
                data = data with { Url = "" };
                await CreateExeJson(jsonPath, data);
                break;

            case { MultipleLaunch: null }:
                data = data with { MultipleLaunch = new string[0] };
                await CreateExeJson(jsonPath, data);
                break;
        }

    }

}