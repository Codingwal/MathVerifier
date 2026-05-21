using MathVerifier.AST;
using MathVerifier.IR;
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
            QuantifiedStatement qStmt => $"{new Token(qStmt.Op).ToSymbol()}{string.Join(",", qStmt.Objs)}({Expr2Str(qStmt.Stmt)})",
            UnaryExpr unExpr => $"{unExpr.Op.ToSymbol()}({Expr2Str(unExpr.Expr)})",
            Variable var => var.Str,
            AST.Tuple tuple => $"[{ExprList2Str(tuple.Elements)}]",
            SetEnumNotation setEnumNotation => $"{{{ExprList2Str(setEnumNotation.Elements)}}}",
            SetBuilder setBuilder => $"{{{setBuilder.Obj}: {Expr2Str(setBuilder.Requirement)}}}",
            _ => throw new()
        };
    }
}

public static class IRPrinter
{
    public static string Data2Str(IR.Data data)
    {
        string str = "";
        foreach (var block in data.Blocks)
        {
            if (block.TryAs<IR.Theorem>(out var theorem))
                str += Theorem2Str(theorem);
            else
                str += Definition2Str(block.As<IR.Definition>());
        }
        return str;
    }

    private static string Theorem2Str(IR.Theorem theorem)
    {
        string str = $"theorem {theorem.Name}({Params2Str(theorem.ParamCount)}):\n";

        for (int i = 0; i < theorem.Defs.Count; i++)
            str += $"    {VarDef2Str(theorem.Defs[i], i)}\n";

        str += "  requirements:\n";
        foreach (var req in theorem.Requirements)
            str += $"    {Var2Str(req)}\n";

        str += "  proof statements:\n";
        foreach (var stmt in theorem.ProofStmts)
            str += $"    {Var2Str(stmt)}\n";

        str += "  hypothesis:\n";
        str += $"    {Var2Str(theorem.Hypothesis)}\n";

        return str + "\n";
    }

    private static string Definition2Str(IR.Definition definition)
    {
        string str = $"define {definition.Name}({Params2Str(definition.ParamCount)}):\n";

        for (int i = 0; i < definition.Defs.Count; i++)
            str += $"    {VarDef2Str(definition.Defs[i], i)}\n";

        str += "  def:\n";
        str += $"    {Var2Str(definition.Def)}\n";

        return str + "\n";
    }

    private static string VarDef2Str(VarDef varDef, int varId)
    {
        return $"t{varId}: {VarDef2StrHelper(varDef)}    <{varDef.ArgsCount}>";
    }
    private static string VarDef2StrHelper(VarDef varDef)
    {
        if (varDef is FuncVarDef funcVarDef)
        {
            string str = $"{Var2Str(funcVarDef.Function)} ";
            foreach (var arg in funcVarDef.Args)
            {
                str += $"{Var2Str(arg.Var)}";
                if (arg.Args.Count != 0) str += '[';
                foreach (var genericArg in arg.Args)
                {
                    str += Var2Str(genericArg);
                    if (genericArg != arg.Args.Last()) str += ", ";
                }
                if (arg.Args.Count != 0) str += ']';
                str += ' ';
            }
            return str;
        }
        else if (varDef is LoadVarDef loadVarDef)
            return $"load {loadVarDef.Origin}";
        else
            throw new NotImplementedException();
    }

    private static string Var2Str(Var var)
    {
        return var switch
        {
            VarId varId => $"t{varId.Value}",
            UserVar userVar => userVar.Name,
            GenericVar genericVar => $".{genericVar.Value}",
            StandardVar standardVar => standardVar.Value.ToString(),
            _ => throw new NotImplementedException()
        };
    }

    private static string Params2Str(int paramCount)
    {
        string str = "";
        for (int i = 0; i < paramCount; i++)
        {
            str += $"t{i}";
            if (i < paramCount - 1) str += ", ";
        }
        return str;
    }

    // private static string Enumerable2Str<T>(IEnumerable<T> enumerable, Func<T, string> element2Str)
    // {
    //     string str = "";
    //     foreach (var item in enumerable)
    //     {
    //         str += element2Str(item);
    //         if (!item!.Equals(enumerable.Last())) str += ", ";
    //     }
    //     return str;
    // }
}
