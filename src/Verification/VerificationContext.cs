using MathVerifier.IR;

namespace MathVerifier.Services.Verifier;

public class VerificationContext
{
    private List<ScopeContext> Contexts = [];
    private ScopeStack<VarId> StmtStack = new();

    public HashSet<VarId> GetAllStatements()
    {
        HashSet<VarId> stmts = [];

        foreach (var stmt in StmtStack.GetAll())
            stmts.Add(stmt);

        foreach (var context in Contexts)
        {
            // Verify that all requirements are met
            if (context.Requirements.Any(req => !stmts.Contains(req)))
                continue;

            // Add all context statements
            foreach (var stmt in context.ProvenStmts)
                stmts.Add(stmt);
        }

        return stmts;
    }
    public bool IsProven(VarId stmt)
    {
        return GetAllStatements().Contains(stmt);
    }

    public void EnterScope(HashSet<VarId> scopedStmts)
    {
        StmtStack.EnterScope("");
        foreach (var stmt in scopedStmts)
            StmtStack.Add(stmt);
    }
    public void ExitScope()
    {
        StmtStack.ExitScope("");
    }
    public void AddGlobalStatement(VarId stmt)
    {
        if (StmtStack.CurrentScopeName != "Global") throw new();

        if (!StmtStack.Contains(stmt)) StmtStack.Add(stmt);
    }
}

public class ScopeContext(HashSet<VarId> _requirements, int _genericsCount)
{
    public readonly int GenericsCount = _genericsCount;
    public readonly HashSet<VarId> Requirements = _requirements;
    public readonly HashSet<VarId> ProvenStmts = [];
}