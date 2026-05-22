
using MathVerifier.AST;
using MathVerifier.IR;
using MathVerifier.Tokens;

namespace MathVerifier.Services.Flatter;

public static partial class Flatter
{
    private static VarInfo FlattenExpr(Expression expr, Context context)
    {
        if (expr is BinExpr binExpr)
            return FlattenFuncCall(binExpr.Op, [binExpr.Lhs, binExpr.Rhs], context);
        else if (expr is UnaryExpr unExpr)
            return FlattenFuncCall(unExpr.Op, [unExpr.Expr], context);
        else if (expr is FuncCall funcCall)
            return FlattenFuncCall(new Token(funcCall.Name), funcCall.Args, context);
        else if (expr is QuantifiedStatement qStmt)
            return FlattenQuantifiedStatement(qStmt, context);
        else if (expr is Variable var)
            return FlattenVar(var, context);
        else if (expr is TruthValue truthValue)
            return new(new StandardVar(truthValue.Value ? StandardVar.Values.TRUE : StandardVar.Values.FALSE), []);
        else
            throw new NotImplementedException(); // TODO: Tuple, SetEnumNotation, SetBuilder
    }

    private static VarInfo FlattenQuantifiedStatement(QuantifiedStatement qStmt, Context context)
    {
        if (qStmt.Op == TokenType.EXISTS)
            throw new NotImplementedException();

        // Add to stack of qStmt vars so that the callee knows this is a qStmt var
        foreach (var obj in qStmt.Objs)
            context.PushQStmtVar(obj);

        VarInfo info = FlattenExpr(qStmt.Stmt, context);

        // Pop all previously added qStmt vars
        for (int i = 0; i < qStmt.Objs.Count; i++)
            context.PopQStmtVar();

        List<VarRef> args = [];
        List<string> argMapping = []; // Which generic slot corresponds with which user variable name?
        foreach (var arg in info.ArgMapping)
        {
            if (qStmt.Objs.Contains(arg))
            {
                // Use an universal quantifier for this generic arg slot
                args.Add(new VarRef(new StandardVar(StandardVar.Values.ALL), []));
            }
            else
            {
                // Use a generic var for this generic arg slot. The referenced varDef is expected to be well-formed,
                // so arg is not a duplicate and can safely be added to argMapping.
                argMapping.Add(arg);
                uint argId = (uint)argMapping.Count - 1;
                args.Add(new VarRef(new GenericVar(argId), []));
            }
        }

        // for example: "t5: t4 .0 all all .1"
        VarId id = context.AddVarDef(new FuncVarDef(info.Var, args, argMapping.Count));

        return new VarInfo(id, argMapping);
    }

    private static VarInfo FlattenFuncCall(Token nameToken, List<Expression> argsOld, Context context)
    {
        List<string> argMapping = []; // Which generic slot corresponds to which user variable name?

        Var funcVar;
        if (nameToken.type != TokenType.STRING)
        {
            // If the functions is not a string (user-defined), it must be a standard operator/function
            StandardVar.Values? stdFunc = nameToken.type switch
            {
                TokenType.IMPLIES => StandardVar.Values.IMPLIES,
                TokenType.AND => StandardVar.Values.AND,
                TokenType.OR => StandardVar.Values.OR,
                TokenType.EQUALS => StandardVar.Values.EQUALS,
                TokenType.ELEMENT_OF => StandardVar.Values.ELEMENT_OF,
                TokenType.NOT => StandardVar.Values.NOT,
                _ => null
            };
            Logger.Assert(stdFunc != null, $"Unknown operator {nameToken}");
            funcVar = new StandardVar(stdFunc!.Value);
        }
        else
        {
            // Handle user defined functions
            funcVar = new UserVar(nameToken.GetString());
        }

        // Flatten args
        List<VarRef> argsNew = [];
        foreach (var arg in argsOld)
        {
            argsNew.Add(GetVarRef(arg, argMapping, context));
        }

        VarId funcCallId = context.AddVarDef(new FuncVarDef(funcVar, argsNew, argMapping.Count));
        return new(funcCallId, argMapping);
    }

    private static VarRef GetVarRef(Expression expr, List<string> argMapping, Context context)
    {
        VarInfo info = FlattenExpr(expr, context);

        // GenericVars must be handled seperately, as they are of the form ".1" instead of "t0[.1]"
        if (info.Var is GenericVar) // The value contained in the GenericVar is just a placeholder, the caller doesn't know which generic slot to use
        {
            if (info.ArgMapping.Count != 1) throw new(); // Must contain the mapping for this specific generic
            uint argId = AddOrGetArgId(info.ArgMapping[0]);
            return new VarRef(new GenericVar(argId), []);
        }

        // Check which generic arg ids should be used for the args for this var
        List<GenericVar> args = [];
        foreach (var argName in info.ArgMapping)
        {
            uint argId = AddOrGetArgId(argName);
            args.Add(new GenericVar(argId));
        }

        return new VarRef(info.Var, args);


        uint AddOrGetArgId(string argName)
        {
            int argIndex = argMapping.FindIndex(n => n == argName);
            if (argIndex == -1)
            {
                argMapping.Add(argName);
                return (uint)argMapping.Count - 1;
            }
            return (uint)argIndex;
        }
    }

    private static VarInfo FlattenVar(Variable var, Context context)
    {
        // Handle generics and parameters
        if (context.IsGeneric(var.Str) || context.IsParam(var.Str))
            return new VarInfo(new GenericVar(0), [var.Str]);

        // Handle user vars
        return new VarInfo(new UserVar(var.Str), []);
    }

    // Combine multiple expressions into a single one using a specific binary operator.
    // For example: [10, 5*a, b+3], "∧" => "10 ∧ 5*a ∧ b+3"
    private static Expression CombineExpressions(List<Expression> expressions, Token binOp)
    {
        Expression expr = expressions[0];
        for (int i = 1; i < expressions.Count; i++)
        {
            expr = new BinExpr(expr, binOp, expressions[i]);
        }
        return expr;
    }

    // Information about a flattened expression, sent to the caller
    private record VarInfo(
        Var Var,
        List<string> ArgMapping // Which generic slot corresponds with which user variable name?
    );
}