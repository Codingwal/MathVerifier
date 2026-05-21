namespace MathVerifier.IR;

public abstract record Var();

public record VarId(uint Value) : Var;
public record UserVar(string Name) : Var;
public record GenericVar(uint Value) : Var;
public record StandardVar(StandardVar.Values Value) : Var
{
    public enum Values { IMPLIES, AND, OR, EQUALS, ELEMENT_OF, TRUE, FALSE, ALL }
}

public record VarRef(Var Var, List<GenericVar> Args);

public abstract record VarDef(int ArgsCount);

public record FuncVarDef(Var Function, List<VarRef> Args, int ArgsCount) : VarDef(ArgsCount);
public record LoadVarDef(string Origin, int ArgsCount) : VarDef(ArgsCount);

public record Theorem(
    string Name,
    int ParamCount,
    List<VarDef> Defs,
    List<VarId> Requirements,
    List<VarId> ProofStmts,
    VarId Hypothesis
);

public record Definition(
    string Name,
    int ParamCount,
    List<VarDef> Defs,
    VarId Def
);

public record Data(
    List<Variant<Theorem, Definition>> Blocks
);