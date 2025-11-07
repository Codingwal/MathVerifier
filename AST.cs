public struct None
{

}
public struct Command
{
    public enum CommandType
    {
        NONE,
        CHECK,
        SORRY,
    }
    public CommandType type;
    public Command(CommandType type)
    {
        this.type = type;
    }
}
public class Term
{
    public Variant<Expression, string, double> term;
    public Term(Variant<Expression, string, double> term)
    {
        this.term = term;
    }
}
public class QuantifiedExpr // It exists... / For all...
{
    public enum QuantifiedExprType
    {
        NONE,
        EXISTS,
        ALL
    }
    public QuantifiedExprType type;
    public List<string> objects;
    public List<Statement> rules;
    public Statement stmt;
    public QuantifiedExpr()
    {
        type = QuantifiedExprType.NONE;
        objects = new();
        rules = new();
        stmt = new();
    }
}
public class BinExpr
{
    public Expression lhs;
    public Token op;
    public Expression rhs;
}
public struct Expression : ICustomFormatting
{
    public Variant<BinExpr, Term, QuantifiedExpr> expr;
    public Expression(Variant<BinExpr, Term, QuantifiedExpr> expr)
    {
        this.expr = expr;
    }
    public readonly string Format(string prefix)
    {
        return Formatter.Format(expr, prefix);
    }
}

public struct Statement
{
    public int line;
    public Variant<Expression, Command> stmt;
    public readonly Expression Expr => stmt.As<Expression>();
    public Statement(int line, Variant<Expression, Command> stmt)
    {
        this.line = line;
        this.stmt = stmt;
    }
}
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
public struct ProvenStatement
{
    public Statement stmt;
    public Variant<FuncCall, Command, None> theorem;
}

public struct Theorem
{
    public string name;
    public List<string> parameters;
    public List<Statement> requirements;
    public Statement hypothesis;
    public List<ProvenStatement> proof;

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
    public List<Statement> rules;
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