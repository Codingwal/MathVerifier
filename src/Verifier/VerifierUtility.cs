public partial class Verifier
{
    private static IExpression RenameIterationVars(IExpression old)
    {
        Dictionary<string, IExpression> conversionDict = [];

        IExpression? Callback(IExpression expr)
        {
            if (expr is QuantifiedStatement qStmt)
            {
                conversionDict.Add(qStmt.obj, new Variable($"_{qStmt.obj}"));
                IExpression newStmt = RewriteExpression(qStmt.stmt, conversionDict, Callback);
                conversionDict.Remove(qStmt.obj);

                return new QuantifiedStatement() { op = qStmt.op, obj = $"_{qStmt.obj}", stmt = newStmt };
            }
            else if (expr is SetBuilder setBuilder)
            {
                conversionDict.Add(setBuilder.obj, new Variable($"_{setBuilder.obj}"));
                IExpression newStmt = RewriteExpression(setBuilder.requirement, conversionDict, Callback);
                conversionDict.Remove(setBuilder.obj);

                return new SetBuilder() { obj = $"_{setBuilder.obj}", requirement = newStmt };
            }
            return null;
        }

        return RewriteExpression(old, conversionDict, Callback);
    }
    private static IExpression RewriteExpression(IExpression old, Dictionary<string, IExpression> conversionDict, Func<IExpression, IExpression?>? callback = null)
    {
        // Use callback if present
        if (callback != null)
        {
            IExpression? tmp = callback(old);
            if (tmp != null)
                return tmp;
        }

        List<IExpression> RewriteList(List<IExpression> list)
        {
            List<IExpression> newList = [];
            foreach (IExpression expr in list)
                newList.Add(RewriteExpression(expr, conversionDict, callback));
            return newList;
        }

        // TODO: use switch?b

        if (old is BinExpr binExpr)
        {
            return new BinExpr()
            {
                lhs = RewriteExpression(binExpr.lhs, conversionDict, callback),
                op = binExpr.op,
                rhs = RewriteExpression(binExpr.rhs, conversionDict, callback),
            };
        }
        else if (old is FuncCall funcCall)
        {
            string name = "";
            if (conversionDict.TryGetValue(funcCall.name, out var newExpr))
            {
                if (newExpr is not Variable var)
                    Logger.Error($"Expression \"{Utility.Expr2Str(newExpr)}\" can't be used as a function (previous name: {funcCall.name}).");
                else
                    name = var.str;
            }
            else name = funcCall.name;

            FuncCall newFuncCall = new() { name = name, args = RewriteList(funcCall.args) };
            return newFuncCall;
        }
        else if (old is QuantifiedStatement qStmt)
            return new QuantifiedStatement() { op = qStmt.op, obj = qStmt.obj, stmt = RewriteExpression(qStmt.stmt, conversionDict, callback) };
        else if (old is Variable var)
        {
            if (conversionDict.TryGetValue(var.str, out IExpression? value))
                return value;
            else
                return new Variable(var.str);
        }
        else if (old is UnaryExpr unExpr)
            return new UnaryExpr() { op = unExpr.op, expr = RewriteExpression(unExpr.expr, conversionDict, callback) };
        else if (old is Tuple tuple)
            return new Tuple() { elements = RewriteList(tuple.elements) };
        else if (old is SetEnumNotation setEnumNotation)
            return new SetEnumNotation() { elements = RewriteList(setEnumNotation.elements) };
        else if (old is SetBuilder setBuilder)
            return new SetBuilder() { obj = setBuilder.obj, requirement = RewriteExpression(setBuilder.requirement, conversionDict, callback) };
        else
            throw new();
    }
    private bool CompareExpressions(IExpression a, IExpression b, bool compareUsingStatements = true, Func<IExpression, IExpression, bool>? callback = null)
    {
        if (compareUsingStatements)
            if (CompareUsingStatements(a, b))
                return true;

        if (callback != null)
            if (callback(a, b))
                return true;

        if (a.GetType() != b.GetType()) return false;

        bool CompareList(List<IExpression> a, List<IExpression> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
                if (!CompareExpressions(a[i], b[i], compareUsingStatements, callback)) return false;
            return true;
        }

        // TODO: rewrite as switch?

        if (a is BinExpr binA)
        {
            var binB = (BinExpr)b;
            return (binA.op == binB.op)
                && CompareExpressions(binA.lhs, binB.lhs, compareUsingStatements, callback)
                && CompareExpressions(binA.rhs, binB.rhs, compareUsingStatements, callback);
        }
        else if (a is FuncCall funcCallA)
        {
            if (funcCallA.name != ((FuncCall)b).name) return false;
            return CompareList(funcCallA.args, ((FuncCall)b).args);
        }
        else if (a is QuantifiedStatement qStmtA)
        {
            var qStmtB = (QuantifiedStatement)b;
            if (qStmtA.op != qStmtB.op) return false;
            return CompareExpressions(qStmtA.stmt, RewriteExpression(qStmtB.stmt, new() { { qStmtB.obj, new Variable(qStmtA.obj) } }), compareUsingStatements, callback);
        }
        else if (a is Variable var)
        {
            return var.str == ((Variable)b).str;
        }
        else if (a is UnaryExpr unExprA)
        {
            var unExprB = (UnaryExpr)b;
            return unExprA.op == unExprB.op && CompareExpressions(unExprA.expr, unExprB.expr, compareUsingStatements, callback);
        }
        else if (a is Tuple tupleA)
        {
            return CompareList(tupleA.elements, ((Tuple)b).elements);
        }
        else if (a is SetEnumNotation setEnumNotationA)
        {
            return CompareList(setEnumNotationA.elements, ((SetEnumNotation)b).elements);
        }
        else if (a is SetBuilder setBuilderA)
        {
            var setBuilderB = (SetBuilder)b;
            IExpression reqBRewritten = RewriteExpression(setBuilderB.requirement, new() { { setBuilderB.obj, new Variable(setBuilderA.obj) } });
            return CompareExpressions(setBuilderA.requirement, reqBRewritten, compareUsingStatements, callback);
        }
        else throw new();
    }
    private bool CompareUsingStatements(IExpression a, IExpression b)
    {
        foreach (var stmt in statements.GetAll())
        {
            if (stmt is not BinExpr binExpr || binExpr.op != new Token(TokenType.EQUALS))
                continue;

            // Check if a = b or b = a is a proven statement
            if (CompareExpressions(new BinExpr() { lhs = a, op = new(TokenType.EQUALS), rhs = b }, stmt, compareUsingStatements: false))
                return true;
            if (CompareExpressions(new BinExpr() { lhs = b, op = new(TokenType.EQUALS), rhs = a }, stmt, compareUsingStatements: false))
                return true;

            // Check if a ⇔ b or b ⇔ a is a proven statement
            if (CompareExpressions(new BinExpr() { lhs = a, op = new(TokenType.EQUIVALENT), rhs = b }, stmt, compareUsingStatements: false))
                return true;
            if (CompareExpressions(new BinExpr() { lhs = b, op = new(TokenType.EQUIVALENT), rhs = a }, stmt, compareUsingStatements: false))
                return true;
        }
        return false;
    }

    private static void ForEach(IExpression expr, Action<IExpression> callback)
    {
        callback(expr);

        void ForEachList(List<IExpression> list)
        {
            foreach (var e in list)
                ForEach(e, callback);
        }

        switch (expr)
        {
            case BinExpr binExpr:
                ForEach(binExpr.lhs, callback); ForEach(binExpr.rhs, callback); break;
            case FuncCall funcCall:
                ForEachList(funcCall.args); break;
            case QuantifiedStatement qStmt:
                ForEach(qStmt.stmt, callback); break;
            case Variable var:
                break;
            case UnaryExpr unExpr:
                ForEach(unExpr.expr, callback); break;
            case Tuple tuple:
                ForEachList(tuple.elements); break;
            case SetEnumNotation setEnumNotation:
                ForEachList(setEnumNotation.elements); break;
            case SetBuilder setBuilder:
                ForEach(setBuilder.requirement, callback); break;
            default:
                throw new();
        }
    }

    private static bool Find(IExpression expr, Func<IExpression, bool> predicate)
    {
        bool found = false;

        ForEach(expr, expr =>
        {
            if (predicate(expr))
                found = true;
        });

        return found;
    }

    // Compare a and b
    // If two objects are compared and the object of a is the parameter replace,
    // All occurences of the object in a are handled as if they were the object that b had in the first comparison
    private bool CompareExpressionsReplaceFirstMismatch(IExpression a, IExpression b, string replace)
    {
        IExpression? newExpr = null;

        bool Compare(IExpression a, IExpression b)
        {
            if (a is not Variable varA) return false;

            if (varA.str == replace)
            {
                newExpr ??= b;

                // Compare with newObject instead of the object to replace
                if (CompareExpressions(newExpr, b, compareUsingStatements: false))
                    return true;
            }
            return false;
        }

        bool val = CompareExpressions(a, b, compareUsingStatements: false, Compare);
        return val;
    }

    private static List<string> GetAllObjects(IExpression expr)
    {
        List<string> objects = [];

        ForEach(expr, expr =>
        {
            if (expr is not Variable var) return;

            if (!objects.Contains(var.str))
                objects.Add(var.str);
        });

        return objects;
    }

    private static bool ContainsReplaceArgs(IExpression expr)
    {
        return Find(expr,
                expr => expr is Variable var
                    && var.str[0] == '_'
                    && char.IsNumber(var.str[1]));
    }
}