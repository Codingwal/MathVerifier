using MathVerifier.IR;

namespace MathVerifier.Services;

public static class Loader
{
    private class Context(List<Definition> _definitions)
    {
        private List<Definition> Definitions { get; } = _definitions;
        public List<VarDef?> VarDefs { get; } = [];

        public VarId AddVarDef(VarDef? def)
        {
            if (def == null)
            {
                VarDefs.Add(null);
                return new VarId((uint)VarDefs.Count - 1);
            }

            int index = VarDefs.FindIndex(d => d != null && Comparer.IsEqual(d, def));
            if (index != -1) return new VarId((uint)index);

            VarDefs.Add(def);
            return new VarId((uint)VarDefs.Count - 1);
        }
        public Definition? GetDefinition(string name)
        {
            return Definitions.FirstOrDefault(d => d.Name == name);
        }
    }

    public static Data Update(Data data)
    {
        List<Definition> definitions = [];
        foreach (var def in data.Defs)
        {
            Logger.Info($"Definition \"{def.Name}\" has {def.ParamCount} parameters and {def.Defs.Count} definitions before update");
            List<VarDef?> defs = UpdateDefs(def.Defs, definitions);
            Logger.Info($"Definition \"{def.Name}\" has {def.ParamCount} parameters and {defs.Count} definitions after update");
            definitions.Add(new Definition(def.Name, def.ParamCount, defs!, def.Def));
        }

        List<Theorem> theorems = [];
        foreach (var theorem in data.Theorems)
        {
            List<VarDef?> defs = UpdateDefs(theorem.Defs, definitions);
            theorems.Add(new Theorem(theorem.Name, theorem.ParamCount, defs!, theorem.Requirements, theorem.ProofStmts, theorem.Hypothesis));
        }

        return new Data(definitions, theorems);
    }

    private static List<VarDef?> UpdateDefs(List<VarDef?> defs, List<Definition> definitions)
    {
        Context context = new(definitions);

        for (int i = 0; i < defs.Count; i++)
        {
            if (defs[i] == null)
            {
                context.AddVarDef(null);
                continue;
            }

            if (defs[i] is FuncVarDef funcDef)
                context.AddVarDef(UpdateFuncVarDef(funcDef, context));
            else
                throw new NotImplementedException();
        }

        return context.VarDefs;
    }
    private static FuncVarDef UpdateFuncVarDef(FuncVarDef def, Context context)
    {
        Var func = ReplaceVar(def.Function, context);

        List<VarRef> argRefs = [];
        foreach (var argRef in def.Args)
        {
            Var arg = ReplaceVar(argRef.Var, context);
            argRefs.Add(new VarRef(arg, argRef.Args));
        }

        return new FuncVarDef(func, argRefs, def.ArgsCount);
    }
    private static Var ReplaceVar(Var var, Context context)
    {
        if (var is UserVar userVar)
            return ReplaceUserVar(userVar, context);
        else
            return var;
    }
    private static VarId ReplaceUserVar(UserVar userVar, Context context)
    {
        Definition? definition = context.GetDefinition(userVar.Name);
        Logger.Assert(definition != null, $"Undefined variable \"{userVar.Name}\"");

        Dictionary<VarId, Var> idMap = [];

        for (uint i = 0; i < definition!.ParamCount; i++)
            idMap.Add(new VarId(i), new GenericVar(i));

        Logger.Info($"Processing definition \"{definition.Name}\" with {definition.ParamCount} parameters and {definition.Defs.Count} definitions");
        for (int i = 0; i < definition!.Defs.Count; i++)
        {
            Logger.Info($"Processing definition {i} in \"{definition.Name}\"");

            if (definition.Defs[i] == null) continue;

            VarId id = context.AddVarDef(RewriteVarDef(definition.Defs[i]!, idMap));
            idMap.Add(new VarId((uint)i), id);
        }

        return (VarId)idMap[definition.Def];
    }

    private static VarDef RewriteVarDef(VarDef def, Dictionary<VarId, Var> idMap)
    {
        if (def is FuncVarDef funcDef)
            return RewriteFuncVarDef(funcDef, idMap);
        else
            throw new NotImplementedException();
    }
    private static FuncVarDef RewriteFuncVarDef(FuncVarDef def, Dictionary<VarId, Var> idMap)
    {
        Var func = RewriteVar(def.Function, idMap);

        List<VarRef> argRefs = [];
        foreach (var argRef in def.Args)
        {
            Var arg = RewriteVar(argRef.Var, idMap);
            argRefs.Add(new VarRef(arg, argRef.Args));
        }

        return new FuncVarDef(func, argRefs, def.ArgsCount);
    }
    private static Var RewriteVar(Var var, Dictionary<VarId, Var> idMap)
    {
        if (var is VarId id)
            return idMap[id];
        else
            return var;
    }
}