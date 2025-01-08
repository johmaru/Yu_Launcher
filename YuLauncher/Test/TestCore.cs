using System.Threading.Tasks;

namespace YuLauncher.Test;

public class TestCore
{
    public static bool IsDebug { get; private set; }
    
    public static ValueTask Initialize()
    {
        IsDebug = IsDebugMode();
        return ValueTask.CompletedTask;;
    }
    
    public ValueTask Test()
    {
        return ValueTask.CompletedTask;
    }
    
    
    private static bool IsDebugMode()
    {
#if DEBUG
        return true;
#else
        return false;
#endif
    }
}