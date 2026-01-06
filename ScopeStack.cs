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
        foreach (Scope scope in scopes)
            foreach (T value in scope.statements)
                yield return value;
    }
    public bool Contains(T value)
    {
        foreach (T e in GetAll())
            if (e!.Equals(value)) return true;
        return false;
    }
}