using MathVerifier.IR;

namespace MathVerifier;

public static class ReferenceResolver
{
    private class Context(List<VarDef> _varDefs)
    {
        public List<VarDef> VarDefs = _varDefs;
        public Dictionary<Var, VarId> mapping = [];

        public VarId AddVarDef(VarDef def)
        {
            int index = VarDefs.FindIndex(d => d != null && Comparer.IsEqual(d, def));
            if (index != -1) return new VarId((uint)index);

            VarDefs.Add(def);
            return new VarId((uint)VarDefs.Count - 1);
        }
    }

    public static Data Resolve(Data data)
    {
        // Rewrite all definitions
        List<Definition> definitions = []; // Create a new list of definitions to prevent circular references
        foreach (var def in data.Defs)
        {
            var context = Resolve(def.Defs, definitions);
            VarId defId = context.mapping[def.Def]; // Use the mapping to find the new id of the "var to return"
            definitions.Add(new Definition(def.Name, def.ParamCount, context.VarDefs, defId));
        }

        // Rewrite all theorems
        List<Theorem> theorems = [];
        foreach (var theorem in data.Theorems)
        {
            var context = Resolve(theorem.Defs, definitions);

            // Use the mapping to find the new ids of the proof statements
            List<IStatement> proofStmts = theorem.ProofStmts.Select(stmt =>
            {
                if (stmt is VarId varId)
                    return context.mapping[varId];
                else
                    return stmt;
            }).ToList();

            VarId hyp = context.mapping[theorem.Hypothesis]; // Also remap the hypothesis

            theorems.Add(new Theorem(theorem.Name, context.VarDefs, proofStmts, hyp));
        }

        return new Data(definitions, theorems);
    }

    private static Context Resolve(List<VarDef> varDefsOld, List<Definition> definitions)
    {
        // Create a set containing all referenced user variables
        HashSet<string> defRefs = [];
        foreach (var varDef in varDefsOld)
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

        // Extract all referenced definitions into this VarDef list
        Context context = new([]); // Start with an empty list of VarDefs, so that the extracted definitions come first
        foreach (var defRef in defRefs)
        {
            // Find the definition
            Definition? def = definitions.FirstOrDefault(def => def.Name == defRef);
            Logger.Assert(def != null, $"Reference to undefined variable '{defRef}'");

            // Extract all VarDefs of the definition into this VarDef list
            // Store the VarId that should be used instead of the UserVar in the mapping
            VarId id = ExtractDefinition(def, context.VarDefs);
            context.mapping[new UserVar(defRef)] = id;
        }

        // (Re-)add the VarDefs and replace the UserVars with the new VarIds
        for (int i = 0; i < varDefsOld.Count; i++)
        {
            if (varDefsOld[i] is FuncVarDef funcDef)
            {
                funcDef = Rewrite(funcDef, context); // Replace all UserVars with the new VarIds
                VarId id = context.AddVarDef(funcDef); // Add the rewritten VarDef to the context
                context.mapping[new VarId((uint)i)] = id; // Store the new id in the mapping
            }
        }

        return context;
    }

    private static VarId ExtractDefinition(Definition def, List<VarDef> varDefs)
    {
        Context context = new(varDefs);

        // For each VarDef in the definition...
        for (uint i = 0; i < def.Defs.Count; i++)
        {
            VarDef varDef;
            if (def.Defs[(int)i] is FuncVarDef funcVarDef)
            {
                varDef = Rewrite(funcVarDef, context); // Rewrite the function using the mapping
            }
            else throw new NotImplementedException();

            VarId id = context.AddVarDef(varDef); // Add the VarDef to the context
            context.mapping[new VarId(i)] = id; // Add the new VarId to the mapping
        }

        VarId returnId = context.mapping[def.Def]; // The varId of the whole expression
        return returnId;
    }

    // Rewrite a Function-Variable-Definition using the provided mapping
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

    // Rewrite a variable using the provided mapping
    private static Var Rewrite(Var var, Context context)
    {
        if (context.mapping.TryGetValue(var, out VarId? mapped))
            return mapped;

        return var;
    }
}