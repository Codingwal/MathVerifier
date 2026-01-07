class Lexer
{
    public static List<List<Token>> Tokenize(string fileName)
    {
        List<List<Token>> tokens = new();

        StreamReader reader = new(fileName);

        int line = 0;
        while (reader.Peek() >= 0)
        {
            string str = reader.ReadLine()!;

            List<Token> lineTokens = new();
            int i = 0;
            while (i < str.Length)
            {
                if (char.IsWhiteSpace(str[i]))
                {
                    i++;
                    continue;
                }

                if (str[i] == '/' && str[i] == '/') // Comment
                {
                    break;
                }
                else if (char.IsNumber(str[i])) // Number
                {
                    string numStr = "";
                    while (i < str.Length && (char.IsNumber(str[i]) || str[i] == '.'))
                    {
                        numStr += str[i];
                        i++;
                    }
                    double num = double.Parse(numStr);
                    lineTokens.Add(new Token(num));
                }
                else if (char.IsLetter(str[i]) || str[i] == '_') // String
                {
                    string literal = "";
                    for (; i < str.Length && (char.IsLetterOrDigit(str[i]) || str[i] == '_'); i++)
                        literal += str[i];

                    if (Token.str2Token.ContainsKey(literal))
                        lineTokens.Add(new Token(Token.str2Token[literal]));
                    else
                        lineTokens.Add(new Token(literal));
                }
                else // Symbol
                {
                    string literal = "";
                    for (; i < str.Length && !char.IsWhiteSpace(str[i]) && !char.IsLetterOrDigit(str[i]) && str[i] != '_'; i++)
                        literal += str[i];

                    int j = 0;
                    for (int k = literal.Length; j < k;)
                    {
                        if (Token.str2Token.ContainsKey(literal[j..k]))
                        {
                            lineTokens.Add(new Token(Token.str2Token[literal[j..k]]));
                            j = k;
                            k = literal.Length;
                        }
                        else
                            k--;
                    }
                    if (j != literal.Length)
                        Logger.Error($"Invalid symbol \"{literal[j..]}\" in line {line + 1}");
                }
            }
            lineTokens.Add(new Token(TokenType.NEWLINE));
            tokens.Add(lineTokens);
            line++;
        }
        // Add EOF token
        tokens.Add(new List<Token>() { new(TokenType.END_OF_FILE) });

        reader.Close();

        return tokens;
    }
}