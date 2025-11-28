using TokenType = Token.TokenType;

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

    private void VerifyExpression(Expression expr, int line)
    {
        if (expr.TryAs<BinExpr>(out var binExpr))
        {
            // Check if binExpr.op is a Expr x Expr => Expr operator
            Logger.Assert(Token.GetPrecedence(binExpr.op.type) >= Token.ExpressionMinPrec,
                $"Invalid expression operator \"{binExpr.op}\" in line {line}");

            // TODO: Check if operator identifier is defined
            if (binExpr.op.type == TokenType.STRING)
                throw new NotImplementedException();

            VerifyExpression(binExpr.lhs, line);
            VerifyExpression(binExpr.rhs, line);
        }
        else
        {
            var term = expr.As<Term>().term;

            term.Switch(expr => VerifyExpression(expr, line),
                        funcCall => throw new NotImplementedException(),
                        qStmt => Logger.Error($"Expected expression but found quantified statement in line {line}"),
                        str => Logger.Assert(objects.Contains(str), $"Undefined identifier \"{str}\" in line {line}"),
                        num => { }
            );
        }
    }
}