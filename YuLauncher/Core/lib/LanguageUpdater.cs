using System.Globalization;
using WPFLocalizeExtension.Engine;

namespace YuLauncher.Core.lib;

public class LanguageUpdater
{
    public static void UpdateLanguage(string lang)
    {
        var cultureInfo = new CultureInfo(lang);
        LocalizeDictionary.Instance.Culture = cultureInfo;
    }

}