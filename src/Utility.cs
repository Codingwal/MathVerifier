using MathVerifier.AST;
using MathVerifier.Tokens;

namespace MathVerifier;

public static class Utility
{
    /// <summary>
    /// Convert an AST into a human readable expression string
    /// </summary>
    public static string Expr2Str(Expression expr)
    {
        static string ExprList2Str(List<Expression> list)
        {
            string str = "";
            for (int i = 0; i < list.Count; i++)
                str += Expr2Str(list[i]) + ((i + 1 < list.Count) ? "," : "");
            return str;
        }

        return expr switch
        {
            BinExpr binExpr => $"({Expr2Str(binExpr.Lhs)} {binExpr.Op.ToSymbol()} {Expr2Str(binExpr.Rhs)})",
            FuncCall funcCall => $"{funcCall.Name}({ExprList2Str(funcCall.Args)})",
            QuantifiedStatement qStmt => $"{new Token(qStmt.Op).ToSymbol()}{qStmt.Obj}({Expr2Str(qStmt.Stmt)})",
            UnaryExpr unExpr => $"{unExpr.Op.ToSymbol()}({Expr2Str(unExpr.Expr)})",
            Variable var => var.Str,
            AST.Tuple tuple => $"[{ExprList2Str(tuple.Elements)}]",
            SetEnumNotation setEnumNotation => $"{{{ExprList2Str(setEnumNotation.Elements)}}}",
            SetBuilder setBuilder => $"{{{setBuilder.Obj}: {Expr2Str(setBuilder.Requirement)}}}",
            _ => throw new()
        };
    }
}
