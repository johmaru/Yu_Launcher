using System.IO;

namespace YuLauncher.Core.lib;

public abstract class FileControl
{
   public struct Main
    {
        public const string Directory = "./Games";
    }
    
    public static bool ExistGameDirectory(string path)
    {
        return Directory.Exists(path);
    }
    
    public static bool ExistGameFile(string path)
    {
        return File.Exists(path);
    }
    
    public static string[] GetGameList()
    {
        return Directory.GetFiles(Main.Directory);
    }
    
    public static void DeleteGame(string path)
    {
        File.Delete(path);
    }
    
    
}