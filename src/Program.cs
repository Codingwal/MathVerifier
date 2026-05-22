namespace MathVerifier;

using MathVerifier.Services;

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

        // Flatten
        Console.WriteLine("Flattening...");
        var ir = Flatter.Flatten(ast);
        Console.WriteLine("Finished flattening");

        // Print IR1
        Console.WriteLine("\n---------------------------\n");
        Console.WriteLine(IRPrinter.Data2Str(ir));
        Console.WriteLine("-------------------------\n");

        // Run reference resolver
        Console.WriteLine("Executing reference resolver...");
        ir = ReferenceResolver.Resolve(ir);
        Console.WriteLine("Finished reference resolver");

        // Print IR2
        Console.WriteLine("\n---------------------------\n");
        Console.WriteLine(IRPrinter.Data2Str(ir));
        Console.WriteLine("-------------------------\n");
    }
}