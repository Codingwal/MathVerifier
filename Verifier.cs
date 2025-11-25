using TokenType = Token.TokenType;

public class Verifier
{
    private readonly Data ast;
    private List<string> objects;
    private List<Statement> statements;
    private Stack<Statement> statementStack; // Statements only valid in the given context (in quantified statement, in imply statement, ...)
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
                AddStatement(stmt);
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
            AddStatement(stmt);
        }
        VerifyStatementLine(theorem.hypothesis);
        statements.Clear();
        theorems.Add(theorem.name, theorem);
    }
    private void VerifyStatementLine(StatementLine stmt)
    {
        Logger.Assert(!stmt.stmt.Is<Command>(), $"Unexpected command {stmt.stmt.As<Command>()} in line {stmt.line}! Expected statement.");

        bool proven = false;
        stmt.stmt.As<Statement>().Switch(
            funcCall => // Substitute arguments into the theorem and create a new valid statement (hopefully equal to the statement to verify)
            {
                // Get theorem and verify FuncCall
                if (!theorems.ContainsKey(funcCall.name))
                    Logger.Error($"Reference to undefined theorem \"{funcCall.name}\" in line {stmt.stmt.line + 1}");
                Theorem theorem = theorems[funcCall.name];
                if (funcCall.args.Count != theorem.parameters.Count)
                    Logger.Error($"Incorrect argument count ({funcCall.args.Count}, expected {theorem.parameters.Count}) in line {stmt.stmt.line + 1}");

                // Rewrite expression with the given arguments
                Dictionary<string, Expression> conversionDict = new();
                for (int i = 0; i < theorem.parameters.Count; i++)
                {
                    conversionDict.Add(theorem.parameters[i], funcCall.args[i]);
                }
                statements.Add(new(stmt.stmt.line, RewriteExpression(theorem.hypothesis.Expr, conversionDict, num)));
                num++;
            },
            command => // Handle sorry command
            {
                if (command.type == Command.CommandType.SORRY)
                    proven = true;
                else
                    Logger.Error($"Unexpected command {command} as statement proof in line {stmt.stmt.line + 1}. Expected \"sorry\" or theorem reference.");
            },
            none => { }
        );
        if (proven) return; // Early out (for example because of sorry statement)

        // Check if this statement is already proven (or was just proven using a theorem reference)
        foreach (Statement s in statements)
            if (CompareExpressions(s.Expr, stmt.stmt.Expr))
                return;

        VerifyExpression(stmt.stmt.Expr, stmt.stmt.line);
    }
    private bool VerifyStatement(Statement stmt, int line)
    {
        return stmt.Match(
            s =>
            {
                if (s.op == TokenType.EXISTS)
                    return VerifyExistsStatement(s, line);
                else if (s.op == TokenType.FOR_ALL)
                    throw new NotImplementedException();
                else
                    throw new();
            },
            logicalOp =>
            {
                throw new NotImplementedException();
            },
            relationalOp =>
            {
                throw new NotImplementedException();
            },
            setStmt =>
            {
                throw new NotImplementedException();
            }
        );
    }


    private bool VerifyExistsStatement(QuantifiedStatement stmt, int line)
    {
        Logger.Assert(stmt.objects.Count == 1, $"Can't handle quantified statements with more than one object yet.");

        foreach (string obj in stmt.objects)
        {
            Logger.Assert(!objects.Contains(obj), $"An object with the same name (\"{obj}\") already exists (line {line}).");
            objects.Add(obj);
        }

        foreach (var s in statements)
        {
            if (!s.TryAs<QuantifiedStatement>(out var other)) continue;

            if (other.op == TokenType.EXISTS)
            {
                Dictionary<string, Expression> conversionDict = new() { { other.objects[0], new(new Term(stmt.objects[0])) } };

                // Add all statements of the other exists statement rewritten with the current object
                foreach (var rule in other.rules)
                    statementStack.Push(RewriteStatement(rule, conversionDict));
                statementStack.Push(RewriteStatement(other.stmt, conversionDict));
                int popCount = other.rules.Count + 1;

                // Check if all statements can be proven using the other exists statement
                if (stmt.rules.All(rule => VerifyStatement(RewriteStatement(rule, conversionDict), line))
                    && VerifyStatement(RewriteStatement(stmt.stmt, conversionDict), line))
                {
                    for (int i = 0; i < popCount; i++)
                        statementStack.Pop();
                    return true;
                }

                // Remove temporary statements
                for (int i = 0; i < popCount; i++)
                    statementStack.Pop();
            }
            else if (other.op == TokenType.FOR_ALL)
                throw new NotImplementedException();
            else
                throw new();
        }

        // Remove objects only valid inside the quantified statement
        foreach (string obj in stmt.objects)
            objects.Remove(obj);

        return false;
    }
    private Statement RewriteStatement(Statement stmt, Dictionary<string, Expression> conversionDict)
    {
        throw new NotImplementedException();
    }
    private void AddStatement(StatementLine stmt)
    {
        statements.Add(stmt);
        if (stmt.Expr.expr.TryAs<BinExpr>(out var binExpr))
        {
            if (binExpr.op.type == TokenType.ELEMENT_OF)
            {
                if (binExpr.rhs.expr.TryAs<Term>(out var term) && term.term.TryAs<string>(out var str))
                {
                    if (!definitions.ContainsKey(str))
                        Logger.Error($"Use of undefined set \"{str}\" in line {stmt.line + 1}");
                    Definition set = definitions[str];
                    foreach (var rule in set.rules)
                    {
                        Dictionary<string, Expression> conversionDict = new() { { set.obj, binExpr.lhs } };
                        statements.Add(new(stmt.line, RewriteExpression(rule.Expr, conversionDict, num)));
                    }
                    num++;
                }
                else
                    Logger.Error($"Failed to parse element of statement in line {stmt.line}");
            }
        }
    }
    private void VerifyExpression(Expression expr, int line)
    {
        expr.expr.Switch(
            binExpr =>
            {
                if (binExpr.op.type == TokenType.EQUALS)
                    if (CompareExpressions(binExpr.lhs, binExpr.rhs))
                        return;
                Logger.Error($"Unverifiable expression in line {line + 1}");
            },
            term => { },
            qExpr => { }
        );
    }
    private bool CompareExpressions(Expression expr1, Expression expr2)
    {
        if (expr1.expr.Index != expr2.expr.Index)
            return false;

        return expr1.expr.Match(
            binExpr1 =>
            {
                BinExpr binExpr2 = expr2.expr.As<BinExpr>();
                return CompareExpressions(binExpr1.lhs, binExpr2.lhs)
                && binExpr1.op.Equals(binExpr2.op)
                && CompareExpressions(binExpr1.rhs, binExpr2.rhs);
            },
            term1 => term1.term.Equals(expr2.expr.As<Term>().term),
            qExpr1 =>
            {
                throw new NotImplementedException();
            }
        );
    }
    private Expression RewriteExpression(Expression old, Dictionary<string, Expression> conversionDict, int num)
    {
        Expression newExpr = new();
        old.expr.Switch(
            binExpr =>
            {
                newExpr.expr = new BinExpr()
                {
                    lhs = RewriteExpression(binExpr.lhs, conversionDict, num),
                    op = binExpr.op,
                    rhs = RewriteExpression(binExpr.rhs, conversionDict, num),
                };
            },
            term =>
            {
                term.term.Switch(
                    expr => newExpr.expr = new Term(RewriteExpression(expr, conversionDict, num)),
                    str =>
                    {
                        if (conversionDict.ContainsKey(str))
                            newExpr = conversionDict[str];
                        else
                            newExpr = new(new Term(str));
                    },
                    num => newExpr.expr = new Term(num)
                );
            },
            quantifiedExpr =>
            {
                QuantifiedExpr expr = new() { type = quantifiedExpr.type };

                // Copy object names and rename them to provent duplicates
                foreach (var name in quantifiedExpr.objects)
                {
                    string newName = $"_{name}{num}";
                    expr.objects.Add(newName);
                    conversionDict.Add(name, new(new Term(newName)));
                }

                foreach (var rule in quantifiedExpr.rules)
                    expr.rules.Add(new(rule.line, RewriteExpression(rule.Expr, conversionDict, num)));

                expr.stmt = new(quantifiedExpr.stmt.line, RewriteExpression(quantifiedExpr.stmt.Expr, conversionDict, num));
                newExpr.expr = expr;
            }
        );
        return newExpr;
    }
}
