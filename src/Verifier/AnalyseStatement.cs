public partial class Verifier
{
    private StmtVal AnalyseStatement(Expression expr, int line)
    {
        // Check if the statement has already been proven
        foreach (var stmt in statements.GetAll())
            if (CompareExpressions(stmt, expr))
                return StmtVal.TRUE;

        // Analyse statement recursively
        return expr.Match(
            binExpr => AnalyseBinExpr(binExpr, line),
            term => AnalyseTerm(term, line));
    }

    private StmtVal AnalyseBinExpr(BinExpr binExpr, int line)
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
                    return CompareExpressions(binExpr.lhs, binExpr.rhs) ? StmtVal.TRUE : StmtVal.UNKNOWN;
                }
            case TokenType.ELEMENT_OF:
            case TokenType.SUBSET:
                return StmtVal.UNKNOWN;
            default:
                Logger.Error($"Invalid statement operator {binExpr.op} in line {line}");
                throw new();
        }
    }

    private StmtVal AnalyseTerm(Term term, int line)
    {
        return term.term.Match(
            expr => AnalyseStatement(expr, line),
            funcCall => StmtVal.UNKNOWN,
            qStmt =>
            {
                if (qStmt.op == TokenType.FOR_ALL)
                {
                    StmtVal val = AnalyseStatement(qStmt.stmt, line);
                    return val;
                }
                else if (qStmt.op == TokenType.EXISTS)
                {
                    // ∃x(ϕ(x)) ⇔ ¬∀x(¬ϕ(x))
                    return AnalyseStatement(new Term(new UnaryExpr() // ¬∀x(¬ϕ(x))
                    {
                        op = new(TokenType.NOT),
                        expr = new Term(new QuantifiedStatement() // ∀x(¬ϕ(x))
                        {
                            op = TokenType.FOR_ALL,
                            obj = qStmt.obj,
                            stmt = new Term(new UnaryExpr() // ¬ϕ(x)
                            {
                                op = new(TokenType.NOT),
                                expr = qStmt.stmt           // ϕ(x)
                            })
                        })
                    }), line);
                }
                else
                    throw new();
            },
            str => StmtVal.UNKNOWN,
            unaryExpr =>
            {
                switch (unaryExpr.op.type)
                {
                    case TokenType.NOT:
                        {
                            StmtVal val = AnalyseStatement(unaryExpr.expr, line);
                            return val switch
                            {
                                StmtVal.TRUE => StmtVal.FALSE,
                                StmtVal.FALSE => StmtVal.TRUE,
                                StmtVal.UNKNOWN => StmtVal.UNKNOWN,
                                _ => throw new()
                            };
                        }
                    default:
                        throw new();
                }
            });
    }

}