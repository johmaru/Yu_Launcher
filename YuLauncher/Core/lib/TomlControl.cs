using System;
using System.Collections.Generic;
using System.IO;
using Tommy;

namespace YuLauncher.Core.lib;

public class TomlControl
{
   
    public static void CreateToml(string path)
    {
        TomlTable table = new TomlTable
        {
            ["Language"] = "en",
            ["FullScreen"] = "false",
            ["AutoUpdate"] = "false",
            ["Theme"] = "Dark",
            ["GameResolution"] =
            {
                ["Width"] = 1920,
                ["Height"] = 1080
            },
            ["WebViewResolution"] =
            {
                ["Width"] = 1280,
                ["Height"] = 720
            },
            ["WindowResolution"] =
            {
                ["Width"] = 800,
                ["Height"] = 400
            },
            ["SettingResolution"] =
            {
                ["Width"] = 800,
                ["Height"] = 500
            },
            ["MemoResolution"] =
            {
                ["Width"] = 600,
                ["Height"] = 200
            },
            ["MemoFontSize"] = 20,
            ["DividerColor"] = "#8B5CF6"
        };
        using (StreamWriter writer = File.CreateText(path))
        {
            table.WriteTo(writer);
            writer.Flush();
        }
    }

    public static void CreateGameListToml(string path)
    {
        TomlTable table = new TomlTable
        {
            ["GameList"] =
            {
                ["testgame"] = "test"
            }
        };
        using (StreamWriter writer = File.CreateText(path))
        {
            table.WriteTo(writer);
            writer.Flush();
        }
    }

    public static void EditToml(string path,string dat,string value)
    {
        try
        {
            using (StreamReader reader = new StreamReader(File.OpenRead($"{path}")))
            {
                TomlTable table = TOML.Parse(reader);
                table[dat] = value;
                using (StreamWriter writer = new StreamWriter(File.OpenWrite($"{path}")))
                {
                    table.WriteTo(writer);
                    writer.Flush();
                }
            }
        }
        catch (Exception e)
        {
            LoggerController.LogError($"{e}");
            throw;
        }
    }
    
    public static void EditToml(string path, string dat,string dat2, string value)
    {
        try
        {
            using (StreamReader reader = new StreamReader(File.OpenRead($"{path}")))
            {
                TomlTable table = TOML.Parse(reader);
                table[dat][dat2] = value;
                using (StreamWriter writer = new StreamWriter(File.OpenWrite($"{path}")))
                {
                    table.WriteTo(writer);
                    writer.Flush();
                }
            }
        }
        catch (Exception e)
        {
            LoggerController.LogError($"{e}");
            throw;
        }
    }

    public static string GetToml(string path, string key)
    {
        try
        {
            using (StreamReader reader = new StreamReader(File.OpenRead($"{path}")))
            {
                TomlTable table = TOML.Parse(reader);
                return table[key].ToString();
            }
        }
        catch (Exception e)
        {
            LoggerController.LogError($"{e}");
            throw;
        }
    }

    public static string GetTomlString(string path, string key, string list)
    {
        try
        {
            using (StreamReader reader = new StreamReader(File.OpenRead($"{path}")))
            {
                TomlTable table = TOML.Parse(reader);
                return table[key][list].ToString();
            }
        }
        catch (Exception e)
        {
            LoggerController.LogError($"{e}");
            throw;
        }
    }

    public static string GetTomlString(string path, string key)
    {
        try
        {
            using (StreamReader reader = new StreamReader(File.OpenRead($"{path}")))
            {
                TomlTable table = TOML.Parse(reader);
                return table[key].ToString();
            }
        }
        catch (Exception e)
        {
            LoggerController.LogError($"{e}");
            throw;
        }
    }

    /// <summary>
    /// 指定したキーが存在する場合は値を取得する。
    /// キー不在時は例外を投げず false を返す（TomlLazy センチネル問題を回避）。
    /// </summary>
    public static bool TryGetString(string path, string key, out string value)
    {
        value = "";
        try
        {
            using StreamReader reader = new StreamReader(File.OpenRead($"{path}"));
            TomlTable table = TOML.Parse(reader);
            if (!table.HasKey(key))
                return false;
            value = table[key].ToString();
            return true;
        }
        catch (Exception e)
        {
            LoggerController.LogError($"{e}");
            return false;
        }
    }

}

public class ManualTomlSettings
{
    public static string GetSettingWindowResolution(string path, string data, string res)
    {
        try
        {
            using (StreamReader reader = new StreamReader(File.OpenRead($"{path}")))
            {
                TomlTable table = TOML.Parse(reader);
                return table[data][res];
            }
        }
        catch (Exception e)
        {
            LoggerController.LogError($"{e}");
            throw;
        }
    }
}
    