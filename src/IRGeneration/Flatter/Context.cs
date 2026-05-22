using MathVerifier.AST;
using MathVerifier.IR;

namespace MathVerifier.Services.Flatter;

public class Context
{
    public readonly List<VarDef> VarDefs = [];
    private readonly Stack<string> QStmtVars = [];
    public readonly List<string> Params = [];

    // Add a variable definition and returns its id. Prevents duplicates.
    public VarId AddVarDef(VarDef def)
    {
        int index = VarDefs.FindIndex(d => d != null && Comparer.IsEqual(d, def));
        if (index != -1) return new VarId((uint)index);

        VarDefs.Add(def);
        return new VarId((uint)VarDefs.Count - 1);
    }

    public void AddParam(string param)
    {
        Logger.Assert(!Params.Contains(param), $"Duplicate parameter {param}");
        Params.Add(param);
    }

    public bool IsParam(string var)
    {
        return Params.Contains(var);
    }
    public bool IsGeneric(string var)
    {
        return QStmtVars.Contains(var);
    }

    public void PushQStmtVar(string var) => QStmtVars.Push(var);
    public void PopQStmtVar() => QStmtVars.Pop();
}

public class TheoremContext : Context
{
    public List<Expression> Requirements = [];
    public readonly List<IStatement> Statements = [];
}