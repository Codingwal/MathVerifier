global using Expression = Variant<BinExpr, Term>;

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
public struct UnaryExpr
{
    public Token op;
    public Expression expr;
}
public struct Term
{
    public Variant<Expression, FuncCall, QuantifiedStatement, string, UnaryExpr> term;
    public Term(Variant<Expression, FuncCall, QuantifiedStatement, string, UnaryExpr> term)
    {
        this.term = term;
    }
}

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
    public Variant<FuncCall, string, Command>? proof; // <theorem ref, definition ref, "sorry">
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