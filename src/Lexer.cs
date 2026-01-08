public static class Lexer
{
    private static bool inMultiLineComment;
    public static List<List<Token>> Tokenize(string fileName)
    {
        inMultiLineComment = false;

        List<List<Token>> tokens = new();

        StreamReader reader = new(fileName);

        int line = 0;
        while (reader.Peek() >= 0)
        {
            tokens.Add(TokenizeLine(reader.ReadLine()!, line));
            line++;
        }
        // Add EOF token
        tokens.Add(new List<Token>() { new(TokenType.END_OF_FILE) });

        reader.Close();

        return tokens;
    }

    private static List<Token> TokenizeLine(string str, int line)
    {
        List<Token> tokens = new();
        int i = 0;
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
            else if (str[i] == '/' && str[i + 1] == '*') // Multi-line comment
            {
                i += 2;
                inMultiLineComment = true;
            }
            else if (str[i] == '/' && str[i + 1] == '/') // Comment
            {
                tokens.Add(new Token(TokenType.NEWLINE));
                return tokens;
            }
            else if (str[i] == '\\') // Multi-line statement
            {
                return tokens;
            }
            else
                tokens.Add(Tokenize(str, ref i, line));
        }
        tokens.Add(new Token(TokenType.NEWLINE));
        return tokens;
    }
    private static Token Tokenize(string str, ref int i, int line)
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
            if (Token.str2Token.ContainsKey(s))
                return new Token(Token.str2Token[s]);
        }
        return new Token(s);
    }
}