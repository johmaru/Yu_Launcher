using System.IO;
using System.Linq;

namespace YuLauncher.Core.lib;

public abstract class FileControl
{
   public struct Main
    {
        public const string Directory = "./Games";
        public const string Settings = "./settings.toml";
    }
   
    public static void CopyDirectory(string sourceDir, string destDir)
    {
        if (!Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }
        
       Directory.GetFiles(sourceDir).ToList().ForEach(x =>
        {
            string destFileName = Path.Combine(destDir, Path.GetFileName(x));
            File.Copy(x, destFileName, true);
        });
       
        Directory.GetDirectories(sourceDir).ToList().ForEach(subDir =>
        {
            string destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
            CopyDirectory(subDir, destSubDir);
        });
    }
}