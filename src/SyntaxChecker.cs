public class SyntaxChecker
{
    private readonly Data ast;
    private ScopeStack<string> objects;
    private List<string> theoremNames;
    public SyntaxChecker(Data ast)
    {
        this.ast = ast;
        objects = new();
        theoremNames = new();
    }

    public void Check()
    {
        foreach (var e in ast.data)
            e.Switch(CheckTheorem, CheckDefinition);
    }
    private void CheckTheorem(Theorem theorem)
    {
        // Check name
        Logger.Assert(!theoremNames.Contains(theorem.name), $"A theorem with the name \"{theorem.name}\" has already been defined! (line {theorem.line})");
        theoremNames.Add(theorem.name);

        objects.EnterScope("Theorem");

        // Check parameters
        foreach (string param in theorem.parameters)
        {
            Logger.Assert(!objects.Contains(param), $"An object with the name \"{param}\" has already been defined! (line {theorem.line})");
            objects.Add(param);
        }

        // Check requirements
        foreach (var requirement in theorem.requirements)
            CheckStatementLine(requirement, allowProof: false, allowSorry: false, allowCheck: false);

        // Check hypothesis
        CheckStatementLine(theorem.hypothesis, allowProof: false, allowSorry: false, allowCheck: true);

        // Check proof
        foreach (var stmtLine in theorem.proof)
            CheckStatementLine(stmtLine, allowProof: true, allowSorry: true, allowCheck: true);

        objects.ExitScope("Theorem");
    }
    private void CheckDefinition(Definition definition)
    {
        // Check name
        Logger.Assert(!objects.Contains(definition.name), $"An object with the name {definition.name} has already been defined! (line {definition.line}).");
        objects.Add(definition.name);

        objects.EnterScope("Definition");

        // Check rules
        foreach (var rule in definition.rules)
            CheckStatementLine(rule, allowProof: false, allowSorry: false, allowCheck: false);

        objects.ExitScope("Definition");
    }

    private void CheckStatementLine(StatementLine stmtLine, bool allowProof, bool allowSorry, bool allowCheck)
    {
        // Assert that a proof is only present if allowed
        Logger.Assert(stmtLine.proof == null || allowProof, $"Unexpected proof in line {stmtLine.line}.");

        // Handle commands
        if (stmtLine.stmt.TryAs<Command>(out var cmd))
        {
            if (cmd == Command.CHECK)
                Logger.Assert(allowCheck, $"Unexpected command \"check\" in line {stmtLine.line}.");
            else if (cmd == Command.SORRY)
                Logger.Assert(allowSorry, $"Unexpected command \"sorry\" in line {stmtLine.line}.");
            else
                throw new();
            return;
        }

        // Check statement
        CheckExpression(stmtLine.stmt.As<Expression>(), stmtLine.line);

        // Check proof
        stmtLine.proof?.Switch(
            funcCall =>
            {
                Logger.Assert(theoremNames.Contains(funcCall.name), $"Reference to undefined theorem \"{funcCall.name}\" as proof in line {stmtLine.line}.");
                foreach (var arg in funcCall.args)
                    CheckExpression(arg, stmtLine.line);
            },
            definitionRef =>
            {
                Logger.Assert(objects.Contains(definitionRef), $"Reference to undefined object \"{definitionRef}\" as proof in line {stmtLine.line}.");
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