

/// <summary>
/// Convert an AST into a human readable expression string
/// </summary>
public static class ExpressionBuilder
{
    public static string ExpressionToString(Expression expr)
    {
        return expr.Match(
            binExpr => $"{ExpressionToString(binExpr.lhs)} {binExpr.op.ToSymbol()} {ExpressionToString(binExpr.rhs)}",
            term =>
            {
                return term.term.Match(
                    e => ExpressionToString(e),
                    funcCall =>
                    {
                        string str = $"{funcCall.name}(";
                        for (int i = 0; i < funcCall.args.Count; i++)
                            str += ExpressionToString(funcCall.args[i]) + ((i + 1 < funcCall.args.Count) ? "," : "");
                        str += ")";
                        return str;
                    },
                    qStmt => $"{new Token(qStmt.op).ToSymbol()}{qStmt.obj}({ExpressionToString(qStmt.stmt)})",
                    str => str,
                    unaryExpr => $"{unaryExpr.op.ToSymbol()}({ExpressionToString(unaryExpr.expr)})"
                    );
            }
        );
    }
}
