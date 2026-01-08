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
            CheckStatementLine(requirement, allowProof: false, allowCommands: false, allowDefStmt: false);

        // Check hypothesis
        CheckStatementLine(theorem.hypothesis, allowProof: false, allowCommands: false, allowDefStmt: false);

        // Check proof
        foreach (var stmtLine in theorem.proof)
            CheckStatementLine(stmtLine, allowProof: true, allowCommands: true, allowDefStmt: true);

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
            CheckStatementLine(rule, allowProof: false, allowCommands: false, allowDefStmt: false);

        objects.ExitScope("Definition");
    }

    private void CheckStatementLine(StatementLine stmtLine, bool allowProof, bool allowCommands, bool allowDefStmt)
    {
        // Assert that a proof is only present if allowed
        Logger.Assert(stmtLine.proof == null || allowProof, $"Unexpected proof in line {stmtLine.line}.");

        // Handle commands
        if (stmtLine.stmt.TryAs<Command>(out var cmd))
        {
            if (cmd == Command.CHECK)
                Logger.Assert(allowCommands, $"Unexpected command \"check\" in line {stmtLine.line}.");
            else if (cmd == Command.SORRY)
                Logger.Assert(allowCommands, $"Unexpected command \"sorry\" in line {stmtLine.line}.");
            else
                throw new();
            return;
        }

        // Handle definition statements (let x: P(x))
        if (stmtLine.stmt.TryAs<DefinitionStatement>(out var defStmt))
        {
            Logger.Assert(allowDefStmt, $"Unexpected definition statement in line {stmtLine.line}.");
            Logger.Assert(stmtLine.proof == null, $"Unexpected proof in line {stmtLine.line}.");
            Logger.Assert(defStmt.obj[0] != '_', $"Object names are not allowed to start with '_'! (line {stmtLine.line})");
            Logger.Assert(!objects.Contains(defStmt.obj), $"An object with name \"{defStmt.obj}\" has already been defined! (line {stmtLine.line})");
            objects.Add(defStmt.obj);
            CheckExpression(defStmt.stmt, stmtLine.line);
            return;
        }

        // Check statement
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

                foreach (var arg in funcCall.args)
                    CheckExpression(arg, stmtLine.line);

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
                    });
            });
    }
}