public static class Utility
{
    /// <summary>
    /// Convert an AST into a human readable expression string
    /// </summary>
    public static string Expr2Str(IExpression expr)
    {
        string ExprList2Str(List<IExpression> list)
        {
            string str = "";
            for (int i = 0; i < list.Count; i++)
                str += Expr2Str(list[i]) + ((i + 1 < list.Count) ? "," : "");
            return str;
        }

        if (expr is BinExpr binExpr)
            return $"{Expr2Str(binExpr.lhs)} {binExpr.op.ToSymbol()} {Expr2Str(binExpr.rhs)}";
        else if (expr is FuncCall funcCall)
            return $"{funcCall.name}({ExprList2Str(funcCall.args)})";
        else if (expr is QuantifiedStatement qStmt)
            return $"{new Token(qStmt.op).ToSymbol()}{qStmt.obj}({Expr2Str(qStmt.stmt)})";
        else if (expr is Variable var)
            return var.str;
        else if (expr is UnaryExpr unExpr)
            return $"{unExpr.op.ToSymbol()}({Expr2Str(unExpr.expr)})";
        else if (expr is Tuple tuple)
            return $"[{ExprList2Str(tuple.elements)}]";
        else if (expr is SetEnumNotation setEnumNotation)
            return $"{{{ExprList2Str(setEnumNotation.elements)}}}";
        else
            throw new();
    }
}
