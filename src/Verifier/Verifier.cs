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
            e.Switch(VerifyTheorem, VerifyDefinition);
    }
    private void VerifyTheorem(Theorem theorem)
    {
        statements.EnterScope("Theorem");

        foreach (var stmt in theorem.requirements)
            statements.Add(stmt.expr);

        VerifyScope(theorem.proof, out bool sorryStatement);

        // Verify hypothesis
        if (!sorryStatement)
        {
            StmtVal val = AnalyseStatement(theorem.hypothesis.expr, theorem.hypothesis.line);
            Logger.Assert(val != StmtVal.FALSE, $"Hypothesis in line {theorem.hypothesis.line} is false.");
            Logger.Assert(val != StmtVal.UNKNOWN, $"Failed to verify hypothesis in line {theorem.hypothesis.line}");
        }

        statements.ExitScope("Theorem");
        theorems.Add(theorem.name, theorem);
    }
    private void VerifyDefinition(Definition definition)
    {
        statements.EnterScope("Definition");

        VerifyScope(definition.proof, out bool sorryStatement);

        if (!sorryStatement && definition.rules.Count > 0)
        {
            QuantifiedStatement existsStmt = new()
            {
                op = TokenType.EXISTS,
                obj = "_obj_",
                stmt = null!,
            };

            foreach (var stmt in definition.rules)
            {
                Expression stmtRewritten = RewriteExpression(stmt.expr, new() { { definition.name, new Term("_obj_") } });
                if (existsStmt.stmt == null)
                    existsStmt.stmt = stmtRewritten;
                else
                    existsStmt.stmt = new BinExpr() { lhs = existsStmt.stmt, op = new(TokenType.AND), rhs = stmtRewritten };
            }

            StmtVal val = AnalyseStatement(new Term(existsStmt), definition.line);
            Logger.Assert(val == StmtVal.TRUE, $"Failed to verify existence of object \"{definition.name}\" defined in line {definition.line}."
                + $"\n{Utility.Expr2Str(new Term(existsStmt))}");
        }

        statements.ExitScope("Definition");

        definitions.Add(definition.name, definition);
    }

    private void VerifyScope(Scope scope, out bool sorryStatement)
    {
        sorryStatement = false;

        foreach (var stmt in scope.statements)
        {
            if (stmt.stmt.TryAs<Command>(out var cmd))
            {
                if (cmd == Command.SORRY)
                {
                    sorryStatement = true;
                    return;
                }
                else if (cmd == Command.CHECK)
                {
                    statements.EnterScope("Check command");
                    AddProofToStatements(stmt.proof, stmt.line, out var _);

                    // Print statements
                    Console.WriteLine("\nCurrent statements:");
                    foreach (var s in statements.GetAll())
                        Console.WriteLine(Utility.Expr2Str(s));
                    Console.WriteLine("-------------------\n");

                    statements.ExitScope("Check command");
                }
            }
            else if (stmt.stmt.TryAs<DefinitionStatement>(out var defStmt))
            {
                statements.EnterScope("Definition statement");
                AddProofToStatements(stmt.proof, stmt.line, out var sorry);
                if (!sorry)
                {
                    // Verify that an object with the specified rules exists
                    Expression existsStmt = new Term(new QuantifiedStatement()
                    {
                        op = TokenType.EXISTS,
                        obj = "_obj_",
                        stmt = RewriteExpression(defStmt.stmt, new() { { defStmt.obj, new Term("_obj_") } })
                    });
                    VerifyStatementLine(new StatementLine() { line = stmt.line, stmt = existsStmt });
                }
                statements.ExitScope("Definition statement");
                statements.Add(defStmt.stmt);
            }
            else if (stmt.stmt.TryAs<ConditionalStatement>(out var condStmt))
            {
                statements.EnterScope("If");
                statements.Add(condStmt.condition.expr);
                VerifyScope(condStmt.ifScope, out bool _);
                VerifyScope(condStmt.bothScope, out bool _);
                statements.ExitScope("If");

                statements.EnterScope("Else");
                statements.Add(new Term(new UnaryExpr() { op = new(TokenType.NOT), expr = condStmt.condition.expr }));
                VerifyScope(condStmt.elseScope, out bool _);
                VerifyScope(condStmt.bothScope, out bool _);
                statements.ExitScope("Else");

                AddScopeStatements(condStmt.bothScope);
            }
            else
            {
                VerifyStatementLine(stmt);
                statements.Add(stmt.stmt.As<Expression>());
            }
        }
    }

    private void AddScopeStatements(Scope scope)
    {
        foreach (var stmtLine in scope.statements)
        {
            stmtLine.stmt.Switch(
                statements.Add,
                cmd => { },
                defStmt => statements.Add(defStmt.stmt),
                condStmt => AddScopeStatements(condStmt.bothScope)
                );
        }
    }

    private void VerifyStatementLine(StatementLine stmt)
    {
        Console.WriteLine($"Verifying statement in line {stmt.line}.");

        statements.EnterScope("Statement"); // Proof statements should get deleted after verifying the statement

        AddProofToStatements(stmt.proof, stmt.line, out bool sorry);

        StmtVal stmtVal = sorry ? StmtVal.TRUE : AnalyseStatement(stmt.stmt.As<Expression>(), stmt.line);

        statements.ExitScope("Statement");

        if (stmtVal == StmtVal.TRUE)
            return;
        else if (stmtVal == StmtVal.FALSE)
            Logger.Error($"Statement in line {stmt.line} is false.\n{Utility.Expr2Str(stmt.stmt.As<Expression>())}");
        else
            Logger.Error($"Failed to verify statement in line {stmt.line}.\n{Utility.Expr2Str(stmt.stmt.As<Expression>())}");
    }

    private void AddProofToStatements(Variant<FuncCall, string, Command>? proof, int line, out bool sorry)
    {
        sorry = false;

        if (proof == null) return;

        if (proof.TryAs<FuncCall>(out var funcCall))
        {
            HandleFuncCallProof(funcCall, line);
        }
        else if (proof.TryAs<string>(out var str))
        {
            foreach (var rule in definitions[str].rules)
                statements.Add(RewriteExpression(rule.expr, new()));
        }
        else if (proof.TryAs<Command>(out var command))
        {
            if (command == Command.SORRY)
            {
                sorry = true;
            }
        }
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
            for (int i = 0; i < call.args.Count; i++) conversionDict.Remove($"_{i}");

            Logger.Assert(!ContainsReplaceArgs(newExpr), $"Too many replacement arguments used in call to theorem {theorem.name} in line {line} (Expected {call.args.Count}).");

            // A second rewrite is required because the first only converts to the objects used in the theorem
            newExpr = RewriteExpression(newExpr, conversionDict);

            return newExpr;
        }

        // Rewrite and verify requirements
        foreach (var requirement in theorem.requirements)
        {
            Expression req = RewriteExpression(requirement.expr, conversionDict, RewriteCallback);

            Logger.Assert(AnalyseStatement(req, requirement.line) == StmtVal.TRUE,
                $"Failed to verify theorem requirement in line {requirement.line}. Theorem is referenced in line {line}." +
                $"\n{Utility.Expr2Str(req)}");
        }

        // Rewrite hypothesis and add it to the verified statements
        statements.Add(RewriteExpression(theorem.hypothesis.expr, conversionDict, RewriteCallback));
    }

    private bool ContainsReplaceArgs(Expression expr)
    {
        return Find(expr,
                expr => expr.TryAs<Term>(out var term)
                    && term.term.TryAs<string>(out var str)
                    && str[0] == '_');
    }

}
