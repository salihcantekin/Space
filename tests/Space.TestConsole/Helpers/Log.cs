namespace Space.TestConsole;

public static class Log
{
    public static void Add(string logMessage)
    {
        logMessage = $"[{DateTime.Now:HH:mm:ss.fff}] {logMessage}";
        Console.WriteLine(logMessage);
    }
}
