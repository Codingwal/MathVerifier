using TokenType = Token.TokenType;


public class Verifier
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
    private Stack<Expression> statementStack; // Statements only valid in the given context (in quantified statement, in imply statement, ...)
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
                    Logger.Error($"Invalid command {cmd} in theorem requirements in line {stmt.line + 1}");
            }
            else
                AddStatement(stmt.stmt.As<Expression>());
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
            AddStatement(stmt.stmt.As<Expression>());
        }
        VerifyStatementLine(theorem.hypothesis);
        statements.Clear();
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

        if (expr.TryAs<BinExpr>(out var binExpr))
        {
            switch (binExpr.op.type)
            {
                case TokenType.IMPLIES:
                    {
                        StmtVal lhs = AnalyseStatement(binExpr.lhs, line);
                        StmtVal rhs = AnalyseStatement(binExpr.rhs, line);
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
                // case TokenType.ELEMENT_OF:
                //     {

                //     }
                // case TokenType.SUBSET:
                //     {

                //     }
                // case TokenType.EQUALS:
                //     {

                //     }
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
                str => { return StmtVal.UNKNOWN; },
                num => { Logger.Error($"Expected statement but found number {num} in line {line}"); throw new(); }
            );
        }
        else
            throw new();
    }
    private void AddStatement(Expression stmt)
    {
        if (stmt.Is<Term>())
        {
            var term = stmt.As<Term>().term;

            if (term.TryAs<Expression>(out var expr))
            {
                AddStatement(expr);
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
                        AddStatement(RewriteExpression(rule.stmt.As<Expression>(), conversionDict, num));
                    }
                    num++;
                }
                else
                    throw new NotImplementedException();
            }
            else if (binExpr.op.type == TokenType.AND)
            {
                AddStatement(binExpr.lhs);
                AddStatement(binExpr.rhs);
            }
            else if (binExpr.op.type == TokenType.IMPLIES)
            {
                if (AnalyseStatement(binExpr.lhs, -1) == StmtVal.TRUE)
                    AddStatement(binExpr.rhs);
            }
        }
        statements.Add(stmt);
    }
    private Expression RewriteExpression(Expression old, Dictionary<string, Expression> conversionDict, int num)
    {
        return old.Match(
            binExpr =>
            {
                return new BinExpr()
                {
                    lhs = RewriteExpression(binExpr.lhs, conversionDict, num),
                    op = binExpr.op,
                    rhs = RewriteExpression(binExpr.rhs, conversionDict, num),
                };
            },
            term =>
            {
                return term.term.Match(
                    expr => RewriteExpression(expr, conversionDict, num),
                    funcCall =>
                    {
                        FuncCall newFuncCall = new()
                        {
                            name = funcCall.name,
                        };
                        foreach (Expression expr in funcCall.args)
                            newFuncCall.args.Add(RewriteExpression(expr, conversionDict, num));
                        return new Term(newFuncCall);
                    },
                    qStmt =>
                    {
                        QuantifiedStatement newStmt = new()
                        {
                            op = qStmt.op,
                            obj = $"_{qStmt.obj}{num}"
                        };
                        conversionDict.Add(qStmt.obj, new Term(newStmt.obj));
                        newStmt.stmt = RewriteExpression(qStmt.stmt, conversionDict, num);
                        conversionDict.Remove(qStmt.obj);
                        return new Term(newStmt);
                    },
                    str =>
                    {
                        if (conversionDict.ContainsKey(str))
                            return conversionDict[str];
                        else
                            return new Term(str);
                    },
                    num => new Term(num)
                );
            }
        );
    }
    private bool CompareExpressions(Expression a, Expression b)
    {
        if (a.Index != b.Index) return false;

        if (a.TryAs<BinExpr>(out var binA))
        {
            var binB = b.As<BinExpr>();
            return (binA.op == binB.op)
                && CompareExpressions(binA.lhs, binB.lhs)
                && CompareExpressions(binA.rhs, binB.rhs);
        }
        else
        {
            var termA = a.As<Term>().term;
            var termB = b.As<Term>().term;
            if (termA.Index != termB.Index) return false;

            return termA.Match(
                expr => CompareExpressions(expr, termB.As<Expression>()),
                funcCall => { throw new NotImplementedException(); },
                qStmt => { throw new NotImplementedException(); },
                str => str == termB.As<string>(),
                num => num == termB.As<double>()
            );
        }
    }
}
