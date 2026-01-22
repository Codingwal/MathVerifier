public static class Utility
{
    /// <summary>
    /// Convert an AST into a human readable expression string
    /// </summary>
    public static string Expr2Str(IExpression expr)
    {
        static string ExprList2Str(List<IExpression> list)
        {
            string str = "";
            for (int i = 0; i < list.Count; i++)
                str += Expr2Str(list[i]) + ((i + 1 < list.Count) ? "," : "");
            return str;
        }

        return expr switch
        {
            BinExpr binExpr => $"{Expr2Str(binExpr.lhs)} {binExpr.op.ToSymbol()} {Expr2Str(binExpr.rhs)}",
            FuncCall funcCall => $"{funcCall.name}({ExprList2Str(funcCall.args)})",
            QuantifiedStatement qStmt => $"{new Token(qStmt.op).ToSymbol()}{qStmt.obj}({Expr2Str(qStmt.stmt)})",
            UnaryExpr unExpr => $"{unExpr.op.ToSymbol()}({Expr2Str(unExpr.expr)})",
            Variable var => var.str,
            Tuple tuple => $"[{ExprList2Str(tuple.elements)}]",
            SetEnumNotation setEnumNotation => $"{{{ExprList2Str(setEnumNotation.elements)}}}",
            SetBuilder setBuilder => $"{{{setBuilder.obj}: {Expr2Str(setBuilder.requirement)}}}",
            _ => throw new()
        };
    }
}
