using MathVerifier.IR;

namespace MathVerifier;

public static class ReferenceResolver
{
    private class Context(List<VarDef?> _varDefs)
    {
        public List<VarDef?> VarDefs = _varDefs;
        public Dictionary<VarId, VarId> mapping = [];

        public VarId AddVarDef(VarDef def)
        {
            // if (def == null)
            // {
            //     VarDefs.Add(null);
            //     return new VarId((uint)VarDefs.Count - 1);
            // }

            int index = VarDefs.FindIndex(d => d != null && Comparer.IsEqual(d, def));
            if (index != -1) return new VarId((uint)index);

            VarDefs.Add(def);
            return new VarId((uint)VarDefs.Count - 1);
        }
    }

    public static Data Resolve(Data data)
    {
        List<Definition> definitions = [];
        foreach (var def in data.Defs)
        {
            Resolve(def.Defs, definitions);
            definitions.Add(def);
        }

        List<Theorem> theorems = [];
        foreach (var theorem in data.Theorems)
        {
            Resolve(theorem.Defs, definitions);
            theorems.Add(theorem);
        }

        return new Data(definitions, theorems);
    }

    private static void Resolve(List<VarDef?> varDefs, List<Definition> definitions)
    {
        HashSet<string> defRefs = [];
        foreach (var varDef in varDefs)
        {
            if (varDef is FuncVarDef funcDef)
            {
                if (funcDef.Function is UserVar userVar)
                    defRefs.Add(userVar.Name);

                foreach (var arg in funcDef.Args)
                {
                    if (arg.Var is UserVar userVarArg)
                        defRefs.Add(userVarArg.Name);
                }
            }
        }

        Dictionary<string, VarId> mapping = [];
        foreach (var defRef in defRefs)
        {
            Definition? def = definitions.FirstOrDefault(def => def.Name == defRef);
            Logger.Assert(def != null, $"Reference to undefined variable '{defRef}'");

            VarId id = ExtractDefinition(def, varDefs);
            mapping[defRef] = id;
        }

        for (int i = 0; i < varDefs.Count; i++)
        {
            if (varDefs[i] is FuncVarDef funcDef)
            {
                varDefs[i] = Rewrite(funcDef, mapping);
            }
        }
    }


    private static VarId ExtractDefinition(Definition def, List<VarDef?> varDefs)
    {
        Context context = new(varDefs);

        for (uint i = 0; i < def.Defs.Count; i++)
        {
            if (def.Defs[(int)i] is FuncVarDef funcVarDef)
            {
                funcVarDef = Rewrite(funcVarDef, context);
                VarId id = context.AddVarDef(funcVarDef);
                context.mapping[new VarId(i)] = id;
            }
            else if (def.Defs[(int)i] == null) { }
            else throw new NotImplementedException();
        }

        return context.mapping[def.Def];
    }

    private static FuncVarDef Rewrite(FuncVarDef funcVarDef, Context context)
    {
        Var function = Rewrite(funcVarDef.Function, context);

        List<VarRef> args = [];
        foreach (var arg in funcVarDef.Args)
        {
            args.Add(new VarRef(Rewrite(arg.Var, context), arg.Args));
        }

        return new FuncVarDef(function, args, funcVarDef.ArgsCount);
    }
    private static Var Rewrite(Var var, Context context)
    {
        return var switch
        {
            VarId varId => context.mapping[varId],
            GenericVar genericVar => genericVar,
            StandardVar stdVar => stdVar,
            _ => throw new()
        };
    }

    private static FuncVarDef Rewrite(FuncVarDef funcVarDef, Dictionary<string, VarId> mapping)
    {
        Var function = Rewrite(funcVarDef.Function, mapping);

        List<VarRef> args = [];
        foreach (var arg in funcVarDef.Args)
        {
            args.Add(new VarRef(Rewrite(arg.Var, mapping), arg.Args));
        }

        return new FuncVarDef(function, args, funcVarDef.ArgsCount);
    }
    private static Var Rewrite(Var var, Dictionary<string, VarId> mapping)
    {
        return var switch
        {
            VarId varId => varId,
            GenericVar genericVar => genericVar,
            StandardVar stdVar => stdVar,
            UserVar userVar => mapping[userVar.Name],
            _ => throw new()
        };
    }
}