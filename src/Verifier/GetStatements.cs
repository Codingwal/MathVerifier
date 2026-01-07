public partial class Verifier
{
    private IEnumerable<Expression> GetAllStatements()
    {
        List<Expression> stmts = new();
        foreach (var stmt in statements.GetAll())
        {
            GetStatements(stmt, ref stmts);
            foreach (var s in stmts)
                yield return s;
            stmts.Clear();
        }
    }

    private void GetStatements(Expression stmt, ref List<Expression> stmts)
    {
        stmts.Add(stmt);

        // TODO:
        // iterate over all used objects and add the statement "∃x(...)" (the object is replaced with x)
        // For example: "0 ∈ ℕ" should generate "∃x(x ∈ ℕ)" and "∃x(0 ∈ x)"

        if (stmt.TryAs<BinExpr>(out var binExpr))
        {
            switch (binExpr.op.type)
            {
                case TokenType.IMPLIES:
                    // P ⇒ Q and P implies Q
                    if (StatementIsProven(binExpr.lhs))
                        GetStatements(binExpr.rhs, ref stmts);
                    break;
                case TokenType.AND:
                    // P ∧ Q implies P and Q
                    GetStatements(binExpr.lhs, ref stmts);
                    GetStatements(binExpr.rhs, ref stmts);
                    break;
                case TokenType.OR:
                    // TODO (can't use AnalyseStatement because this will cause an infinite recursion loop)
                    
                    // // P ∨ Q and ¬P implies Q
                    // if (AnalyseStatement(binExpr.lhs, line: -1) == StmtVal.FALSE)
                    //     GetStatements(binExpr.rhs, ref stmts);
                    // // Q ∨ P and ¬Q implies P
                    // if (AnalyseStatement(binExpr.rhs, line: -1) == StmtVal.FALSE)
                    //     GetStatements(binExpr.lhs, ref stmts);
                    break;
                case TokenType.EQUIVALENT:
                    // P ⇔ Q and P implies Q
                    if (StatementIsProven(binExpr.lhs))
                        GetStatements(binExpr.rhs, ref stmts);
                    // P ⇔ Q and Q implies P
                    if (StatementIsProven(binExpr.rhs))
                        GetStatements(binExpr.lhs, ref stmts);
                    break;
                default:
                    break;
            }
        }
        else if (stmt.TryAs<Term>(out var term))
        {
            if (term.term.TryAs<Expression>(out var expr))
            {
                GetStatements(expr, ref stmts);
            }
            else if (term.term.TryAs<QuantifiedStatement>(out var qStmt))
            {
                if (qStmt.op == TokenType.FOR_ALL)
                {
                    // ∀x(ϕ(x)) implies ∃x(ϕ(x))
                    QuantifiedStatement copy = qStmt;
                    copy.op = TokenType.EXISTS;
                    GetStatements(new Term(copy), ref stmts);

                    // TODO: substitute x with each object
                }
                else if (qStmt.op == TokenType.EXISTS)
                {
                    // TODO   
                }
            }
        }
    }
    private bool StatementIsProven(Expression stmt)
    {
        foreach (var s in statements.GetAll())
        {
            if (CompareExpressions(s, stmt))
                return true;
        }
        return false;
    }
}