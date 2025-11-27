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
    private List<string> objects;
    private List<Expression> statements;
    private List<Expression> statementStack; // Statements only valid in the given context (in quantified statement, in imply statement, ...)
    private Dictionary<string, Theorem> theorems;
    private Dictionary<string, Definition> definitions;
    private int num; // Used so that expressions copied from a definition use different variable names 
    public Verifier(Data ast)
    {
        this.ast = ast;
        objects = new();
        statements = new();
        statementStack = new();
        theorems = new();
        definitions = new();
        num = 0;
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
        objects.Add(definition.obj);
        if (definition.parameters.Count != 0) throw new NotImplementedException();

        // Verify syntax / grammar of rules
        foreach (var rule in definition.rules)
        {
            Logger.Assert(rule.stmt.Is<Expression>(), $"Expected expression in definiton rules list (line {rule.line})");

            AnalyseStatement(rule.stmt.As<Expression>(), rule.line); // Result is not important, but syntax / grammar must be checked
        }

        objects.Clear();

        definitions.Add(definition.name, definition);
    }
    private void VerifyTheorem(Theorem theorem)
    {
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
                AddStatement(stmt.stmt.As<Expression>(), statements);
            }
        }

        foreach (var stmt in theorem.proof)
        {
            if (stmt.stmt.TryAs<Command>(out var cmd))
            {
                if (cmd == Command.SORRY)
                {
                    statements.Clear();
                    theorems.Add(theorem.name, theorem);
                    return;
                }
                else if (cmd == Command.CHECK)
                {
                    Console.WriteLine("\nCurrent statements:");
                    Console.Write(Formatter.Format(statements));
                    Console.WriteLine("-------------------\n");
                }
                continue;
            }
            VerifyStatementLine(stmt);
            AddStatement(stmt.stmt.As<Expression>(), statements);
        }
        VerifyStatementLine(theorem.hypothesis);
        statements.Clear();
        objects.Clear();
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

                AddStatement(RewriteExpression(theorem.hypothesis.stmt.As<Expression>(), conversionDict, num++), statements);
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
            Logger.Error($"Statement in line {stmt.line} is false.");
        else
            Logger.Error($"Failed to verify statement in line {stmt.line}");
    }
    private StmtVal AnalyseStatement(Expression expr, int line)
    {
        // Check if the statement has already been proven
        foreach (var stmt in statements)
            if (CompareExpressions(stmt, expr))
                return StmtVal.TRUE;
        foreach (var stmt in statementStack)
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
                        int stmtStackSize = statementStack.Count;
                        AddStatement(binExpr.lhs, statementStack);

                        StmtVal rhs = AnalyseStatement(binExpr.rhs, line);

                        statementStack.SetCount(stmtStackSize); // Remove statements only valid in this context

                        if (lhs == StmtVal.FALSE)
                            return StmtVal.TRUE;
                        else
                            return rhs;

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
                case TokenType.ELEMENT_OF:
                    {
                        VerifyExpression(binExpr.lhs, line);

                        if (!binExpr.rhs.TryAs<Term>(out var term)) throw new NotImplementedException();
                        if (!term.term.TryAs<string>(out var str)) throw new NotImplementedException();
                        Logger.Assert(definitions.ContainsKey(str), $"Undefined set \"{str}\" in line {line}");
                        Definition definition = definitions[str];

                        // Check if all rules are fullfilled
                        Dictionary<string, Expression> conversionDict = new() { { definition.obj, binExpr.lhs } };
                        foreach (var rule in definition.rules)
                        {
                            StmtVal val = AnalyseStatement(RewriteExpression(rule.stmt.As<Expression>(), conversionDict, num++), line);
                            if (val != StmtVal.TRUE)
                                return val;
                        }

                        return StmtVal.TRUE;
                    }
                case TokenType.SUBSET:
                    {
                        throw new NotImplementedException();
                    }
                case TokenType.EQUALS:
                    {
                        VerifyExpression(binExpr.lhs, line);
                        VerifyExpression(binExpr.rhs, line);

                        if (CompareExpressions(binExpr.lhs, binExpr.rhs))
                            return StmtVal.TRUE;
                        return StmtVal.UNKNOWN;
                    }
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
                        objects.Add(qStmt.obj);
                        StmtVal val = AnalyseStatement(qStmt.stmt, line);
                        objects.Remove(qStmt.obj);
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
    private void AddStatement(Expression stmt, List<Expression> statements)
    {
        if (stmt.Is<Term>())
        {
            var term = stmt.As<Term>().term;

            if (term.TryAs<Expression>(out var expr))
            {
                AddStatement(expr, statements);
                return;
            }
        }
        else
        {
            var binExpr = stmt.As<BinExpr>();

            if (binExpr.op.type == TokenType.ELEMENT_OF)
            {
                if (binExpr.rhs.TryAs<Term>(out var term) && term.term.TryAs<string>(out var str))
                {
                    if (!definitions.ContainsKey(str))
                        Logger.Error($"Use of undefined set \"{str}\" in ElementOf statement");
                    Definition set = definitions[str];
                    foreach (var rule in set.rules)
                    {
                        Dictionary<string, Expression> conversionDict = new() { { set.obj, binExpr.lhs } };
                        AddStatement(RewriteExpression(rule.stmt.As<Expression>(), conversionDict, num), statements);
                    }
                    num++;
                }
                else
                    throw new NotImplementedException();
            }
            else if (binExpr.op.type == TokenType.AND)
            {
                AddStatement(binExpr.lhs, statements);
                AddStatement(binExpr.rhs, statements);
            }
            else if (binExpr.op.type == TokenType.IMPLIES)
            {
                if (AnalyseStatement(binExpr.lhs, -1) == StmtVal.TRUE)
                    AddStatement(binExpr.rhs, statements);
            }
        }
        statements.Add(stmt);
    }
}
