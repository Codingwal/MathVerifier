using TokenType = Token.TokenType;

public class Verifier
{
    private readonly Data ast;
    private List<Statement> statements;
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
            if (stmt.stmt.TryAs<Command>(out var cmd))
            {
                if (cmd.type == Command.CommandType.CHECK)
                {
                    Console.WriteLine("\nCurrent statements:");
                    Console.Write(Formatter.Format(statements));
                    Console.WriteLine("-------------------\n");
                }
                else
                    Logger.Error($"Invalid command {cmd} in theorem requirements");
            }
            else
                AddStatement(stmt);
        }
        foreach (var stmt in theorem.proof)
        {
            if (stmt.stmt.stmt.TryAs<Command>(out var cmd))
            {
                if (cmd.type == Command.CommandType.SORRY)
                {
                    statements.Clear();
                    theorems.Add(theorem.name, theorem);
                    return;
                }
                else if (cmd.type == Command.CommandType.CHECK)
                {
                    Console.WriteLine("\nCurrent statements:");
                    Console.Write(Formatter.Format(statements));
                    Console.WriteLine("-------------------\n");
                }
                continue;
            }
            VerifyStatement(stmt);
            AddStatement(stmt.stmt);
        }
        VerifyStatement(new() { stmt = theorem.hypothesis });
        statements.Clear();
        theorems.Add(theorem.name, theorem);
    }
    private void VerifyStatement(ProvenStatement stmt)
    {
        stmt.theorem.Switch(
            funcCall =>
            {

            },
            command =>
            {
                if (command.type == Command.CommandType.SORRY)
                    return;
                else
                    Logger.Error($"Unexpected command {command} as statement proof. Expected \"sorry\" or theorem reference.");
            },
            none => { }
        );
        bool proven = false;
        foreach (Statement s in statements)
            if (CompareExpressions(s.Expr, stmt.stmt.Expr))
            {
                proven = true;
                break;
            }
        if (!proven)
            Logger.Error("Unproven statement");
    }
    private void AddStatement(Statement stmt)
    {
        statements.Add(stmt);
        if (stmt.Expr.expr.TryAs<BinExpr>(out var binExpr))
        {
            if (binExpr.op.type == TokenType.ELEMENT_OF)
            {
                if (binExpr.rhs.expr.TryAs<Term>(out var term) && term.term.TryAs<string>(out var str))
                {
                    if (!definitions.ContainsKey(str))
                        Logger.Error($"Use of undefined set \"{str}\"");
                    Definition set = definitions[str];
                    foreach (var rule in set.rules)
                    {
                        Dictionary<string, Expression> conversionDict = new() { { set.obj, binExpr.lhs } };
                        statements.Add(new(RewriteExpression(rule.Expr, conversionDict, num)));
                    }
                    num++;
                }
                else
                    Logger.Error("Failed to parse element of statement");
            }
        }
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
                    expr.rules.Add(new(RewriteExpression(rule.Expr, conversionDict, num)));

                expr.stmt = new(RewriteExpression(quantifiedExpr.stmt.Expr, conversionDict, num));
                newExpr.expr = expr;
            }
        );
        return newExpr;
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
                && CompareExpressions(binExpr1.lhs, binExpr2.lhs);
            },
            term1 => term1.term.Equals(expr2.expr.As<Term>().term),
            qExpr1 =>
            {
                throw new NotImplementedException();
            }
        );
    }
}