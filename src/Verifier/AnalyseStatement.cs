public partial class Verifier
{
    private StmtVal AnalyseStatement(Expression expr, int line, bool recursion = true)
    {
        // Check if the statement has already been proven
        foreach (var stmt in statements.GetAll())
            if (ProofStatementWith(expr, stmt, recursion))
                return StmtVal.TRUE;

        // Analyse statement recursively
        return expr.Match(
            binExpr => AnalyseBinExpr(binExpr, line, recursion),
            term => AnalyseTerm(term, line, recursion));
    }

    private StmtVal AnalyseBinExpr(BinExpr binExpr, int line, bool recursion)
    {
        switch (binExpr.op.type)
        {
            case TokenType.IMPLIES:
                {
                    StmtVal lhs = AnalyseStatement(binExpr.lhs, line, recursion);

                    // Add statements valid in this context
                    statements.EnterScope("Implies");
                    statements.Add(binExpr.lhs);

                    StmtVal rhs = AnalyseStatement(binExpr.rhs, line, recursion);

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
                    StmtVal lhs = AnalyseStatement(binExpr.lhs, line, recursion);
                    StmtVal rhs = AnalyseStatement(binExpr.rhs, line, recursion);

                    if (lhs == StmtVal.UNKNOWN || rhs == StmtVal.UNKNOWN)
                        return StmtVal.UNKNOWN;
                    else
                        return lhs == rhs ? StmtVal.TRUE : StmtVal.FALSE;
                }
            case TokenType.OR:
                {
                    StmtVal lhs = AnalyseStatement(binExpr.lhs, line, recursion);
                    StmtVal rhs = AnalyseStatement(binExpr.rhs, line, recursion);

                    if (lhs == StmtVal.TRUE || rhs == StmtVal.TRUE)
                        return StmtVal.TRUE;
                    else if (lhs == StmtVal.FALSE && rhs == StmtVal.FALSE)
                        return StmtVal.FALSE;
                    else
                        return StmtVal.UNKNOWN;
                }
            case TokenType.AND:
                {
                    StmtVal lhs = AnalyseStatement(binExpr.lhs, line, recursion);
                    StmtVal rhs = AnalyseStatement(binExpr.rhs, line, recursion);

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
            case TokenType.STRING:
                return StmtVal.UNKNOWN;
            default:
                Logger.Error($"Invalid statement operator {binExpr.op} in line {line}");
                throw new();
        }
    }

    private StmtVal AnalyseTerm(Term term, int line, bool recursion)
    {
        return term.term.Match(
            expr => AnalyseStatement(expr, line, recursion),
            funcCall => StmtVal.UNKNOWN,
            qStmt =>
            {
                if (qStmt.op == TokenType.FOR_ALL)
                {
                    StmtVal val = AnalyseStatement(qStmt.stmt, line, recursion);
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
                    }), line, recursion);
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
                            StmtVal val = AnalyseStatement(unaryExpr.expr, line, recursion);
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
            },
            tuple =>
            {
                Logger.Error($"Expected statement but found tuple in line {line}");
                throw new();
            },
            setEnumNotation =>
            {
                Logger.Error($"Expected statement but found set (enumeration notation) in line {line}");
                throw new();
            });
    }

    private bool ProofStatementWith(Expression stmt, Expression other, bool recursion = true)
    {
        if (CompareExpressions(stmt, other))
            return true;

        if (stmt.TryAs<Term>(out var termA) && termA.term.TryAs<QuantifiedStatement>(out var qStmtA) && qStmtA.op == TokenType.EXISTS)
        {
            // If there is an object for which P is true, ∃x(P(x)) is also true
            if (CompareExpressionsReplaceFirstMismatch(qStmtA.stmt, other, qStmtA.obj))
                return true;
        }

        return other.Match(
            binExpr => recursion && ProofStatementWithBinExpr(stmt, binExpr), // Proof with binary expression uses recursion
            termB => termB.term.Match(
                exprB => ProofStatementWith(stmt, exprB, recursion),
                funcCallB => false,
                qStmtB =>
                {
                    if (qStmtB.op == TokenType.FOR_ALL)
                    {
                        // Generate statement from quantified statement for eached object used in statement
                        // This does not work for terms (e.g. "a + b")
                        foreach (string obj in GetAllObjects(stmt))
                            if (ProofStatementWith(stmt, RewriteExpression(qStmtB.stmt, new() { { qStmtB.obj, new Term(obj) } }), recursion))
                                return true;

                        // Use statement of quantified statement but replace the iteration variable with the
                        // object used in the statement at its place
                        if (CompareExpressionsReplaceFirstMismatch(qStmtB.stmt, stmt, qStmtB.obj))
                            return true;
                    }
                    return false;
                },
                strB => false,
                unExprB => false,
                tupleB => throw new(),
                setEnumNotation => throw new()));
    }

    private bool ProofStatementWithBinExpr(Expression stmt, BinExpr binExpr)
    {
        if (Token.GetBinOpType(binExpr.op.type) != Token.BinOpType.Stmt2Stmt)
            return false;

        StmtVal lhs = AnalyseStatement(binExpr.lhs, -1, recursion: false);
        StmtVal rhs = AnalyseStatement(binExpr.rhs, -1, recursion: false);

        switch (binExpr.op.type)
        {
            case TokenType.EQUIVALENT:
                // L ⇔ R and L implies R
                if (lhs == StmtVal.TRUE)
                    if (ProofStatementWith(stmt, binExpr.rhs)) return true;
                // L ⇔ R and R implies L
                if (rhs == StmtVal.TRUE)
                    if (ProofStatementWith(stmt, binExpr.lhs)) return true;
                break;
            case TokenType.IMPLIES:
                // L ⇒ R and L implies R
                if (lhs == StmtVal.TRUE)
                    if (ProofStatementWith(stmt, binExpr.rhs)) return true;
                break;
            case TokenType.AND:
                // L ∧ R implies L and R
                if (ProofStatementWith(stmt, binExpr.lhs)) return true;
                if (ProofStatementWith(stmt, binExpr.rhs)) return true;
                break;
            case TokenType.OR:
                // L ∨ R and ¬L implies R
                if (lhs == StmtVal.FALSE)
                    if (ProofStatementWith(stmt, binExpr.rhs)) return true;
                // L ∨ R and ¬R implies L
                if (rhs == StmtVal.FALSE)
                    if (ProofStatementWith(stmt, binExpr.lhs)) return true;
                break;
            default:
                throw new();
        }
        return false;
    }
}
