namespace MathVerifier.IR;

public interface IStatement;

public abstract record Var();

public record VarId(uint Value) : Var, IStatement;
public record UserVar(string Name) : Var;
public record GenericVar(uint Value) : Var;
public record StandardVar(StandardVar.Values Value) : Var
{
    public enum Values { IMPLIES, AND, OR, EQUALS, ELEMENT_OF, NOT, TRUE, FALSE, ALL }
}

public record VarRef(Var Var, List<GenericVar> Args);

public abstract record VarDef(int ArgsCount);
public record FuncVarDef(Var Function, List<VarRef> Args, int ArgsCount) : VarDef(ArgsCount);

// TODO: A theorem call doesn't need args (?)
public record TheoremRef(string Name, List<Var> Args) : IStatement;


public record Theorem(
    string Name,
    List<VarDef> Defs,
    List<IStatement> ProofStmts,
    VarId Hypothesis
);

public record Definition(
    string Name,
    int ParamCount,
    List<VarDef> Defs,
    VarId Def
);

public record Data(
    List<Definition> Defs,
    List<Theorem> Theorems
);


public static class Comparer
{
    public static bool IsEqual(Var a, Var b)
    {
        if (a.GetType() != b.GetType()) return false;

        return a switch
        {
            VarId varIdA => varIdA.Value == ((VarId)b).Value,
            UserVar userVarA => userVarA.Name == ((UserVar)b).Name,
            GenericVar genericVarA => genericVarA.Value == ((GenericVar)b).Value,
            StandardVar stdVarA => stdVarA.Value == ((StandardVar)b).Value,
            _ => throw new()
        };
    }
    public static bool IsEqual(VarRef a, VarRef b)
    {
        if (!IsEqual(a.Var, b.Var)) return false;
        if (a.Args.Count != b.Args.Count) return false;
        for (int i = 0; i < a.Args.Count; i++)
            if (!IsEqual(a.Args[i], b.Args[i])) return false;
        return true;
    }
    public static bool IsEqual(VarDef a, VarDef b)
    {
        if (a.GetType() != b.GetType()) return false;

        return a switch
        {
            FuncVarDef funcDefA => IsEqual(funcDefA, (FuncVarDef)b),
            _ => throw new()
        };
    }
    public static bool IsEqual(FuncVarDef a, FuncVarDef b)
    {
        if (!IsEqual(a.Function, b.Function)) return false;
        if (a.Args.Count != b.Args.Count) return false;
        for (int i = 0; i < a.Args.Count; i++)
            if (!IsEqual(a.Args[i], b.Args[i])) return false;
        return true;
    }
}