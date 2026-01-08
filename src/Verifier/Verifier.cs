public partial class Verifier
{
    private enum StmtVal
    {
        UNKNOWN,
        TRUE,
        FALSE
    }

    private readonly Data ast;
    private ScopeStack<Expression> statements;
    private Dictionary<string, Theorem> theorems;
    private Dictionary<string, Definition> definitions;
    private int num; // Used so that expressions copied from a definition use different variable names 
    public Verifier(Data ast)
    {
        this.ast = ast;
        statements = new();
        theorems = new();
        definitions = new();
        num = 0;
    }
    public void Verify()
    {
        foreach (var e in ast.data)
            e.Switch(
                VerifyTheorem,
                definition => definitions.Add(definition.name, definition)
                );
    }
    private void VerifyTheorem(Theorem theorem)
    {
        statements.EnterScope("Theorem");

        foreach (var stmt in theorem.requirements)
            AddStatement(stmt.stmt.As<Expression>());

        foreach (var stmt in theorem.proof)
        {
            if (stmt.stmt.TryAs<Command>(out var cmd))
            {
                if (cmd == Command.SORRY)
                {
                    goto done;
                }
                else if (cmd == Command.CHECK)
                {
                    Console.WriteLine("\nCurrent statements:");
                    foreach (var s in statements.GetAll())
                        Console.WriteLine(ExpressionBuilder.ExpressionToString(s));
                    Console.WriteLine("-------------------\n");
                }
                continue;
            }
            VerifyStatementLine(stmt);
            AddStatement(stmt.stmt.As<Expression>());
        }
        VerifyStatementLine(theorem.hypothesis);
    done:
        statements.ExitScope("Theorem");
        theorems.Add(theorem.name, theorem);
    }
    private void VerifyStatementLine(StatementLine stmt)
    {
        Console.WriteLine($"Verifying statement in line {stmt.line}.");

        statements.EnterScope("Statement"); // Proof statements should get deleted after verifying the statement

        // Handle proof
        if (stmt.proof != null)
        {
            if (stmt.proof.TryAs<FuncCall>(out var funcCall))
            {
                Theorem theorem = theorems[funcCall.name];

                Dictionary<string, Expression> conversionDict = new();
                for (int i = 0; i < theorem.parameters.Count; i++)
                    conversionDict.Add(theorem.parameters[i], funcCall.args[i]);

                foreach (var requirement in theorem.requirements)
                    Logger.Assert(AnalyseStatement(RewriteExpression(requirement.stmt.As<Expression>(), conversionDict, num++), requirement.line) == StmtVal.TRUE,
                        $"Failed to verify theorem requirement in line {requirement.line}. Theorem is referenced in line {stmt.line}." +
                        $"\n{ExpressionBuilder.ExpressionToString(RewriteExpression(requirement.stmt.As<Expression>(), conversionDict, num))}");

                AddStatement(RewriteExpression(theorem.hypothesis.stmt.As<Expression>(), conversionDict, num++));
            }
            else if (stmt.proof.TryAs<string>(out var str))
            {
                foreach (var rule in definitions[str].rules)
                    AddStatement(RewriteExpression(rule.stmt.As<Expression>(), new(), num++));
            }
            else if (stmt.proof.TryAs<Command>(out var command))
            {
                if (command == Command.SORRY)
                {
                    statements.ExitScope("Statement");
                    return;
                }
            }
        }

        StmtVal stmtVal = AnalyseStatement(stmt.stmt.As<Expression>(), stmt.line);

        statements.ExitScope("Statement");

        if (stmtVal == StmtVal.TRUE)
            return;
        else if (stmtVal == StmtVal.FALSE)
            Logger.Error($"Statement in line {stmt.line} is false.\n{ExpressionBuilder.ExpressionToString(stmt.stmt.As<Expression>())}");
        else
            Logger.Error($"Failed to verify statement in line {stmt.line}.\n{ExpressionBuilder.ExpressionToString(stmt.stmt.As<Expression>())}");
    }

    private void AddStatement(Expression stmt)
    {
        statements.Add(stmt);
    }
}
