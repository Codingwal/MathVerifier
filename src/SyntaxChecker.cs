public class SyntaxChecker
{
    private readonly Data ast;
    private ScopeStack<string> objects;
    private Dictionary<string, Theorem> theorems;
    private List<string> definitions;
    public SyntaxChecker(Data ast)
    {
        this.ast = ast;
        objects = new();
        theorems = new();
        definitions = new();
    }

    public void Check()
    {
        foreach (var e in ast.data)
            e.Switch(CheckTheorem, CheckDefinition);
    }
    private void CheckTheorem(Theorem theorem)
    {
        // Check name
        Logger.Assert(!theorems.ContainsKey(theorem.name), $"A theorem with the name \"{theorem.name}\" has already been defined! (line {theorem.line})");

        objects.EnterScope("Theorem");

        // Check parameters
        foreach (string param in theorem.parameters)
        {
            Logger.Assert(param[0] != '_', $"Object names are not allowed to start with '_'! (line {theorem.line})");
            Logger.Assert(!objects.Contains(param), $"An object with the name \"{param}\" has already been defined! (line {theorem.line})");
            objects.Add(param);
        }

        // Check requirements
        foreach (var requirement in theorem.requirements)
            CheckExpressionLine(requirement);

        // Check hypothesis
        CheckExpressionLine(theorem.hypothesis);

        // Check proof
        CheckScope(theorem.proof);

        objects.ExitScope("Theorem");
        theorems.Add(theorem.name, theorem);
    }
    private void CheckDefinition(Definition definition)
    {
        // Check name
        Logger.Assert(definition.name[0] != '_', $"Object names are not allowed to start with '_'! (line {definition.line})");
        Logger.Assert(!objects.Contains(definition.name), $"An object with the name {definition.name} has already been defined! (line {definition.line}).");
        objects.Add(definition.name);
        definitions.Add(definition.name);

        objects.EnterScope("Definition");

        // Check rules
        foreach (var rule in definition.rules)
            CheckExpressionLine(rule);

        objects.ExitScope("Definition");

        CheckScope(definition.proof);
    }
    private void CheckExpressionLine(ExpressionLine exprLine)
    {
        CheckExpression(exprLine.expr, exprLine.line);
    }
    private void CheckStatementLine(StatementLine stmtLine)
    {
        // Handle sorry statement
        if (stmtLine.stmt.TryAs<Command>(out var cmd) && cmd == Command.SORRY)
        {
            Logger.Assert(stmtLine.proof == null, $"Unexpected proof in line {stmtLine.line}.");
            return;
        }

        // Handle conditional statements
        if (stmtLine.stmt.TryAs<ConditionalStatement>(out var condStmt))
        {
            CheckExpressionLine(condStmt.condition);

            objects.EnterScope("If");
            CheckScope(condStmt.ifScope);
            objects.ExitScope("If");

            objects.EnterScope("Else");
            CheckScope(condStmt.ifScope);
            objects.ExitScope("Else");

            objects.EnterScope("Both");
            CheckScope(condStmt.ifScope);
            objects.ExitScope("Both");

            return;
        }

        // Handle definition statements (let x: P(x))
        if (stmtLine.stmt.TryAs<DefinitionStatement>(out var defStmt))
        {
            Logger.Assert(defStmt.obj[0] != '_', $"Object names are not allowed to start with '_'! (line {stmtLine.line})");
            Logger.Assert(!objects.Contains(defStmt.obj), $"An object with name \"{defStmt.obj}\" has already been defined! (line {stmtLine.line})");
            objects.Add(defStmt.obj);
            CheckExpression(defStmt.stmt, stmtLine.line);
        }
        else if (stmtLine.stmt.TryAs<Command>(out var command))
            Logger.Assert(command == Command.CHECK, $"Expected check command in line {stmtLine.line}");
        else
            CheckExpression(stmtLine.stmt.As<Expression>(), stmtLine.line);

        // Check proof
        stmtLine.proof?.Switch(
            funcCall =>
            {
                Logger.Assert(theorems.ContainsKey(funcCall.name), $"Reference to undefined theorem \"{funcCall.name}\" as proof in line {stmtLine.line}.");
                Logger.Assert(funcCall.args.Count == theorems[funcCall.name].parameters.Count,
                    $"Expected {theorems[funcCall.name].parameters.Count} arguments but found {funcCall.args.Count} at reference to theorem {funcCall.name} in line {stmtLine.line}");

                objects.EnterScope("Proof-FuncCall");

                for (int i = 0; i < 5; i++)
                    objects.Add($"_{i}");

                // foreach (var arg in funcCall.args)
                // CheckExpression(arg, stmtLine.line);

                objects.ExitScope("Proof-FuncCall");
            },
            definitionRef =>
            {
                Logger.Assert(definitions.Contains(definitionRef), $"Reference to undefined object \"{definitionRef}\" as proof in line {stmtLine.line}.");
            },
            cmd =>
            {
                Logger.Assert(cmd == Command.SORRY, $"Unexpected command {cmd} as proof in line {stmtLine.line}.");
            });
    }

    private void CheckScope(Scope scope)
    {
        foreach (var stmtLine in scope.statements)
            CheckStatementLine(stmtLine);
    }

    private void CheckExpression(Expression expr, int line)
    {
        expr.Switch(
            binExpr =>
            {
                CheckExpression(binExpr.lhs, line);
                if (binExpr.op.type == TokenType.STRING)
                    Logger.Assert(objects.Contains(binExpr.op.GetString()), $"Reference to undefined binary operator \"{binExpr.op.GetString()}\" in line {line}.");
                CheckExpression(binExpr.rhs, line);
            },
            term =>
            {
                term.term.Switch(
                    expr => CheckExpression(expr, line),
                    funcCall =>
                    {
                        Logger.Assert(objects.Contains(funcCall.name), $"Reference to undefined function \"{funcCall.name}\" in line {line}.");
                        foreach (var arg in funcCall.args)
                            CheckExpression(arg, line);
                    },
                    qStmt =>
                    {
                        // Operator is checked on creation
                        Logger.Assert(qStmt.obj[0] != '_', $"Object names are not allowed to start with '_'! (line {line})");
                        Logger.Assert(!objects.Contains(qStmt.obj), $"An object with name \"{qStmt.obj}\" has already been defined! (line {line})");
                        objects.EnterScope("Quantified statement");
                        objects.Add(qStmt.obj);
                        CheckExpression(qStmt.stmt, line);
                        objects.ExitScope("Quantified statement");
                    },
                    str => Logger.Assert(objects.Contains(str), $"Reference to undefined object \"{str}\" in line {line}."),
                    unExpr =>
                    {
                        // Operator is checked on creation
                        CheckExpression(unExpr.expr, line);
                    },
                    tuple =>
                    {
                        foreach (var e in tuple.elements)
                            CheckExpression(e, line);
                    });
            });
    }
}