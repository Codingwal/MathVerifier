using MathVerifier.IR;

namespace MathVerifier.Services.Verifier;

public class Verifier
{
    private readonly List<Theorem> ProvenTheorems = [];
    private readonly VerificationContext Context = new();
    private List<VarDef> varDefs = [];

    private VarDef GetVarDef(VarId varId) => varDefs[(int)varId.Value];

    private void Verify(Data data)
    {
        foreach (var theorem in data.Theorems)
        {
            varDefs = theorem.Defs;
            Verify(theorem);
            ProvenTheorems.Add(theorem);
        }
    }
    private void Verify(Theorem theorem)
    {
        foreach (var stmt in theorem.ProofStmts)
        {
            if (stmt is VarId stmtId)
            {
                Logger.Assert(Verify(stmtId), $"Failed to verify statement {stmtId}");
            }
            else if (stmt is TheoremRef theoremRef)
            {
                Theorem? t = ProvenTheorems.FirstOrDefault(t => t.Name == theoremRef.Name);
                Logger.Assert(t != null, $"Reference to undefined theorem \"{theoremRef.Name}\"");

                throw new NotImplementedException();
                // TODO: The terms must already be present in this VarDef list
                // Context.AddGlobalStatement(t.Hypothesis);
            }
            else throw new NotImplementedException();
        }
    }
    private bool Verify(Var stmt)
    {
        if (stmt is StandardVar standardVar)
        {
            if (standardVar.Value == StandardVar.Values.TRUE) return true;
            if (standardVar.Value == StandardVar.Values.FALSE) return false;
            throw new NotImplementedException();
        }
        else if (stmt is VarId varId)
            return Verify(varId);

        throw new NotImplementedException();
    }
    private bool Verify(VarId stmtId)
    {
        if (Context.IsProven(stmtId)) return true;

        VarDef stmt = GetVarDef(stmtId);

        if (stmt is FuncVarDef funcVarDef)
            return Verify(funcVarDef);
        else throw new NotImplementedException();
    }
    private bool Verify(FuncVarDef funcVarDef)
    {
        if (funcVarDef.Function is not StandardVar standardVar)
            return false;

        // TODO: Handle generic args

        if (standardVar.Value == StandardVar.Values.AND)
        {
            bool lhs = Verify(funcVarDef.Args[0].Var);
            bool rhs = Verify(funcVarDef.Args[1].Var);
            return lhs && rhs;
        }
        else if (standardVar.Value == StandardVar.Values.OR)
        {
            bool lhs = Verify(funcVarDef.Args[0].Var);
            bool rhs = Verify(funcVarDef.Args[1].Var);
            return lhs || rhs;
        }
        else if (standardVar.Value == StandardVar.Values.IMPLIES)
        {
            bool lhs = Verify(funcVarDef.Args[0].Var);

            Context.EnterScope(Expand(funcVarDef.Args[0].Var));
            bool rhs = Verify(funcVarDef.Args[1].Var);
            Context.ExitScope();

            return !lhs || rhs;
        }
        else if (standardVar.Value == StandardVar.Values.EQUALS)
        {
            return Comparer.IsEqual(funcVarDef.Args[0].Var, funcVarDef.Args[1].Var);
        }
        else
            throw new NotImplementedException();
    }

    private HashSet<VarId> Expand(Var var)
    {
        if (var is not VarId varId) return []; // Ignore true/false/...

        VarDef varDef = GetVarDef(varId);
        if (varDef is not FuncVarDef funcVarDef) throw new NotImplementedException();

        if (funcVarDef.Function is not StandardVar standardVar) return [];

        if (standardVar.Value == StandardVar.Values.AND)
        {
            // TODO: Handle generics
            HashSet<VarId> setLhs = Expand(funcVarDef.Args[0].Var);
            HashSet<VarId> setRhs = Expand(funcVarDef.Args[1].Var);

            foreach (var id in setRhs) setLhs.Add(id);
            return setLhs;
        }
        return [];
    }
}