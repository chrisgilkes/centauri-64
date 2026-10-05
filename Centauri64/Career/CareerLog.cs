using System.Diagnostics;

namespace Centauri64.Career;

public static class CareerLog
{
    public static void Info(string message)
    {
        Debug.WriteLine($"[Career] {message}");
        System.Console.WriteLine($"[Career] {message}");
    }
}
