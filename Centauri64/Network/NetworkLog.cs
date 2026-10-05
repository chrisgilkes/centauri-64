using System.Diagnostics;

namespace Centauri64.Network;

public static class NetworkLog
{
    public static bool Verbose { get; set; }

    public static void Info(string message)
    {
        Debug.WriteLine($"[NET] {message}");
        System.Console.WriteLine($"[NET] {message}");
    }

    public static void VerboseMessage(string message)
    {
        if (!Verbose)
            return;

        Debug.WriteLine($"[NET] {message}");
        System.Console.WriteLine($"[NET] {message}");
    }
}
