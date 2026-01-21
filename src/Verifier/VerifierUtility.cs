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
                        if (conversionDict.TryGetValue(str, out Expression? value))
                            return value;
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
                    },
                    tuple =>
                    {
                        Tuple newTuple = new();
                        foreach (var e in tuple.elements)
                            newTuple.elements.Add(RewriteExpression(e, conversionDict, callback));
                        return new Term(newTuple);
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
                },
                tupleA =>
                {
                    var tupleB = termB.As<Tuple>();
                    for (int i = 0; i < tupleA.elements.Count; i++)
                        if (!CompareExpressions(tupleA.elements[i], tupleB.elements[i], compareUsingStatements, callback))
                            return false;
                    return true;
                }
            );
        }
    }
    private bool CompareUsingStatements(Expression a, Expression b)
    {
        foreach (var stmt in statements.GetAll())
        {
            if (!stmt.TryAs<BinExpr>(out var binExpr) || binExpr.op != new Token(TokenType.EQUALS))
                continue;

            // Check if a = b or b = a is a proven statement
            if (CompareExpressions(new BinExpr() { lhs = a, op = new(TokenType.EQUALS), rhs = b }, stmt, compareUsingStatements: false))
                return true;
            if (CompareExpressions(new BinExpr() { lhs = b, op = new(TokenType.EQUALS), rhs = a }, stmt, compareUsingStatements: false))
                return true;
        }
        return false;
    }

    private void ForEach(Expression expr, Action<Expression> callback)
    {
        callback(expr);

        expr.Switch(
            binExpr =>
            {
                ForEach(binExpr.lhs, callback);
                ForEach(binExpr.rhs, callback);
            },
            term => term.term.Switch(
                expr => ForEach(expr, callback),
                funcCall =>
                {
                    foreach (var arg in funcCall.args)
                        ForEach(arg, callback);
                },
                qStmt => ForEach(qStmt.stmt, callback),
                str => { },
                unExpr => ForEach(unExpr.expr, callback),
                tuple =>
                {
                    foreach (var e in tuple.elements)
                        ForEach(e, callback);
                }));
    }

    private bool Find(Expression expr, Func<Expression, bool> predicate)
    {
        bool found = false;

        ForEach(expr, expr =>
        {
            if (predicate(expr))
                found = true;
        });

        return found;
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

        bool val = CompareExpressions(a, b, compareUsingStatements: false, Compare);
        return val;
    }

    private List<string> GetAllObjects(Expression expr)
    {
        List<string> objects = new();

        ForEach(expr, expr =>
        {
            if (!expr.TryAs<Term>(out var term)) return;
            if (!term.term.TryAs<string>(out var str)) return;

            if (!objects.Contains(str))
                objects.Add(str);
        });

        return objects;
    }

    private bool ContainsReplaceArgs(Expression expr)
    {
        return Find(expr,
                expr => expr.TryAs<Term>(out var term)
                    && term.term.TryAs<string>(out var str)
                    && str[0] == '_');
    }
}