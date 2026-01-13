using System.Linq.Expressions;

public partial class Verifier
{
    private Expression RewriteExpression(Expression old, Dictionary<string, Expression> conversionDict, Func<Expression, Expression?>? callback = null)
    {
        if (callback != null)
        {
            Expression? tmp = callback(old);
            if (tmp != null)
                return tmp;
        }


        return old.Match(
            binExpr =>
            {
                return new BinExpr()
                {
                    lhs = RewriteExpression(binExpr.lhs, conversionDict, callback),
                    op = binExpr.op,
                    rhs = RewriteExpression(binExpr.rhs, conversionDict, callback),
                };
            },
            term =>
            {
                return term.term.Match(
                    expr => RewriteExpression(expr, conversionDict, callback),
                    funcCall =>
                    {
                        string name = "";
                        if (conversionDict.TryGetValue(funcCall.name, out var newExpr))
                        {
                            if (!newExpr.TryAs<Term>(out var term) || !term.term.TryAs(out string newName))
                                Logger.Error($"Expected object as argument (parameter {funcCall.name} is used as a function)");
                            else
                                name = newName;
                        }
                        else name = funcCall.name;

                        FuncCall newFuncCall = new() { name = name };
                        foreach (Expression expr in funcCall.args)
                            newFuncCall.args.Add(RewriteExpression(expr, conversionDict, callback));
                        return new Term(newFuncCall);
                    },
                    qStmt =>
                    {
                        QuantifiedStatement newStmt = new()
                        {
                            op = qStmt.op,
                            obj = $"_{qStmt.obj}"
                        };
                        conversionDict.Add(qStmt.obj, new Term(newStmt.obj));
                        newStmt.stmt = RewriteExpression(qStmt.stmt, conversionDict, callback);
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
                    unExpr =>
                    {
                        return new Term(new UnaryExpr()
                        {
                            op = unExpr.op,
                            expr = RewriteExpression(unExpr.expr, conversionDict, callback),
                        });
                    }
                );
            }
        );
    }
    private bool CompareExpressions(Expression a, Expression b, bool compareUsingStatements = true, Func<Expression, Expression, bool>? callback = null)
    {
        // Brackets should not make a difference
        if (a.TryAs<Term>(out var tA) && tA.term.TryAs<Expression>(out var eA))
            a = eA;
        if (b.TryAs<Term>(out var tB) && tB.term.TryAs<Expression>(out var eB))
            b = eB;

        if (compareUsingStatements)
            if (CompareUsingStatements(a, b))
                return true;

        if (callback != null)
            if (callback(a, b))
                return true;

        if (a.Index != b.Index) return false;

        if (a.TryAs<BinExpr>(out var binA))
        {
            var binB = b.As<BinExpr>();
            return (binA.op == binB.op)
                && CompareExpressions(binA.lhs, binB.lhs, compareUsingStatements, callback)
                && CompareExpressions(binA.rhs, binB.rhs, compareUsingStatements, callback);
        }
        else
        {
            var termA = a.As<Term>().term;
            var termB = b.As<Term>().term;
            if (termA.Index != termB.Index) return false;

            return termA.Match(
                expr => CompareExpressions(expr, termB.As<Expression>(), compareUsingStatements, callback),
                funcCall =>
                {
                    if (funcCall.name != termB.As<FuncCall>().name) return false;
                    for (int i = 0; i < funcCall.args.Count; i++)
                        if (!CompareExpressions(funcCall.args[i], termB.As<FuncCall>().args[i], compareUsingStatements, callback)) return false;
                    return true;
                },
                qStmtA =>
                {

                    var qStmtB = termB.As<QuantifiedStatement>();
                    if (qStmtA.op != qStmtB.op) return false;
                    return CompareExpressions(qStmtA.stmt, RewriteExpression(qStmtB.stmt, new() { { qStmtB.obj, new Term(qStmtA.obj) } }), compareUsingStatements, callback);
                },
                str => str == termB.As<string>(),
                unExprA =>
                {
                    var unExprB = termB.As<UnaryExpr>();
                    return unExprA.op == unExprB.op && CompareExpressions(unExprA.expr, unExprB.expr, compareUsingStatements, callback);
                }
            );
        }
    }
    private bool CompareUsingStatements(Expression a, Expression b)
    {
        Expression equalStmtA = new BinExpr()
        {
            lhs = a,
            op = new(TokenType.EQUALS),
            rhs = b
        };
        Expression equalStmtB = new BinExpr()
        {
            lhs = b,
            op = new(TokenType.EQUALS),
            rhs = a
        };

        foreach (var stmt in statements.GetAll())
        {
            if (!stmt.TryAs<BinExpr>(out var binExpr) || binExpr.op != new Token(TokenType.EQUALS))
                continue;

            if (CompareExpressions(equalStmtA, stmt, compareUsingStatements: false))
                return true;
            if (CompareExpressions(equalStmtB, stmt, compareUsingStatements: false))
                return true;
        }
        return false;
    }

    private bool Find(Expression expr, Func<Expression, bool> predicate)
    {
        if (predicate(expr))
            return true;

        return expr.Match(
            binExpr => Find(binExpr.lhs, predicate) || Find(binExpr.rhs, predicate),
            term => term.term.Match(
                expr => Find(expr, predicate),
                funcCall =>
                {
                    foreach (var arg in funcCall.args)
                        if (Find(arg, predicate)) return true;
                    return false;
                },
                qStmt => Find(qStmt.stmt, predicate),
                str => false,
                unExpr => Find(unExpr.expr, predicate)
                )
            );
    }

    // Compare a and b
    // If two objects are compared and the object of a is the parameter replace,
    // All occurences of the object in a are handled as if they were the object that b had in the first comparison
    private bool CompareExpressionsReplaceFirstMismatch(Expression a, Expression b, string replace)
    {
        Expression? newExpr = null;

        bool Compare(Expression a, Expression b)
        {
            // Only handle object comparisons
            if (!a.TryAs<Term>(out var termA) || !termA.term.TryAs<string>(out var strA))
                return false;
            // if (!b.TryAs<Term>(out var termB) || !termB.term.TryAs<string>(out var strB))
            //     return false;

            if (strA == replace)
            {
                newExpr ??= b;

                // Compare with newObject instead of the object to replace
                if (CompareExpressions(newExpr, b, compareUsingStatements: false))
                    return true;
            }
            return false;
        }

        // Console.WriteLine($"Comparing.\n{ExpressionBuilder.ExpressionToString(a)}\n{ExpressionBuilder.ExpressionToString(b)}");

        bool val = CompareExpressions(a, b, compareUsingStatements: false, Compare);
        // Console.WriteLine(val);
        return val;
    }
}