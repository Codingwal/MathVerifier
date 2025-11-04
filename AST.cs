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

public struct Invalid
{
};

public struct Equality
{

    public readonly string ToString(int i)
    {
        throw new NotImplementedException();
    }
};

public struct ElementOf
{
    public string set;
    public ElementOf(string set)
    {
        this.set = set;
    }

    public readonly string ToString(int i)
    {
        return ASTUtility.GetPrefix(i) + "Element of \"" + set + "\"\n";
    }
};

public struct Statement
{
    public OneOf<Invalid, ElementOf, Equality> data;

    public Statement()
    {
        data = new Invalid();
    }
    public Statement(OneOf<Invalid, ElementOf, Equality> data)
    {
        this.data = data;
    }

    public readonly string ToString(int i)
    {
        return ASTUtility.GetPrefix(i) + data.Match(
               invalid => "Invalid\n",
               elementOf => elementOf.ToString(0),
               equality => equality.ToString(0)
           );
    }
};

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
};

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
};

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
};