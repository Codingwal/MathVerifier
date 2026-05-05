namespace MathVerifier.Flat;

public record VarId(uint Value, bool Generic)
{
    // public static implicit operator VarId(uint value) => new(value, );
    // public static implicit operator uint(VarId value) => !value.Generic ? value.Value : throw new();
}


public abstract record VarDefinition(int ArgsCount);

public record VarRef(VarId Id, List<VarId> Args);
public record FunctionVarDefinition(VarId Function, List<VarRef> Args, int ArgsCount) : VarDefinition(ArgsCount);
public record LoadVarDefinition(string Origin, int ArgsCount) : VarDefinition(ArgsCount);