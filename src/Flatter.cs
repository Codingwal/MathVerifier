using MathVerifier.AST;
using MathVerifier.Flat;

namespace MathVerifier.Services;

public class Context
{
    public List<VarDefinition> Definitions = [];
    public List<VarId> Statements = [];
    public ScopeStack<string> Variables = new();

    public VarId AddDefinition(VarDefinition def)
    {
        int index = Definitions.FindIndex(d => d == def);
        if (index != -1) return (uint)index;

        Definitions.Add(def);
        return (uint)(Definitions.Count - 1);
    }
}

public class Flatter
{
    private Context Flatten(Theorem theorem)
    {

    }

    private void Flatten(ExpressionLine exprLine, Context context)
    {
        VarId id = Flatten(exprLine.Expr, context);
        context.Statements.Add(id);
    }
    private VarInfo Flatten(Expression expr, Context context)
    {
        if (expr is BinExpr binExpr)
        {
            return FlattenFuncCall(new Variable(binExpr.Op.ToString()), [binExpr.Lhs, binExpr.Rhs], context);
        }
        else if (expr is Variable var)
        {
            context.AddDefinition(new LoadVarDefinition(var.Str, 0));
        }
    }

    private VarInfo FlattenFuncCall(Expression func, List<Expression> args, Context context)
    {
        VarInfo funcInfo = Flatten(func, context);
        if (funcInfo.ArgMapping.Count != 0) throw new();

        List<string> argMapping = [];

        List<VarRef> argRefs = [];
        foreach (var arg in args)
        {
            VarInfo argInfo = Flatten(arg, context);

            List<VarId> argIds = [];
            foreach (var argName in argInfo.ArgMapping)
            {
                int argIndex = argMapping.FindIndex(n => n == argName);
                if (argIndex == -1)
                {
                    argMapping.Add(argName);
                    argIds.Add(new((uint)argMapping.Count - 1, Generic: true));
                }
                else
                    argIds.Add(new((uint)argIndex, Generic: true));
            }

            argRefs.Add(new(argInfo.Id, argIds));
        }

        VarId funcCallId = context.AddDefinition(new FunctionVarDefinition(funcInfo.Id, argRefs, argMapping.Count));
        return new(funcCallId, argMapping);
    }

    private record VarInfo(VarId Id, List<string> ArgMapping);
}