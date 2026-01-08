public partial class Verifier
{
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
    private bool CompareExpressions(Expression a, Expression b, bool compareUsingStatements = true)
    {
        if (compareUsingStatements)
            if (CompareUsingStatements(a, b))
                return true;

        if (a.Index != b.Index) return false;

        if (a.TryAs<BinExpr>(out var binA))
        {
            var binB = b.As<BinExpr>();
            return (binA.op == binB.op)
                && CompareExpressions(binA.lhs, binB.lhs, compareUsingStatements)
                && CompareExpressions(binA.rhs, binB.rhs, compareUsingStatements);
        }
        else
        {
            var termA = a.As<Term>().term;
            var termB = b.As<Term>().term;
            if (termA.Index != termB.Index) return false;

            return termA.Match(
                expr => CompareExpressions(expr, termB.As<Expression>(), compareUsingStatements),
                funcCall =>
                {
                    if (funcCall.name != termB.As<FuncCall>().name) return false;
                    for (int i = 0; i < funcCall.args.Count; i++)
                        if (!CompareExpressions(funcCall.args[i], termB.As<FuncCall>().args[i], compareUsingStatements)) return false;
                    return true;
                },
                qStmtA =>
                {
                    var qStmtB = termB.As<QuantifiedStatement>();
                    if (qStmtA.op != qStmtB.op) return false;
                    return CompareExpressions(qStmtA.stmt, RewriteExpression(qStmtB.stmt, new() { { qStmtB.obj, new Term(qStmtA.obj) } }, num++), compareUsingStatements);
                },
                str => str == termB.As<string>(),
                unExprA =>
                {
                    var unExprB = termB.As<UnaryExpr>();
                    return unExprA.op == unExprB.op && CompareExpressions(unExprA.expr, unExprB.expr, compareUsingStatements);
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
            if (CompareExpressions(equalStmtA, stmt, compareUsingStatements: false))
                return true;
            if (CompareExpressions(equalStmtB, stmt, compareUsingStatements: false))
                return true;
        }
        return false;
    }
}