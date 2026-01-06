global using Expression = Variant<BinExpr, Term>;
using TokenType = Token.TokenType;

// Miscellaneous
public enum Command
{
    NONE,
    CHECK,
    SORRY,
}

// Expressions
public struct FuncCall
{
    public string name;
    public List<Expression> args;
    public FuncCall()
    {
        name = "";
        args = new();
    }
}
public struct BinExpr
{
    public Expression lhs;
    public Token op;
    public Expression rhs;
}
public struct Term
{
    public Variant<Expression, FuncCall, QuantifiedStatement, string, double> term;
    public Term(Variant<Expression, FuncCall, QuantifiedStatement, string, double> term)
    {
        this.term = term;
    }
}

// Statements
public struct QuantifiedStatement
{
    public TokenType op;
    public string obj;
    public Expression stmt;
}

// High-level
public struct StatementLine
{
    public int line;
    public Variant<Expression, Command> stmt;
    public Variant<FuncCall, Command>? proof;
}

public struct Theorem
{
    public string name;
    public List<string> parameters;
    public List<StatementLine> requirements;
    public StatementLine hypothesis;
    public List<StatementLine> proof;
    public Theorem()
    {
        name = "";
        parameters = new();
        requirements = new();
        hypothesis = new();
        proof = new();
    }
}

public struct Definition
{
    public string name;
    public List<StatementLine> rules;
    public Definition()
    {
        name = "";
        rules = new();
    }
}

public struct Data
{
    public List<Variant<Theorem, Definition>> data;
    public Data()
    {
        data = new();
    }
}