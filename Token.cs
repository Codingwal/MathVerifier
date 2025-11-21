using System.Diagnostics;

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

        // Brackets
        BRACKET_OPEN,
        BRACKET_CLOSE,
        CURLY_OPEN,
        CURLY_CLOSE,

        // Set statements
        ELEMENT_OF,
        SUBSET,

        // Relational operators
        EQUALS,

        // Logical operators
        IMPLIES,

        // Quantified statements
        EXISTS,
        FOR_ALL,

        // Binary operators
        PLUS,
        MINUS,
        STAR,
        BACKSLASH,

        // Misc
        SEMICOLON,
        COLON,
        PIPE,
        COMMA,

        // Keywords
        THEOREM,
        DEFINE,
        END,
        LET,

        // Commands
        CHECK,
        SORRY,
    };

    public static Dictionary<string, TokenType> str2Token = new()
    {
        // Brackets
        {"(", TokenType.BRACKET_OPEN},
        {")", TokenType.BRACKET_CLOSE},
        {"{", TokenType.CURLY_OPEN},
        {"}", TokenType.CURLY_CLOSE},

        // Set statements
        {"e", TokenType.ELEMENT_OF},
        {"is", TokenType.SUBSET},

        // Relational operators
        {"=", TokenType.EQUALS},

        // Logical operators
        {"=>", TokenType.IMPLIES},

        // Quantified operators
        { "E", TokenType.EXISTS},
        {"A", TokenType.FOR_ALL},

        // Binary operators
        {"+", TokenType.PLUS},
        {"-", TokenType.MINUS},
        {"*", TokenType.STAR},
        {"/", TokenType.BACKSLASH},

        // Misc
        {",", TokenType.COMMA},
        {";", TokenType.SEMICOLON},
        {":", TokenType.COLON},
        {"|", TokenType.PIPE},

        // Keywords
        { "theorem", TokenType.THEOREM},
        {"define", TokenType.DEFINE},
        {"let", TokenType.LET},
        {"end", TokenType.END},

        // Commands
        { "check", TokenType.CHECK},
        {"sorry", TokenType.SORRY},
    };

    /// <remarks> Returns -1 if the token is not an operator </remarks>
    public const int ExpressionMinPrec = 10;
    public static int GetPrecedence(TokenType type)
    {
        return type switch
        {
            // Statements

            TokenType.IMPLIES => 0,

            TokenType.ELEMENT_OF => 2,
            TokenType.EQUALS => 2,

            // Expressions

            TokenType.PLUS => 10,
            TokenType.MINUS => 10,

            TokenType.STRING => 11, // Operator object

            TokenType.STAR => 12,
            TokenType.BACKSLASH => 12,

            _ => -1,
        };
    }

    public TokenType type;
    public Variant<string, double> data;

    public Token(TokenType type)
    {
        Debug.Assert(type != TokenType.STRING);
        Debug.Assert(type != TokenType.NUMBER);
        this.type = type;
        data = new();
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
        return data.As<string>();
    }
    public readonly double GetDouble()
    {
        Debug.Assert(type == TokenType.NUMBER);
        return data.As<double>();
    }

    public readonly bool Equals(Token other)
    {
        return type == other.type && data.Equals(other.data);
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