using MathVerifier.AST;
using MathVerifier.IR;
using MathVerifier.Tokens;

namespace MathVerifier.Services;

public static class Flatter
{
    public class Context
    {
        public List<VarDef> Definitions = [];
        public List<VarId> Statements = [];
        public Stack<string> QStmtVars = [];
        public List<string> Params = [];

        public VarId AddDefinition(VarDef def)
        {
            int index = Definitions.FindIndex(d => Comparer.IsEqual(d, def));
            if (index != -1) return new VarId((uint)index);

            Definitions.Add(def);
            return new VarId((uint)Definitions.Count - 1);
        }
        public void AddParam(string name)
        {
            Logger.Assert(!Params.Contains(name), $"Duplicate param name {name}");
            Params.Add(name);
            Definitions.Add(new FuncVarDef(new UserVar(name), [], 0));
        }

        public VarId? GetParam(string name)
        {
            int index = Params.FindIndex(p => p == name);
            if (index == -1) return null;
            return new VarId((uint)index);
        }
        public bool IsGeneric(string var)
        {
            return QStmtVars.Contains(var);
        }
    }

    public static IR.Data Flatten(AST.Data data)
    {
        List<Variant<IR.Theorem, IR.Definition>> blocks = [];
        foreach (var block in data.data)
        {
            if (block.TryAs<AST.Theorem>(out var theorem))
                blocks.Add(FlattenTheorem(theorem));
            else
                blocks.Add(FlattenDefinition(block.As<AST.Definition>()));
        }

        return new IR.Data(blocks);
    }
    private static IR.Theorem FlattenTheorem(AST.Theorem theorem)
    {
        Context context = new();

        foreach (var param in theorem.Params)
            context.AddParam(param);

        List<VarId> requirements = [];
        foreach (var req in theorem.Requirements)
            requirements.Add(FlattenExprStmt(req.Expr, context));

        VarId hypothesis = FlattenExprStmt(theorem.Hypothesis.Expr, context);

        FlattenScope(theorem.Proof, context);

        return new IR.Theorem(
            theorem.Name,
            theorem.Params.Count,
            context.Definitions,
            requirements,
            context.Statements,
            hypothesis
        );
    }
    private static IR.Definition FlattenDefinition(AST.Definition definition)
    {
        Context context = new();

        foreach (var param in definition.Params)
            context.AddParam(param);

        VarId expr = FlattenExprStmt(definition.Expr, context);

        return new IR.Definition(
            definition.Name,
            definition.Params.Count,
            context.Definitions,
            expr
        );
    }
    private static void FlattenScope(Scope scope, Context context)
    {
        foreach (var stmt in scope.Statements)
        {
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
    private static VarId FlattenExprStmt(Expression expr, Context context)
    {
        VarInfo exprInfo = FlattenExpr(expr, context);
        if (exprInfo.ArgMapping.Count != 0) throw new();
        if (exprInfo.Var is not VarId varId) throw new();
        return varId;
    }
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
        else
            throw new NotImplementedException(); // TODO: Tuple, SetEnumNotation, SetBuilder
    }

    private static VarInfo FlattenQuantifiedStatement(QuantifiedStatement qStmt, Context context)
    {
        if (qStmt.Op == TokenType.EXISTS)
            throw new NotImplementedException();

        foreach (var obj in qStmt.Objs)
            context.QStmtVars.Push(obj);

        VarInfo info = FlattenExpr(qStmt.Stmt, context);

        for (int i = 0; i < qStmt.Objs.Count; i++)
            context.QStmtVars.Pop();

        List<VarRef> args = [];
        List<string> argMapping = [];
        foreach (var arg in info.ArgMapping)
        {
            if (qStmt.Objs.Contains(arg))
            {
                args.Add(new VarRef(new StandardVar(StandardVar.Values.ALL), []));
            }
            else
            {
                argMapping.Add(arg);
                uint argId = (uint)argMapping.Count - 1;
                args.Add(new VarRef(new GenericVar(argId), []));
            }
        }

        VarId id = context.AddDefinition(new FuncVarDef(info.Var, args, argMapping.Count));
        return new VarInfo(id, argMapping);
    }

    private static VarInfo FlattenFuncCall(Token nameToken, List<Expression> args, Context context)
    {
        List<string> argMapping = [];

        Var funcVar;
        if (nameToken.type != TokenType.STRING)
        {
            StandardVar.Values? stdFunc = nameToken.type switch
            {
                TokenType.IMPLIES => StandardVar.Values.IMPLIES,
                TokenType.AND => StandardVar.Values.AND,
                TokenType.OR => StandardVar.Values.OR,
                TokenType.EQUALS => StandardVar.Values.EQUALS,
                TokenType.ELEMENT_OF => StandardVar.Values.ELEMENT_OF,
                _ => null
            };
            Logger.Assert(stdFunc != null, $"Unknown operator {nameToken}");
            funcVar = new StandardVar(stdFunc!.Value);
        }
        else
        {
            VarRef funcVarRef = GetVarRef(new Variable(nameToken.GetString()), argMapping, context);
            if (funcVarRef.Args.Count != 0) throw new();
            funcVar = funcVarRef.Var;
        }

        List<VarRef> argRefs = [];
        foreach (var arg in args)
        {
            argRefs.Add(GetVarRef(arg, argMapping, context));
        }

        VarId funcCallId = context.AddDefinition(new FuncVarDef(funcVar, argRefs, argMapping.Count));
        return new(funcCallId, argMapping);
    }

    private static VarRef GetVarRef(Expression expr, List<string> argMapping, Context context)
    {
        VarInfo info = FlattenExpr(expr, context);

        if (info.Var is GenericVar)
        {
            if (info.ArgMapping.Count != 1) throw new();
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
        // Handle generics
        if (context.IsGeneric(var.Str))
            return new VarInfo(new GenericVar(0), [var.Str]);

        // Handle params
        VarId? param = context.GetParam(var.Str);
        if (param != null) return new VarInfo(param, []);

        // Handle user vars
        return new VarInfo(new UserVar(var.Str), []);
    }

    private record VarInfo(Var Var, List<string> ArgMapping);


    private static class Comparer
    {
        public static bool IsEqual(Var a, Var b)
        {
            if (a.GetType() != b.GetType()) return false;

            return a switch
            {
                VarId varIdA => varIdA.Value == ((VarId)b).Value,
                UserVar userVarA => userVarA.Name == ((UserVar)b).Name,
                GenericVar genericVarA => genericVarA.Value == ((GenericVar)b).Value,
                StandardVar stdVarA => stdVarA.Value == ((StandardVar)b).Value,
                _ => throw new()
            };
        }
        public static bool IsEqual(VarRef a, VarRef b)
        {
            if (!IsEqual(a.Var, b.Var)) return false;
            if (a.Args.Count != b.Args.Count) return false;
            for (int i = 0; i < a.Args.Count; i++)
                if (!IsEqual(a.Args[i], b.Args[i])) return false;
            return true;
        }
        public static bool IsEqual(VarDef a, VarDef b)
        {
            if (a.GetType() != b.GetType()) return false;

            return a switch
            {
                FuncVarDef funcDefA => IsEqual(funcDefA, (FuncVarDef)b),
                LoadVarDef loadDefA => loadDefA.Origin == ((LoadVarDef)b).Origin,
                _ => throw new()
            };
        }
        public static bool IsEqual(FuncVarDef a, FuncVarDef b)
        {
            if (!IsEqual(a.Function, b.Function)) return false;
            if (a.Args.Count != b.Args.Count) return false;
            for (int i = 0; i < a.Args.Count; i++)
                if (!IsEqual(a.Args[i], b.Args[i])) return false;
            return true;
        }
    }
}