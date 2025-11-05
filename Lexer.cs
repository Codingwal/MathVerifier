using TokenType = Token.TokenType;

class Lexer
{
    public static List<List<Token>> Tokenize(string fileName)
    {
        List<List<Token>> tokens = new();

        StreamReader reader = new(fileName);

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

                if (str[i] == '/' && str[i] == '/')
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
                else if (char.IsLetter(str[i]))
                {
                    string literal = "";
                    for (; i < str.Length && char.IsLetter(str[i]); i++)
                        literal += str[i];

                    if (Token.str2Token.ContainsKey(literal))
                        lineTokens.Add(new Token(Token.str2Token[literal]));
                    else
                        lineTokens.Add(new Token(literal));
                }
                else
                {
                    string literal = "";
                    while (i < str.Length && !char.IsWhiteSpace(str[i]) && !char.IsLetterOrDigit(str[i]))
                    {
                        literal += str[i];
                        i++;
                        if (Token.str2Token.ContainsKey(literal))
                        {
                            lineTokens.Add(new Token(Token.str2Token[literal]));
                            break;
                        }
                    }

                    if (!Token.str2Token.ContainsKey(literal))
                        lineTokens.Add(new Token(literal));
                }
            }
            lineTokens.Add(new Token(TokenType.NEWLINE));
            tokens.Add(lineTokens);
        }

        // Add EOF token
        tokens.Add(new List<Token>() { new(TokenType.END_OF_FILE) });

        reader.Close();

        return tokens;
    }
};