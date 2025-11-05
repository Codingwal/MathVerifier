using System.Diagnostics;
using OneOf;

public struct Token : ICustomFormatting
{
    public enum TokenType
    {
        UNDEFINED,
        NEWLINE,
        END_OF_FILE,

        // Literals
        STRING,
        NUMBER,

        // Symbols
        BRACKET_OPEN,
        BRACKET_CLOSE,
        CURLY_OPEN,
        CURLY_CLOSE,
        EQUALS,
        EXISTS,
        ALL,
        ELEMENT_OF,
        IMPLIES,
        SEMICOLON,
        COMMA,
        COLON,
        PLUS,
        MINUS,
        STAR,
        BACKSLASH,

        // Keywords
        THEOREM,
        DEFINE,
        LET,
        IS,
        END,
    };

    public static Dictionary<string, TokenType> str2Token = new()
    {
        {"(", TokenType.BRACKET_OPEN},
        {")", TokenType.BRACKET_CLOSE},
        {"{", TokenType.CURLY_OPEN},
        {"}", TokenType.CURLY_CLOSE},
        {"=", TokenType.EQUALS},
        {"E", TokenType.EXISTS},
        {"A", TokenType.ALL},
        {"e", TokenType.ELEMENT_OF},
        {"=>", TokenType.IMPLIES},
        {";", TokenType.SEMICOLON},
        {",", TokenType.COMMA},
        {":", TokenType.COLON},
        {"+", TokenType.PLUS},
        {"-", TokenType.MINUS},
        {"*", TokenType.STAR},
        {"/", TokenType.BACKSLASH},

        {"theorem", TokenType.THEOREM},
        {"define", TokenType.DEFINE},
        {"let", TokenType.LET},
        {"is", TokenType.IS},
        {"end", TokenType.END},
    };

    /// <remarks> Returns -1 if the token is not an operator </remarks>
    public static int GetPrecedence(TokenType type)
    {
        return type switch
        {
            TokenType.IMPLIES => 0,

            TokenType.ELEMENT_OF => 2,
            TokenType.EQUALS => 2,

            TokenType.PLUS => 3,
            TokenType.MINUS => 3,

            TokenType.STRING => 4, // Operator object

            TokenType.STAR => 5,
            TokenType.BACKSLASH => 5,

            _ => -1,
        };
    }

    public TokenType type;
    public OneOf<string, double> data;

    public Token(TokenType type)
    {
        Debug.Assert(type != TokenType.STRING);
        Debug.Assert(type != TokenType.NUMBER);
        this.type = type;
    }
    public Token(string str)
    {
        type = TokenType.STRING;
        data = str;
    }
    public Token(double num)
    {
        type = TokenType.NUMBER;
        data = num;
    }

    public readonly string GetString()
    {
        Debug.Assert(type == TokenType.STRING);
        return data.AsT0;
    }
    public readonly double GetDouble()
    {
        Debug.Assert(type == TokenType.NUMBER);
        return data.AsT1;
    }

    public override readonly string ToString()
    {
        string str = type.ToString();
        if (type == TokenType.STRING)
            str += " (\"" + GetString() + "\")";
        else if (type == TokenType.NUMBER)
            str += " (" + GetDouble() + ")";
        return str;
    }

    public readonly string Format(string prefix)
    {
        return $"{prefix}{ToString()}\n";
    }
}