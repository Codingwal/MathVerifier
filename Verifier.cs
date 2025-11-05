public class Verifier
{
    private readonly Data ast;
    private List<Statement> statements;
    private Dictionary<string, Definition> definitions;
    public Verifier(Data ast)
    {
        this.ast = ast;
        statements = new();
        definitions = new();
    }
    public void Verify()
    {
        foreach (var e in ast.data)
        {
            e.Switch(
                VerifyTheorem,
                VerifyDefinition
            );
        }
    }
    private void VerifyDefinition(Definition definition)
    {
        definitions.Add(definition.name, definition);
    }
    private void VerifyTheorem(Theorem theorem)
    {
        foreach (var stmt in theorem.requirements)
        {
            statements.Add(stmt);
        }
    }
    private void AddStatement(Statement stmt)
    {
        // if (stmt.expr.expr.)
    }
}