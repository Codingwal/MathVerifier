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

public struct DefinitionStatement
{
    public string obj;
    public Expression stmt;
}

// High-level

public struct ExpressionLine
{
    public Expression expr;
    public int line;
}
public struct Scope
{
    public List<StatementLine> statements;
    public Scope()
    {
        statements = new();
    }
}
public struct ConditionalStatement
{
    public ExpressionLine condition;
    public Scope ifScope;
    public Scope elseScope;
    public Scope bothScope;
}
public struct StatementLine
{
    public int line;
    public Variant<Expression, Command, DefinitionStatement, ConditionalStatement> stmt;
    public Variant<FuncCall, string, Command>? proof; // <theorem ref, definition ref, "sorry">
}

public struct Theorem
{
    public string name;
    public List<string> parameters;
    public List<ExpressionLine> requirements;
    public ExpressionLine hypothesis;
    public Scope proof;
    public int line;
    public Theorem()
    {
        name = "";
        parameters = new();
        requirements = new();
        hypothesis = new();
        proof = new();
        line = -1;
    }
}

public struct Definition
{
    public string name;
    public List<ExpressionLine> rules;
    public int line;
    public Scope proof; // Proof that such an object exists
    public Definition()
    {
        name = "";
        rules = new();
        line = -1;
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
