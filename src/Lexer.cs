public static class Lexer
{
    public static List<TokenLine> Tokenize(string fileName)
    {
        bool inMultiLineComment = false;

        Logger.Assert(File.Exists(fileName), $"The file \"{fileName}\" does not exist.");

        StreamReader reader = new(fileName);

        List<TokenLine> tokens = [];

        int line = 1;
        while (reader.Peek() >= 0)
        {
            string lineString = reader.ReadLine()!;

            LineInfo lineInfo = new(line, fileName);

            bool include = HandleInclude(lineString, lineInfo, ref tokens);

            if (!include)
                tokens.Add(TokenizeLine(lineString, lineInfo, ref inMultiLineComment));

            line++;
        }
        // Add EOF token
        tokens.Add(new([new(TokenType.END_OF_FILE)], new(line, fileName)));

        reader.Close();

        return tokens;
    }
    private static bool HandleInclude(string str, LineInfo lineInfo, ref List<TokenLine> tokens)
    {
        if (!str.StartsWith("#include"))
            return false;

        int i = 0;

        // Consume the "#include" string and the whitespace afterwards
        i += "#include".Length;
        Logger.Assert(char.IsWhiteSpace(str[i]), $"Expected space after \"#include\" ({lineInfo})");
        i++;

        string fileName = str[i..];

        List<TokenLine> fileTokens = Tokenize(fileName);

        foreach (TokenLine tokenLine in fileTokens)
        {
            if (tokenLine.tokens[0].type != TokenType.END_OF_FILE)
                tokens.Add(tokenLine);
        }

        return true;
    }
    private static TokenLine TokenizeLine(string str, LineInfo lineInfo, ref bool inMultiLineComment)
    {
        int i = 0;

        TokenLine tokenLine = new([], lineInfo);

        while (i < str.Length)
        {
            if (inMultiLineComment)
            {
                if (str[i] == '*' && str[i + 1] == '/')
                {
                    i += 2;
                    inMultiLineComment = false;
                }
                else
                    i++;
            }
            else if (char.IsWhiteSpace(str[i]))
            {
                i++;
            }
            else if (str[i] == '/' && i + 1 < str.Length && str[i + 1] == '*') // Multi-line comment
            {
                i += 2;
                inMultiLineComment = true;
            }
            else if (str[i] == '/' && i + 1 < str.Length && str[i + 1] == '/') // Comment
            {
                break;
            }
            else if (str[i] == '\\') // Multi-line statement
            {
                return tokenLine;
            }
            else
            {
                tokenLine.tokens.Add(Tokenize(str, ref i, lineInfo));
            }
        }
        tokenLine.tokens.Add(new Token(TokenType.NEWLINE));
        return tokenLine;
    }
    private static Token Tokenize(string str, ref int i, LineInfo lineInfo)
    {
        // Single-char symbol? (':', '|', '∃', '⇒', ...)
        if (Token.str2Token.ContainsKey(str[i].ToString()))
            return new Token(Token.str2Token[str[i++].ToString()]);

        string s = "";
        while (i < str.Length && !char.IsWhiteSpace(str[i]))
        {
            // Break if a single-char symbol has been reached (will be tokenized on its one the next iteration)
            if (Token.str2Token.ContainsKey(str[i].ToString()))
                break;

            s += str[i];
            i++;

            // Check if the string is a known keyword
            if (Token.str2Token.TryGetValue(s, out TokenType type))
                return new Token(type);
        }
        return new Token(s);
    }
}