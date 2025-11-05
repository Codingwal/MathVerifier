using OneOf;

static class ASTUtility
{
    public static string GetPrefix(int indentation)
    {
        string str = "";
        for (int i = 0; i < indentation; i++)
            str += " | ";
        return str;
    }
}

public class Term
{
    public OneOf<Expression, string, double> term;
    public Term(OneOf<Expression, string, double> term)
    {
        this.term = term;
    }
    public string ToString(int i)
    {
        return term.Match(
             expr => expr.ToString(i),
             str => $"{ASTUtility.GetPrefix(i)}\"{str}\"\n",
             num => ASTUtility.GetPrefix(i) + num.ToString() + "\n"
        );
    }
}
public class BinExpr
{
    public Expression lhs;
    public Token op;
    public Expression rhs;
    public string ToString(int i)
    {
        string str = ASTUtility.GetPrefix(i) + "BinExpr:\n";
        string prefix = ASTUtility.GetPrefix(i + 1);
        str += prefix + "lhs:\n" + lhs.ToString(i + 2);
        str += prefix + $"operator: {op}\n";
        str += prefix + "rhs:\n" + rhs.ToString(i + 2);
        return str;
    }
}
public struct Expression
{
    public OneOf<BinExpr, Term> expr;
    public Expression(OneOf<BinExpr, Term> expr)
    {
        this.expr = expr;
    }
    public readonly string ToString(int i)
    {
        return expr.Match(
            binExpr => binExpr.ToString(i),
            term => term.ToString(i)
        );
    }
}

public struct Statement
{
    public Expression expr;
    public Statement(Expression expr)
    {
        this.expr = expr;
    }
    public readonly string ToString(int i)
    {
        return expr.ToString(i);
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
    public readonly string ToString(int i)
    {
        string str = ASTUtility.GetPrefix(i) + "Theorem:\n";
        string prefix = ASTUtility.GetPrefix(i + 1);

        str += prefix + "name: " + name + "\n";

        str += prefix + "params:\n";
        foreach (var param in parameters)
            str += ASTUtility.GetPrefix(i + 2) + param + "\n";

        str += prefix + "requirements:\n";
        foreach (var stmt in requirements)
            str += stmt.ToString(i + 2);

        str += prefix + "hypothesis:\n" + hypothesis.ToString(i + 2);

        str += prefix + "proof:\n";
        foreach (var stmt in proof)
            str += stmt.ToString(i + 2);

        return str;
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
    public readonly string ToString(int i)
    {
        string str = ASTUtility.GetPrefix(i) + "Definition:\n";
        string prefix = ASTUtility.GetPrefix(i + 1);

        str += prefix + "name: " + name + "\n";

        str += prefix + "obj:\n" + ASTUtility.GetPrefix(i + 2) + obj + "\n";

        str += prefix + "params:\n";
        foreach (var param in parameters)
            str += ASTUtility.GetPrefix(i + 2) + param + "\n";

        str += prefix + "rules:\n";
        foreach (var stmt in rules)
            str += stmt.ToString(i + 2);

        return str;
    }
}

public struct Data
{
    public List<OneOf<Theorem, Definition>> data;
    public Data()
    {
        data = new();
    }

    public override readonly string ToString()
    {
        string str = "Data:\n";
        foreach (var variant in data)
        {
            str += variant.Match(
                theorem => theorem.ToString(1),
                definition => definition.ToString(1)
            );
        }
        return str;
    }
}