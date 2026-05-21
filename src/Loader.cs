using MathVerifier.IR;

namespace MathVerifier.Services;

public static class Loader
{
    private class Context(List<Definition> _definitions)
    {
        private List<Definition> Definitions { get; } = _definitions;
        private List<VarDef> VarDefs { get; } = [];

        public VarId AddVarDef(VarDef def)
        {
            int index = VarDefs.FindIndex(d => Comparer.IsEqual(d, def));
            if (index != -1) return new VarId((uint)index);

            VarDefs.Add(def);
            return new VarId((uint)VarDefs.Count - 1);
        }
        public Definition? GetDefinition(string name)
        {
            return Definitions.FirstOrDefault(d => d.Name == name);
        }
    }

    public static void Update(Data data)
    {
        List<Definition> definitions = data.Defs;
        foreach (var def in data.Defs)
            UpdateDefs(def.Defs, definitions);

        foreach (var theorem in data.Theorems)
            UpdateDefs(theorem.Defs, definitions);
    }

    private static void UpdateDefs(List<VarDef> defs, List<Definition> definitions)
    {
        Context context = new(definitions);

        for (int i = 0; i < defs.Count; i++)
        {
            if (defs[i] is FuncVarDef funcDef)
                defs[i] = UpdateFuncVarDef(funcDef, context);
            else
                throw new NotImplementedException();
        }
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

        Dictionary<VarId, VarId> idMap = [];
        for (int i = 0; i < definition!.Defs.Count; i++)
        {
            VarId id = context.AddVarDef(RewriteVarDef(definition.Defs[i], idMap));
            idMap.Add(new VarId((uint)i), id);
        }

        return idMap[definition.Def];
    }

    private static VarDef RewriteVarDef(VarDef def, Dictionary<VarId, VarId> idMap)
    {
        if (def is FuncVarDef funcDef)
            return RewriteFuncVarDef(funcDef, idMap);
        else
            throw new NotImplementedException();
    }
    private static FuncVarDef RewriteFuncVarDef(FuncVarDef def, Dictionary<VarId, VarId> idMap)
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
    private static Var RewriteVar(Var var, Dictionary<VarId, VarId> idMap)
    {
        if (var is VarId id)
            return idMap[id];
        else
            return var;
    }
}