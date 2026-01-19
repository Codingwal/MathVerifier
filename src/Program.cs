public static class Program
{
    public static void Main(string[] args)
    {
        if (args.Length < 1)
            Logger.Error("Missing name of file to parse");
        string fileName = args[0];

        // Tokenize
        Console.WriteLine("Tokenizing...");
        var tokens = Lexer.Tokenize(fileName);
        Console.WriteLine("Finished tokenizing");

        // Print tokens
        // Console.WriteLine("\n-------------------------");
        // Console.Write(Formatter.Format(tokens));
        // Console.WriteLine("-------------------------\n");

        // Parse
        Console.WriteLine("Parsing...");
        Parser parser = new(tokens);
        var ast = parser.Parse();
        Console.WriteLine("Finished parsing");

        // Print AST
        // Console.WriteLine("\n-------------------------");
        // Console.Write(Formatter.Format(ast));
        // Console.WriteLine("-------------------------\n");

        // Check syntax
        Console.WriteLine("Checking syntax...");
        SyntaxChecker checker = new(ast);
        checker.Check();
        Console.WriteLine("Finished checking syntax");

        // Verify
        Console.WriteLine("Verifying...");
        Verifier verifier = new(ast);
        verifier.Verify();
        Console.WriteLine("Finished verifying");
    }
}