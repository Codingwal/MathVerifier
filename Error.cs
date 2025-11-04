public class Logger
{
    public static void Error(string message)
    {
        throw new($"Error: {message}\n");
        // Console.Write($"Error: {message}\n");
        // Environment.Exit(1);
    }
}