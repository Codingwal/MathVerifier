using System.Diagnostics.CodeAnalysis;

public class Logger
{
    [DoesNotReturn]
    public static void Error(string message)
    {
        throw new($"Error: {message}\n");
        // Console.Write($"Error: {message}\n");
        // Environment.Exit(1);
    }
    public static void Assert(bool condition, string message)
    {
        if (!condition)
            Error(message);
    }
}