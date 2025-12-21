public class Statements
{
    private struct Scope
    {
        public string debugName;
        public List<Expression> statements;
        public Scope(string debugName)
        {
            this.debugName = debugName;
            statements = new();
        }
    }
    private readonly List<Scope> scopes;

    public Statements()
    {
        scopes = new();
        EnterScope("Global");
    }

    public void EnterScope(string debugName)
    {
        scopes.Add(new Scope(debugName));
    }
    public void ExitScope(string expectedName)
    {
        Logger.Assert(scopes[^1].debugName == expectedName, $"Popped scope with unexpected name ({scopes[^1].debugName} instead of {expectedName})");
        scopes.RemoveAt(scopes.Count - 1);
    }
    public void AddStatement(Expression stmt)
    {
        scopes.Last().statements.Add(stmt);
    }
    public IEnumerable<Expression> GetStatements()
    {
        foreach (var scope in scopes)
            foreach (var stmt in scope.statements)
                yield return stmt;
    }
}