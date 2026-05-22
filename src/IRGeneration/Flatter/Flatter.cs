using MathVerifier.AST;
using MathVerifier.IR;

namespace MathVerifier.Services.Flatter;

public static partial class Flatter
{
    public static IR.Data Flatten(AST.Data data)
    {
        List<IR.Definition> definitions = [];
        List<IR.Theorem> theorems = [];
        foreach (var block in data.data)
        {
            if (block.TryAs<AST.Theorem>(out var theorem))
                theorems.Add(FlattenTheorem(theorem));
            else
                definitions.Add(FlattenDefinition(block.As<AST.Definition>()));
        }

        return new IR.Data(definitions, theorems);
    }

    private static IR.Definition FlattenDefinition(AST.Definition definition)
    {
        Context context = new();

        foreach (var param in definition.Params)
            context.AddParam(param);

        VarInfo expr = FlattenExpr(definition.Expr, context);
        if (expr.Var is not VarId) throw new();

        // Verify that all generics left are parameters
        // TODO: Order (?)
        foreach (var arg in expr.ArgMapping)
            Logger.Assert(context.IsParam(arg), $"Undefined parameter {arg} in definition {definition.Name}");

        return new IR.Definition(
            definition.Name,
            definition.Params.Count,
            context.VarDefs,
            (VarId)expr.Var
        );
    }

    private static IR.Theorem FlattenTheorem(AST.Theorem theorem)
    {
        TheoremContext context = new();

        foreach (var param in theorem.Params)
            context.AddParam(param);

        context.Requirements = theorem.Requirements.Select(req => req.Expr).ToList();

        VarId hypothesis = FlattenExprStmt(theorem.Hypothesis.Expr, context);

        FlattenScope(theorem.Proof, context);

        return new IR.Theorem(
            theorem.Name,
            context.VarDefs,
            context.Statements,
            hypothesis
        );
    }
    private static void FlattenScope(Scope scope, TheoremContext context)
    {
        foreach (var stmt in scope.Statements)
        {
            if (stmt.Proof != null)
            {
                if (stmt.Proof is FuncCall funcCall)
                {
                    List<Var> args = [];
                    foreach (var arg in funcCall.Args)
                    {
                        VarInfo info = FlattenExpr(arg, context);
                        Logger.Assert(info.ArgMapping.Count == 0, "Theorem reference arguments cannot have generic args");
                        args.Add(info.Var);
                    }

                    context.Statements.Add(new TheoremRef(funcCall.Name, args));
                }
                else
                    throw new NotImplementedException();
            }

            switch (stmt.Stmt)
            {
                case Expression expr:
                    VarId varId = FlattenExprStmt(expr, context);
                    context.Statements.Add(varId);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }

    // Flatten a statement expression in the body of a theorem. The actually flattened expression is "∀params(requirements ⇒ expr)".
    // This way, a statement remains valid completely independently of the context.
    private static VarId FlattenExprStmt(Expression expr, TheoremContext context)
    {
        // Use all requirements as the lhs of an implication
        if (context.Requirements.Count > 0)
            expr = new BinExpr(CombineExpressions(context.Requirements, new(TokenType.AND)), new(TokenType.IMPLIES), expr);

        // Wrap the expression in a universal quantifier with all parameters
        expr = new QuantifiedStatement(TokenType.FOR_ALL, context.Params, expr);

        VarInfo exprInfo = FlattenExpr(expr, context);

        Logger.Assert(exprInfo.ArgMapping.Count == 0, "Invalid statement. Did you forget a quantifier?");
        Logger.Assert(exprInfo.Var is VarId, "Statements cannot be a single user/standard value");

        return (VarId)exprInfo.Var;
    }
}