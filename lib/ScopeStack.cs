public class ScopeStack<T>
{
    private struct Scope
    {
        public string debugName;
        public List<T> statements;
        public Scope(string debugName)
        {
            this.debugName = debugName;
            statements = new();
        }
    }
    private readonly List<Scope> scopes;

    public ScopeStack()
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
    public void Add(T value)
    {
        scopes.Last().statements.Add(value);
    }
    public IEnumerable<T> GetAll()
    {
        // Use for loops instead of foreach iteration because the collection might be modified 
        // while iterating (for example analysing a P => Q statement while trying to proof with another statement)
        // These modifications will be reverted before the next element is called but C# doesn't know that
        for (int i = 0; i < scopes.Count; i++)
        {
            Scope scope = scopes[i];
            for (int j = 0; j < scope.statements.Count; j++)
                yield return scope.statements[j];
        }
    }
    public bool Contains(T value)
    {
        foreach (T e in GetAll())
            if (e!.Equals(value)) return true;
        return false;
    }
}