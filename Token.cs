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
        EQUIVALENT,
        AND,
        OR,

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
        {"∈", TokenType.ELEMENT_OF},
        {"⊆", TokenType.SUBSET},

        // Relational operators
        {"=", TokenType.EQUALS},

        // Logical operators
        {"⇒", TokenType.IMPLIES},
        {"⇔", TokenType.EQUIVALENT},
        {"∧", TokenType.AND},
        {"∨", TokenType.OR},

        // Quantified operators
        {"∃", TokenType.EXISTS},
        {"∀", TokenType.FOR_ALL},

        // Binary operators
        {"+", TokenType.PLUS},
        {"-", TokenType.MINUS},
        {"⋅", TokenType.STAR},
        {"÷", TokenType.BACKSLASH},

        // Misc
        {",", TokenType.COMMA},
        {";", TokenType.SEMICOLON},
        {":", TokenType.COLON},
        {"|", TokenType.PIPE},

        // Keywords
        {"theorem", TokenType.THEOREM},
        {"define", TokenType.DEFINE},
        {"let", TokenType.LET},
        {"end", TokenType.END},

        // Commands
        {"check", TokenType.CHECK},
        {"sorry", TokenType.SORRY},
    };

    /// <remarks> Returns -1 if the token is not an operator </remarks>
    public const int ExpressionMinPrec = 10;
    public static int GetPrecedence(TokenType type)
    {
        return type switch
        {
            // Stmt x Stmt => Stmt
            TokenType.IMPLIES => 1,
            TokenType.OR => 2,
            TokenType.AND => 3,

            // Expr x Expr => Stmt
            TokenType.ELEMENT_OF => 5,
            TokenType.SUBSET => 5,
            TokenType.EQUALS => 5,

            // Expr x Expr => Expr
            TokenType.PLUS => 10,
            TokenType.MINUS => 10,
            TokenType.STAR => 12,
            TokenType.BACKSLASH => 12,
            TokenType.STRING => 100, // Operator object
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

    public static bool operator ==(Token a, Token b)
    {
        return a.Equals(b);
    }
    public static bool operator !=(Token a, Token b)
    {
        return !(a == b);
    }

    public override bool Equals(object? obj)
    {
        if (obj == null) return false;
        if (obj is Token token)
            return Equals(token);
        else
            return false;
    }
    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}