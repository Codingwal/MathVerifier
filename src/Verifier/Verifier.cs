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
    public Verifier(Data ast)
    {
        this.ast = ast;
        statements = new();
        theorems = new();
        definitions = new();
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
            }
            else if (stmt.stmt.TryAs<DefinitionStatement>(out var defStmt))
            {
                AddStatement(defStmt.stmt);
            }
            else
            {
                VerifyStatementLine(stmt);
                AddStatement(stmt.stmt.As<Expression>());
            }
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
                HandleFuncCallProof(funcCall, stmt.line);
            }
            else if (stmt.proof.TryAs<string>(out var str))
            {
                foreach (var rule in definitions[str].rules)
                    AddStatement(RewriteExpression(rule.stmt.As<Expression>(), new()));
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

    private void HandleFuncCallProof(FuncCall funcCall, int line)
    {
        Theorem theorem = theorems[funcCall.name];

        // Setup conversion lists (inspect arguments)
        Dictionary<string, Expression> conversionDict = new();
        Dictionary<string, Expression> replaceArgs = new();
        for (int i = 0; i < theorem.parameters.Count; i++)
        {
            if (ContainsReplaceArgs(funcCall.args[i]))
                replaceArgs.Add(theorem.parameters[i], funcCall.args[i]);
            else
                conversionDict.Add(theorem.parameters[i], funcCall.args[i]);
        }

        Expression? RewriteCallback(Expression old)
        {
            if (!old.TryAs<Term>(out var term)) return null; // Ignore BinExpr

            // Arguments with replace args can't be used as objects
            if (term.term.TryAs<string>(out var str))
            {
                Logger.Assert(!replaceArgs.ContainsKey(str), $"Can't use replace arguments if the argument is used as an object!");
                return null;
            }

            if (!term.term.TryAs<FuncCall>(out var call)) return null; // Ignore everything except FuncCall
            if (!replaceArgs.TryGetValue(call.name, out var arg)) return null; // Ignore if not in replaceArgs

            // Rewrite functions passed as replace args
            for (int i = 0; i < call.args.Count; i++) conversionDict.Add($"_{i}", call.args[i]);
            Expression newExpr = RewriteExpression(arg, conversionDict);
            Logger.Assert(!ContainsReplaceArgs(newExpr), $"Too many replacement arguments used in call to theorem {theorem.name} in line {line}.");
            for (int i = 0; i < call.args.Count; i++) conversionDict.Remove($"_{i}");

            // A second rewrite is required because the first only converts to the objects used in the theorem
            newExpr = RewriteExpression(newExpr, conversionDict);

            return newExpr;
        }

        // Rewrite and verify requirements
        foreach (var requirement in theorem.requirements)
        {
            Expression req = RewriteExpression(requirement.stmt.As<Expression>(), conversionDict, RewriteCallback);

            Logger.Assert(AnalyseStatement(req, requirement.line) == StmtVal.TRUE,
                $"Failed to verify theorem requirement in line {requirement.line}. Theorem is referenced in line {line}." +
                $"\n{ExpressionBuilder.ExpressionToString(req)}");
        }

        // Rewrite hypothesis and add it to the verified statements
        AddStatement(RewriteExpression(theorem.hypothesis.stmt.As<Expression>(), conversionDict, RewriteCallback));
    }

    private bool ContainsReplaceArgs(Expression expr)
    {
        return Find(expr,
                expr => expr.TryAs<Term>(out var term)
                    && term.term.TryAs<string>(out var str)
                    && str[0] == '_');
    }

    private void AddStatement(Expression stmt)
    {
        statements.Add(stmt);
    }
}
