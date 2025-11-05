using OneOf;

public class Term
{
    public OneOf<Expression, string, double> term;
    public Term(OneOf<Expression, string, double> term)
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
    public OneOf<BinExpr, Term, QuantifiedExpr> expr;
    public Expression(OneOf<BinExpr, Term, QuantifiedExpr> expr)
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
    public Expression expr;
    public Statement(Expression expr)
    {
        this.expr = expr;
    }
}

public struct Theorem
{
    public string name;
    public List<string> parameters;
    public List<Statement> requirements;
    public Statement hypothesis;
    public List<Statement> proof;

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
    public List<OneOf<Theorem, Definition>> data;
    public Data()
    {
        data = new();
    }
}