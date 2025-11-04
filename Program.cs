public static class MathVerifier
{
    public static void Main(string[] args)
    {
        if (args.Length < 1)
            Logger.Error("Missing name of file to parse");
        string fileName = args[0];

        Console.WriteLine("Tokenizing...\n");
        var tokens = Tokenizer.Tokenize(fileName);
        Console.WriteLine("Finished Tokenizing\n");

        // Print tokens
        // Console.WriteLine("-------------------------");
        // Console.WriteLine("Tokens:");
        // foreach (var lineTokens in tokens)
        // {
        //     foreach (var token in lineTokens)
        //         Console.WriteLine("Token: " + token.ToString());
        // }
        // Console.WriteLine("-------------------------");

        Console.WriteLine("Parsing...\n");
        Parser parser = new(tokens);
        var ast = parser.Parse();
        Console.WriteLine("Finished Parsing\n");

        // Print AST
        Console.WriteLine("-------------------------");
        Console.Write(ast.ToString());
        Console.WriteLine("-------------------------");
    }
}