public static class Utility
{
    /// <summary>
    /// Convert an AST into a human readable expression string
    /// </summary>
    public static string Expr2Str(Expression expr)
    {
        return expr.Match(
            binExpr => $"{Expr2Str(binExpr.lhs)} {binExpr.op.ToSymbol()} {Expr2Str(binExpr.rhs)}",
            term =>
            {
                return term.term.Match(
                    e => Expr2Str(e),
                    funcCall =>
                    {
                        string str = $"{funcCall.name}(";
                        for (int i = 0; i < funcCall.args.Count; i++)
                            str += Expr2Str(funcCall.args[i]) + ((i + 1 < funcCall.args.Count) ? "," : "");
                        str += ")";
                        return str;
                    },
                    qStmt => $"{new Token(qStmt.op).ToSymbol()}{qStmt.obj}({Expr2Str(qStmt.stmt)})",
                    str => str,
                    unaryExpr => $"{unaryExpr.op.ToSymbol()}({Expr2Str(unaryExpr.expr)})",
                    tuple =>
                    {
                        string str = $"[";
                        for (int i = 0; i < tuple.elements.Count; i++)
                            str += Expr2Str(tuple.elements[i]) + ((i + 1 < tuple.elements.Count) ? "," : "");
                        str += "]";
                        return str;
                    }
                    );
            }
        );
    }
}
