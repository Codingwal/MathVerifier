public static class Utility
{
    /// <summary>
    /// Convert an AST into a human readable expression string
    /// </summary>
    public static string Expr2Str(Expression expr)
    {
        string ExprList2Str(List<Expression> list)
        {
            string str = "";
            for (int i = 0; i < list.Count; i++)
                str += Expr2Str(list[i]) + ((i + 1 < list.Count) ? "," : "");
            return str;
        }

        return expr.Match(
            binExpr => $"{Expr2Str(binExpr.lhs)} {binExpr.op.ToSymbol()} {Expr2Str(binExpr.rhs)}",
            term =>
            {
                return term.term.Match(
                    e => Expr2Str(e),
                    funcCall => $"{funcCall.name}({ExprList2Str(funcCall.args)})",
                    qStmt => $"{new Token(qStmt.op).ToSymbol()}{qStmt.obj}({Expr2Str(qStmt.stmt)})",
                    str => str,
                    unaryExpr => $"{unaryExpr.op.ToSymbol()}({Expr2Str(unaryExpr.expr)})",
                    tuple => $"[{ExprList2Str(tuple.elements)}]",
                    setEnumNotation => $"{{{ExprList2Str(setEnumNotation.elements)}}}"
                    );
            }
        );
    }
}
