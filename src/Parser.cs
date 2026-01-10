public class Parser
{
    private List<List<Token>> tokens;
    private int line;
    private int index;

    public Parser(List<List<Token>> tokens)
    {
        this.tokens = tokens;
        line = 1;
        index = 0;
    }

    private Token Peek()
    {
        return tokens[line - 1][index];
    }
    private Token Consume()
    {
        Token token = Peek();
        index++;

        // If the end of the line has been reached, skip lines until the next token is found
        while (index >= tokens[line - 1].Count)
        {
            index = 0;
            line++;
        }

        // If the token is of type NEWLINE, skip all additional NEWLINE tokens
        if (token.type == TokenType.NEWLINE)
        {
            while (Peek().type == TokenType.NEWLINE)
            {
                line++;
                index = 0;
            }
        }

        return token;
    }
    private Token ConsumeExpect(TokenType type)
    {
        Token consumed = Consume();
        if (consumed.type != type)
            Logger.Error($"Expected token of type \"{type}\" but found \"{consumed}\" in line {line}");
        return consumed;
    }

    public Data Parse()
    {
        Data data = new();
        while (Peek().type != TokenType.END_OF_FILE)
        {
            switch (Peek().type)
            {
                case TokenType.DEFINE:
                    data.data.Add(ParseDefinition());
                    break;
                case TokenType.THEOREM:
                    data.data.Add(ParseTheorem());
                    break;
                case TokenType.NEWLINE:
                    Consume();
                    break;
                default:
                    Logger.Error($"Invalid token \"{Peek()}\" outside of theorem/definition in line {line}");
                    break;
            }
        }
        return data;
    }
    private Definition ParseDefinition()
    {
        ConsumeExpect(TokenType.DEFINE);
        Definition definition = new()
        {
            name = ConsumeExpect(TokenType.STRING).GetString(),
            line = line
        };
        ConsumeExpect(TokenType.COLON);
        ConsumeExpect(TokenType.NEWLINE);

        // Rules
        while (Peek().type != TokenType.END)
        {
            definition.rules.Add(ParseExpressionLine());
        }
        ConsumeExpect(TokenType.END);
        ConsumeExpect(TokenType.NEWLINE);

        return definition;
    }
    private Theorem ParseTheorem()
    {
        Theorem theorem = new() { line = line };

        ConsumeExpect(TokenType.THEOREM);
        theorem.name = ConsumeExpect(TokenType.STRING).GetString();

        // Parameters
        ConsumeExpect(TokenType.BRACKET_OPEN);
        while (Peek().type == TokenType.STRING)
        {
            string name = ConsumeExpect(TokenType.STRING).GetString();
            theorem.parameters.Add(name);

            if (Peek().type != TokenType.BRACKET_CLOSE)
                ConsumeExpect(TokenType.COMMA);
        }
        ConsumeExpect(TokenType.BRACKET_CLOSE);
        ConsumeExpect(TokenType.COLON);
        ConsumeExpect(TokenType.NEWLINE);

        // Required Statements
        while (Peek().type != TokenType.IMPLIES)
            theorem.requirements.Add(ParseExpressionLine());

        // Hypothesis
        ConsumeExpect(TokenType.IMPLIES);
        theorem.hypothesis = ParseExpressionLine();

        // Proof
        theorem.proof = ParseScope();

        return theorem;
    }
    private Scope ParseScope()
    {
        Scope scope = new();
        ConsumeExpect(TokenType.CURLY_OPEN);
        ConsumeExpect(TokenType.NEWLINE);
        while (Peek().type != TokenType.CURLY_CLOSE)
            scope.statements.Add(ParseStatementLine());
        ConsumeExpect(TokenType.CURLY_CLOSE);
        ConsumeExpect(TokenType.NEWLINE);
        return scope;
    }
    private ExpressionLine ParseExpressionLine()
    {
        var exprLine = new ExpressionLine()
        {
            expr = ParseExpression(),
            line = line
        };
        ConsumeExpect(TokenType.NEWLINE);
        return exprLine;
    }
    private StatementLine ParseStatementLine()
    {
        if (Peek().type == TokenType.SORRY) // Parse sorry statement
        {
            Consume();
            ConsumeExpect(TokenType.NEWLINE);
            return new() { stmt = Command.SORRY, line = line };
        }
        else if (Peek().type == TokenType.IF) // Parse conditional statement
        {
            Consume();
            ConsumeExpect(TokenType.BRACKET_OPEN);
            var condStmt = new ConditionalStatement()
            {
                condition = new() { expr = ParseExpression(), line = line }
            };
            ConsumeExpect(TokenType.BRACKET_CLOSE);
            ConsumeExpect(TokenType.NEWLINE);
            condStmt.ifScope = ParseScope();

            ConsumeExpect(TokenType.ELSE);
            ConsumeExpect(TokenType.NEWLINE);
            condStmt.elseScope = ParseScope();

            ConsumeExpect(TokenType.BOTH);
            ConsumeExpect(TokenType.NEWLINE);
            condStmt.bothScope = ParseScope();

            return new() { stmt = condStmt, line = line };
        }

        StatementLine stmt;
        if (Peek().type == TokenType.LET) // Parse definition statement
        {
            Consume();
            var defStmt = new DefinitionStatement()
            {
                obj = ConsumeExpect(TokenType.STRING).GetString(),
            };
            ConsumeExpect(TokenType.COLON);
            defStmt.stmt = ParseExpression();
            stmt = new() { stmt = defStmt, line = line };
        }
        else if (Peek().type == TokenType.CHECK) // Parse check command
        {
            Consume();
            stmt = new() { stmt = Command.CHECK, line = line };
        }
        else // Parse expression statement
            stmt = new() { stmt = new(ParseExpression()), line = line };

        // Parse proof
        if (Peek().type == TokenType.PIPE)
        {
            Consume();
            if (Peek().type == TokenType.SORRY)
            {
                Consume();
                stmt.proof = Command.SORRY;
            }
            else if (Peek().type == TokenType.AT)
            {
                Consume();
                stmt.proof = ConsumeExpect(TokenType.STRING).GetString();
            }
            else
                stmt.proof = ParseFuncCall();
        }
        ConsumeExpect(TokenType.NEWLINE);
        return stmt;
    }
    private FuncCall ParseFuncCall()
    {
        FuncCall funcCall = new()
        {
            name = ConsumeExpect(TokenType.STRING).GetString()
        };
        ConsumeExpect(TokenType.BRACKET_OPEN);
        while (Peek().type != TokenType.BRACKET_CLOSE)
        {
            funcCall.args.Add(ParseExpression());
            if (Peek().type != TokenType.BRACKET_CLOSE)
                ConsumeExpect(TokenType.COMMA);
        }
        ConsumeExpect(TokenType.BRACKET_CLOSE);
        return funcCall;
    }
    private Expression ParseExpression(int minPrec = 0)
    {
        Expression lhs;
        if (Peek().type == TokenType.BRACKET_OPEN)
        {
            Consume();
            lhs = ParseExpression();
            ConsumeExpect(TokenType.BRACKET_CLOSE);
        }
        else
            lhs = new(ParseTerm());

        while (true)
        {
            BinExpr binExpr = new();
            int prec = Token.GetPrecedence(Peek().type);

            if (prec < minPrec)
                break;

            binExpr.lhs = lhs;
            binExpr.op = Consume();
            binExpr.rhs = ParseExpression(prec + 1);
            lhs = binExpr;
        }
        return lhs;
    }
    private Term ParseTerm()
    {
        switch (Peek().type)
        {
            case TokenType.FOR_ALL:
            case TokenType.EXISTS:
                QuantifiedStatement stmt = new() { op = Peek().type };
                Consume();
                stmt.obj = ConsumeExpect(TokenType.STRING).GetString();
                ConsumeExpect(TokenType.BRACKET_OPEN);
                stmt.stmt = ParseExpression();
                ConsumeExpect(TokenType.BRACKET_CLOSE);
                return new(stmt);
            case TokenType.STRING:
                string str = Consume().GetString();
                if (Peek().type == TokenType.BRACKET_OPEN)
                {
                    Consume();
                    FuncCall funcCall = new() { name = str };
                    while (Peek().type != TokenType.BRACKET_CLOSE)
                    {
                        funcCall.args.Add(ParseExpression());
                        if (Peek().type != TokenType.BRACKET_CLOSE)
                            ConsumeExpect(TokenType.COMMA);
                    }
                    ConsumeExpect(TokenType.BRACKET_CLOSE);
                    return new(funcCall);
                }
                else
                    return new(str);
            case TokenType.NOT:
                return new(new UnaryExpr()
                {
                    op = Consume(),
                    expr = ParseTerm()
                });
            case TokenType.BRACKET_OPEN:
                Consume();
                Term term = new(ParseExpression());
                ConsumeExpect(TokenType.BRACKET_CLOSE);
                return term;
            default:
                Logger.Error($"Invalid term \"{Peek()}\" in line {line}");
                throw new();
        }
    }
};