using System.Diagnostics;

namespace Centauri64.Session;

/// <summary>
/// Gates internal developer UI. Never shown in Release without a debugger.
/// </summary>
public static class DeveloperTools
{
#if DEBUG
    public static bool Available => true;
#else
    public static bool Available => Debugger.IsAttached;
#endif
}
