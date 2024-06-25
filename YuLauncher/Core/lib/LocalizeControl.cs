using System.Reflection;
using WPFLocalizeExtension.Extensions;

namespace YuLauncher.Core.lib;

public static class LocalizeControl
{
    public static T GetLocalize<T>(string key)
    {
        return LocExtension.GetLocalizedValue<T>(Assembly.GetCallingAssembly().GetName().Name + ":Language:" + key);
    }
}