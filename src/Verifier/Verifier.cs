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
            e.Switch(VerifyTheorem, VerifyDefinition);
    }
    private void VerifyDefinition(Definition definition)
    {
        // Verify syntax / grammar of rules
        foreach (var rule in definition.rules)
        {
            Logger.Assert(rule.stmt.Is<Expression>(), $"Expected expression in definiton rules list (line {rule.line})");

            AnalyseStatement(rule.stmt.As<Expression>(), rule.line); // Result is not important, but syntax / grammar must be checked
        }

        definitions.Add(definition.name, definition);
    }
    private void VerifyTheorem(Theorem theorem)
    {
        statements.EnterScope("Theorem");

        foreach (var stmt in theorem.requirements)
        {
            if (stmt.stmt.TryAs<Command>(out var cmd))
            {
                if (cmd == Command.CHECK)
                {
                    Console.WriteLine("\nCurrent statements:");
                    foreach (var s in statements.GetAll())
                        Console.WriteLine(ExpressionBuilder.ExpressionToString(s));
                    Console.WriteLine("-------------------\n");
                }
                else
                    Logger.Error($"Invalid command {cmd} in theorem requirements in line {stmt.line}");
            }
            else
            {
                AnalyseStatement(stmt.stmt.As<Expression>(), stmt.line); // Result is not important, but syntax / grammar must be checked
                AddStatement(stmt.stmt.As<Expression>());
            }
        }

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

        if (stmt.stmt.Is<Command>()) Logger.Error($"Unexpected command {stmt.stmt.As<Command>()} in line {stmt.line}! Expected statement.");

        statements.EnterScope("Statement"); // Proof statements should get deleted after verifying the statement

        // Handle proof
        if (stmt.proof != null)
        {
            if (stmt.proof.TryAs<FuncCall>(out var funcCall))
            {
                Logger.Assert(theorems.ContainsKey(funcCall.name), $"Reference to undefined theorem \"{funcCall.name}\" in line {stmt.line}");
                Theorem theorem = theorems[funcCall.name];
                Logger.Assert(funcCall.args.Count == theorem.parameters.Count,
                    $"Expected {theorem.parameters.Count} arguments but found {funcCall.args.Count} in theorem call in line {stmt.line}");

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
                Logger.Assert(definitions.ContainsKey(str), $"Reference to undefined definition \"{str}\" in line {stmt.line}");
                Definition definition = definitions[str];

                foreach (var rule in definition.rules)
                    AddStatement(RewriteExpression(rule.stmt.As<Expression>(), new(), num++));
            }
            else if (stmt.proof.TryAs<Command>(out var command))
            {
                if (command == Command.SORRY)
                {
                    statements.ExitScope("Statement");
                    return;
                }
                else Logger.Error($"Unexpected command {command} as proof in line {stmt.line}");
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
        // if (stmt.Is<Term>())
        // {
        //     var term = stmt.As<Term>().term;

        //     if (term.TryAs<Expression>(out var expr))
        //     {
        //         AddStatement(expr);
        //         return;
        //     }
        // }
        // else
        // {
        //     var binExpr = stmt.As<BinExpr>();

        //     if (binExpr.op.type == TokenType.ELEMENT_OF)
        //     {
        //         if (binExpr.rhs.TryAs<Term>(out var term) && term.term.TryAs<string>(out var str))
        //         {
        //             if (!definitions.ContainsKey(str))
        //                 Logger.Error($"Use of undefined set \"{str}\" in ElementOf statement");
        //             Definition set = definitions[str];
        //             foreach (var rule in set.rules)
        //             {
        //                 Dictionary<string, Expression> conversionDict = new() { { set.obj, binExpr.lhs } };
        //                 AddStatement(RewriteExpression(rule.stmt.As<Expression>(), conversionDict, num));
        //             }
        //             num++;
        //         }
        //         else
        //             throw new NotImplementedException();
        //     }
        //     else if (binExpr.op.type == TokenType.AND)
        //     {
        //         AddStatement(binExpr.lhs);
        //         AddStatement(binExpr.rhs);
        //     }
        //     else if (binExpr.op.type == TokenType.IMPLIES)
        //     {
        //         if (AnalyseStatement(binExpr.lhs, -1) == StmtVal.TRUE)
        //             AddStatement(binExpr.rhs);
        //     }
        // }
        statements.Add(stmt);
    }
}
