public partial class Verifier
{
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