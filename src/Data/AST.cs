global using Expression = Variant<BinExpr, Term>;

// Miscellaneous
public enum Command
{
    NONE,
    CHECK,
    SORRY,
}

// Expressions
public struct FuncCall()
{
    public string name = "";
    public List<Expression> args = [];
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
public struct QuantifiedStatement
{
    public TokenType op;
    public string obj;
    public Expression stmt;
}
public struct Tuple()
{
    public List<Expression> elements = [];
}

public struct Term(Variant<Expression, FuncCall, QuantifiedStatement, string, UnaryExpr, Tuple> term)
{
    public Variant<Expression, FuncCall, QuantifiedStatement, string, UnaryExpr, Tuple> term = term;
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
public struct Scope()
{
    public List<StatementLine> statements = [];
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

public struct Theorem()
{
    public string name = "";
    public List<string> parameters = [];
    public List<ExpressionLine> requirements = [];
    public ExpressionLine hypothesis = new();
    public Scope proof = new();
    public int line = -1;
}

public struct Definition()
{
    public string name = "";
    public List<ExpressionLine> rules = [];
    public int line = -1;
    public Scope proof = new(); // Proof that such an object exists
}

public struct Data()
{
    public List<Variant<Theorem, Definition>> data = [];
}
