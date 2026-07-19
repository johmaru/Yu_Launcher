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
    public record struct ApplicationJsonData
    {
        public long Id { get; set; }
        public string FilePath { get; set; }
        public string JsonPath { get; set; }
        public string Name { get; set; }
        public string? FileExtension { get; set; }
        public string Memo { get; set; }
        public bool? IsWebView { get; set; }
        public bool? IsUseLog { get; set; }
        public string Url { get; set; }
        public string[] MultipleLaunch { get; set; }
        public bool IsMute { get; set; }
        public double? Volume { get; set; }
        public string[] Genre { get; set; }
        public Dictionary<string, string> WikiData { get; set; }
    }
    
    public static ValueTask CreateExeJson(string path, ApplicationJsonData applicationJsonData)
    {
        var dataWith = applicationJsonData with { JsonPath = path };
        if (GameRepository.ExistsByJsonPath(path))
            GameRepository.UpdateGame(dataWith);
        else
            GameRepository.InsertGame(dataWith);
        return ValueTask.CompletedTask;
    }

    public static ValueTask<ApplicationJsonData> ReadExeJson(string path)
    {
        return new ValueTask<ApplicationJsonData>(GameRepository.GetByJsonPath(path) ?? default);
    }

    public static ApplicationJsonData LoadJson(string path)
    {
        return GameRepository.GetByJsonPath(path) ?? default;
    }
}