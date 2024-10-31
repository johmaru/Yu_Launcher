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
                ["Height"] = 400
            },
            ["MemoResolution"] =
            {
                ["Width"] = 600,
                ["Height"] = 200
            },
            ["MemoFontSize"] = 20
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
            Console.WriteLine(e);
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
            Console.WriteLine(e);
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
                return table[key];
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
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
                return table[key][list];
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
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
                return table[key];
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
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
            Console.WriteLine(e);
            throw;
        }
    }
}
    