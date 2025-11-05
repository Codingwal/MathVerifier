public static class MathVerifier
{
    public static void Main(string[] args)
    {
        if (args.Length < 1)
            Logger.Error("Missing name of file to parse");
        string fileName = args[0];

        Console.WriteLine("Tokenizing...\n");
        var tokens = Lexer.Tokenize(fileName);
        Console.WriteLine("Finished Tokenizing\n");

        // Print tokens
        Console.WriteLine("\n-------------------------");
        Console.Write(Formatter.Format(tokens));
        Console.WriteLine("-------------------------\n");

        Console.WriteLine("Parsing...\n");
        Parser parser = new(tokens);
        var ast = parser.Parse();
        Console.WriteLine("Finished Parsing\n");

        // Print AST
        Console.WriteLine("\n-------------------------");
        Console.Write(Formatter.Format(ast));
        Console.WriteLine("-------------------------\n");
    }
}