using TokenType = Token.TokenType;

public partial class Verifier
{
    private enum StmtVal
    {
        UNKNOWN,
        TRUE,
        FALSE
    }

    private readonly Data ast;
    private ScopeStack<string> objects;
    private ScopeStack<Expression> statements;
    private Dictionary<string, Theorem> theorems;
    private Dictionary<string, Definition> definitions;
    private int num; // Used so that expressions copied from a definition use different variable names 
    public Verifier(Data ast)
    {
        this.ast = ast;
        objects = new();
        statements = new();
        theorems = new();
        definitions = new();
        num = 0;
    }
    private void EnterScope(string scopeName)
    {
        objects.EnterScope(scopeName);
        statements.EnterScope(scopeName);
    }
    private void ExitScope(string scopeName)
    {
        objects.ExitScope(scopeName);
        statements.ExitScope(scopeName);
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
        EnterScope("Definition");

        // Verify syntax / grammar of rules
        foreach (var rule in definition.rules)
        {
            Logger.Assert(rule.stmt.Is<Expression>(), $"Expected expression in definiton rules list (line {rule.line})");

            AnalyseStatement(rule.stmt.As<Expression>(), rule.line); // Result is not important, but syntax / grammar must be checked
        }

        ExitScope("Definition");

        definitions.Add(definition.name, definition);
    }
    private void VerifyTheorem(Theorem theorem)
    {
        EnterScope("Theorem");

        foreach (var param in theorem.parameters)
            objects.Add(param);

        foreach (var stmt in theorem.requirements)
        {
            if (stmt.stmt.TryAs<Command>(out var cmd))
            {
                if (cmd == Command.CHECK)
                {
                    Console.WriteLine("\nCurrent statements:");
                    Console.Write(Formatter.Format(statements));
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
                    theorems.Add(theorem.name, theorem);
                    return;
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
        ExitScope("Theorem");
        theorems.Add(theorem.name, theorem);
    }
    private void VerifyStatementLine(StatementLine stmt)
    {
        Console.WriteLine($"Verifying statement in line {stmt.line} -----------------------------------------------------------------");

        if (stmt.stmt.Is<Command>()) Logger.Error($"Unexpected command {stmt.stmt.As<Command>()} in line {stmt.line}! Expected statement.");

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

                AddStatement(RewriteExpression(theorem.hypothesis.stmt.As<Expression>(), conversionDict, num++));
            }
            else if (stmt.proof.TryAs<Command>(out var command))
            {
                if (command == Command.SORRY) return;
                else Logger.Error($"Unexpected command {command} as proof in line {stmt.line}");
            }
        }

        StmtVal stmtVal = AnalyseStatement(stmt.stmt.As<Expression>(), stmt.line);
        if (stmtVal == StmtVal.TRUE)
            return;
        else if (stmtVal == StmtVal.FALSE)
            Logger.Error($"Statement in line {stmt.line} is false.\n{ExpressionBuilder.ExpressionToString(stmt.stmt.As<Expression>())}");
        else
            Logger.Error($"Failed to verify statement in line {stmt.line}.\n{ExpressionBuilder.ExpressionToString(stmt.stmt.As<Expression>())}");
    }
    private StmtVal AnalyseStatement(Expression expr, int line)
    {
        // Check if the statement has already been proven
        foreach (var stmt in statements.GetAll())
            if (CompareExpressions(stmt, expr))
                return StmtVal.TRUE;

        if (expr.TryAs<BinExpr>(out var binExpr))
        {
            switch (binExpr.op.type)
            {
                case TokenType.IMPLIES:
                    {
                        StmtVal lhs = AnalyseStatement(binExpr.lhs, line);

                        // Add statements valid in this context
                        statements.EnterScope("Implies");
                        AddStatement(binExpr.lhs);

                        StmtVal rhs = AnalyseStatement(binExpr.rhs, line);

                        statements.ExitScope("Implies");

                        if (lhs == StmtVal.FALSE)
                            return StmtVal.TRUE;
                        if (lhs == StmtVal.UNKNOWN && rhs != StmtVal.TRUE)
                            return StmtVal.UNKNOWN;
                        else
                            return rhs;

                    }
                case TokenType.EQUIVALENT:
                    {
                        StmtVal lhs = AnalyseStatement(binExpr.lhs, line);
                        StmtVal rhs = AnalyseStatement(binExpr.rhs, line);

                        if (lhs == StmtVal.UNKNOWN || rhs == StmtVal.UNKNOWN)
                            return StmtVal.UNKNOWN;
                        else
                            return lhs == rhs ? StmtVal.TRUE : StmtVal.FALSE;
                    }
                case TokenType.OR:
                    {
                        StmtVal lhs = AnalyseStatement(binExpr.lhs, line);
                        StmtVal rhs = AnalyseStatement(binExpr.rhs, line);

                        if (lhs == StmtVal.TRUE || rhs == StmtVal.TRUE)
                            return StmtVal.TRUE;
                        else if (lhs == StmtVal.FALSE && rhs == StmtVal.FALSE)
                            return StmtVal.FALSE;
                        else
                            return StmtVal.UNKNOWN;
                    }
                case TokenType.AND:
                    {
                        StmtVal lhs = AnalyseStatement(binExpr.lhs, line);
                        StmtVal rhs = AnalyseStatement(binExpr.rhs, line);

                        if (lhs == StmtVal.TRUE && rhs == StmtVal.TRUE)
                            return StmtVal.TRUE;
                        else if (lhs == StmtVal.FALSE || rhs == StmtVal.FALSE)
                            return StmtVal.FALSE;
                        else
                            return StmtVal.UNKNOWN;
                    }
                case TokenType.EQUALS:
                    {
                        return AnalyseExpressionEquality(binExpr.lhs, binExpr.rhs, line);
                    }
                case TokenType.ELEMENT_OF:
                case TokenType.SUBSET:
                    return StmtVal.UNKNOWN;
                default:
                    Logger.Error($"Invalid statement operator {binExpr.op} in line {line}");
                    throw new();
            }
        }
        else if (expr.TryAs<Term>(out var term))
        {
            return term.term.Match(
                expr => { return AnalyseStatement(expr, line); },
                funcCall => { throw new NotImplementedException(); },
                qStmt =>
                {
                    if (qStmt.op == TokenType.FOR_ALL)
                    {
                        objects.EnterScope("Quantified statement");
                        objects.Add(qStmt.obj);
                        StmtVal val = AnalyseStatement(qStmt.stmt, line);
                        objects.ExitScope("Quantified statement");
                        return val;
                    }
                    else
                        throw new NotImplementedException();
                },
                str =>
                {
                    Logger.Assert(objects.Contains(str), $"Undefined identifier \"{str}\" in line {line}");
                    return StmtVal.UNKNOWN;
                },
                num => { Logger.Error($"Expected statement but found number ({num}) in line {line}"); throw new(); }
            );
        }
        else
            throw new();
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
    private StmtVal AnalyseExpressionEquality(Expression a, Expression b, int line, int recursiveDepth = 0)
    {
        if (a.Index == b.Index)
        {
            if (a.TryAs<BinExpr>(out var binA))
            {
                StmtVal val = AnalyseBinExprEquality(binA, b.As<BinExpr>(), line, recursiveDepth);
                if (val != StmtVal.UNKNOWN) return val;
            }
            else
            {
                StmtVal val = AnalyseTermEquality(a.As<Term>(), b.As<Term>(), line, recursiveDepth);
                if (val != StmtVal.UNKNOWN) return val;
            }
        }

        if (recursiveDepth > 2)
            return StmtVal.UNKNOWN;

        foreach (var expr in statements.GetAll())
        {
            StmtVal val = AnalyseExprEqualityWithStmt(a, b, expr, line, recursiveDepth);
            if (val != StmtVal.UNKNOWN) return val;
        }

        return StmtVal.UNKNOWN;
    }
    private StmtVal AnalyseExprEqualityWithStmt(Expression a, Expression b, Expression expr, int line, int recursiveDepth)
    {
        if (!expr.TryAs<BinExpr>(out var binExpr)) return StmtVal.UNKNOWN;
        if (binExpr.op.type != TokenType.EQUALS) return StmtVal.UNKNOWN;

        StmtVal lhs1 = AnalyseExpressionEquality(binExpr.lhs, a, line, recursiveDepth + 1);
        StmtVal rhs1 = AnalyseExpressionEquality(binExpr.rhs, b, line, recursiveDepth + 1);
        if (lhs1 == StmtVal.TRUE && rhs1 == StmtVal.TRUE) return StmtVal.TRUE;

        StmtVal lhs2 = AnalyseExpressionEquality(binExpr.lhs, b, line, recursiveDepth + 1);
        StmtVal rhs2 = AnalyseExpressionEquality(binExpr.rhs, a, line, recursiveDepth + 1);
        if (lhs2 == StmtVal.TRUE && rhs2 == StmtVal.TRUE) return StmtVal.TRUE;

        // If both pairings result in StmtVal.FALSE, the statement can't be true
        if ((lhs1 == StmtVal.FALSE || rhs1 == StmtVal.FALSE)
            && (lhs2 == StmtVal.FALSE || rhs2 == StmtVal.FALSE))
            return StmtVal.FALSE;

        return StmtVal.UNKNOWN;
    }
    private StmtVal AnalyseBinExprEquality(BinExpr a, BinExpr b, int line, int recursiveDepth)
    {
        Logger.Assert(Token.GetPrecedence(a.op.type) >= Token.ExpressionMinPrec, $"Invalid expression operator \"{a.op}\" in line {line}");
        Logger.Assert(Token.GetPrecedence(b.op.type) >= Token.ExpressionMinPrec, $"Invalid expression operator \"{b.op}\" in line {line}");

        if (a.op != b.op) return StmtVal.UNKNOWN;

        StmtVal lhs = AnalyseExpressionEquality(a.lhs, b.lhs, line, recursiveDepth);
        StmtVal rhs = AnalyseExpressionEquality(a.rhs, b.rhs, line, recursiveDepth);

        if (lhs == StmtVal.FALSE || rhs == StmtVal.FALSE) return StmtVal.FALSE;
        if (lhs == StmtVal.TRUE && rhs == StmtVal.TRUE) return StmtVal.TRUE;
        return StmtVal.UNKNOWN;
    }
    private StmtVal AnalyseTermEquality(Term a, Term b, int line, int recursiveDepth)
    {
        if (a.term.Index != b.term.Index) return StmtVal.UNKNOWN;

        return a.term.Match(
            expr => AnalyseExpressionEquality(expr, b.term.As<Expression>(), line, recursiveDepth),
            funcCall => { throw new NotImplementedException(); },
            qStmt => { Logger.Error($"Unexpected quantified statement in expression in line {line}"); throw new(); },
            str =>
            {
                Logger.Assert(objects.Contains(str), $"Undefined identifier \"{str}\" in line {line}");
                Logger.Assert(objects.Contains(b.term.As<string>()), $"Undefined identifier \"{b.term.As<string>()}\" in line {line}");
                return (str == b.term.As<string>()) ? StmtVal.TRUE : StmtVal.UNKNOWN;
            },
            num => (num == b.term.As<double>()) ? StmtVal.TRUE : StmtVal.FALSE
        );
    }
}
