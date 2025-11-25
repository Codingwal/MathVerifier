global using Statement = Variant<QuantifiedStatement, LogicalOperator, RelationalOperator, SetStatement, string>;
global using Term = Variant<Expression, FuncCall, string, double>;

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
public struct Expression
{
    public Variant<BinExpr, Term> expr;
    public Expression(Variant<BinExpr, Term> expr)
    {
        this.expr = expr;
    }
}

// Statements
public struct SetStatement
{
    public Expression lhs;
    public TokenType op;
    public Expression rhs;
}
public struct RelationalOperator
{
    public Expression lhs;
    public TokenType op;
    public Expression rhs;
}
public struct LogicalOperator
{
    public Statement lhs;
    public TokenType op;
    public Statement rhs;
}
public struct QuantifiedStatement
{
    public TokenType op;
    public List<string> objects;
    public Statement stmt;
    public QuantifiedStatement()
    {
        op = TokenType.UNDEFINED;
        objects = new();
        stmt = new();
    }
}

// High-level
public struct StatementLine
{
    public int line;
    public Variant<Statement, Command> stmt;
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
    public string obj;
    public List<string> parameters;
    public List<StatementLine> rules;
    public Definition()
    {
        name = "";
        obj = "";
        parameters = new();
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