using TokenType = Token.TokenType;

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
        // If the token is a newline, skip all following new lines
        if (Peek().type == TokenType.NEWLINE)
        {
            while (Peek().type == TokenType.NEWLINE)
            {
                line++;
                index = 0;
            }
            return new Token(TokenType.NEWLINE);
        }

        Token token = Peek();
        index++;
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
        Definition definition = new();

        ConsumeExpect(TokenType.DEFINE);
        definition.name = ConsumeExpect(TokenType.STRING).GetString();

        // Object
        ConsumeExpect(TokenType.BRACKET_OPEN);
        definition.obj = ConsumeExpect(TokenType.STRING).GetString();
        ConsumeExpect(TokenType.BRACKET_CLOSE);

        // Parameters
        ConsumeExpect(TokenType.BRACKET_OPEN);
        while (Peek().type == TokenType.STRING)
        {
            string name = ConsumeExpect(TokenType.STRING).GetString();
            definition.parameters.Add(name);

            if (Peek().type == TokenType.SEMICOLON)
                Consume();
        }
        ConsumeExpect(TokenType.BRACKET_CLOSE);
        ConsumeExpect(TokenType.COLON);
        ConsumeExpect(TokenType.NEWLINE);

        // Rules
        while (Peek().type != TokenType.END)
        {
            definition.rules.Add(ParseStatementLine());
        }
        ConsumeExpect(TokenType.END);
        ConsumeExpect(TokenType.NEWLINE);

        return definition;
    }
    private Theorem ParseTheorem()
    {
        Theorem theorem = new();

        ConsumeExpect(TokenType.THEOREM);
        theorem.name = ConsumeExpect(TokenType.STRING).GetString();

        // Parameters
        ConsumeExpect(TokenType.BRACKET_OPEN);
        while (Peek().type == TokenType.STRING)
        {
            string name = ConsumeExpect(TokenType.STRING).GetString();
            theorem.parameters.Add(name);

            if (Peek().type == TokenType.SEMICOLON)
                Consume();
        }
        ConsumeExpect(TokenType.BRACKET_CLOSE);
        ConsumeExpect(TokenType.COLON);
        ConsumeExpect(TokenType.NEWLINE);

        // Required Statements
        while (Peek().type != TokenType.IMPLIES)
        {
            theorem.requirements.Add(ParseStatementLine());
        }

        // Hypothesis
        ConsumeExpect(TokenType.IMPLIES);
        theorem.hypothesis = ParseStatementLine();

        // Proof
        ConsumeExpect(TokenType.CURLY_OPEN);
        ConsumeExpect(TokenType.NEWLINE);
        while (Peek().type != TokenType.CURLY_CLOSE)
        {
            if (Peek().type == TokenType.SORRY)
            {
                Consume();
                theorem.proof.Add(new() { stmt = Command.SORRY, line = line });
                ConsumeExpect(TokenType.NEWLINE);
            }
            else
                theorem.proof.Add(ParseStatementLine());
        }
        ConsumeExpect(TokenType.CURLY_CLOSE);
        ConsumeExpect(TokenType.NEWLINE);

        return theorem;
    }
    private StatementLine ParseStatementLine()
    {
        if (Peek().type == TokenType.CHECK)
        {
            Consume();
            ConsumeExpect(TokenType.NEWLINE);
            return new() { stmt = Command.CHECK, line = line };
        }
        else
        {
            StatementLine stmt = new()
            {
                stmt = new(ParseStatement()),
                line = line,
                proof = null,
            };
            if (Peek().type == TokenType.PIPE)
            {
                Consume();
                if (Peek().type == TokenType.SORRY)
                {
                    Consume();
                    stmt.proof = Command.SORRY;
                }
                else
                    stmt.proof = ParseFuncCall();
            }
            ConsumeExpect(TokenType.NEWLINE);
            return stmt;
        }
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
                ConsumeExpect(TokenType.SEMICOLON);
        }
        ConsumeExpect(TokenType.BRACKET_CLOSE);
        return funcCall;
    }
    private Statement ParseStatement(int minPrec = 0)
    {
        Variant<Expression, Statement> lhs;

        if (Peek().type == TokenType.FOR_ALL || Peek().type == TokenType.EXISTS)
        {
            lhs = (Statement)ParseQuantifiedStatement();
        }
        else if (Peek().type == TokenType.BRACKET_OPEN)
        {
            Consume();
            lhs = ParseStatement();
            ConsumeExpect(TokenType.BRACKET_CLOSE);
        }
        else if (Peek().type == TokenType.STRING &&
                 Peek().GetString().Length == 1 && char.IsUpper(Peek().GetString()[0]))
        {
            lhs = (Statement)Consume().GetString();
        }
        else
        {
            lhs = ParseExpression(Token.ExpressionMinPrec);
        }

        while (true)
        {
            int prec = Token.GetPrecedence(Peek().type);

            if (prec < minPrec)
                break;

            Statement stmt;
            switch (Peek().type)
            {
                case TokenType.ELEMENT_OF:
                case TokenType.SUBSET:
                    Logger.Assert(lhs.Is<Expression>(), $"Can't apply set statement to statement in line {line}");
                    stmt = new SetStatement()
                    {
                        lhs = lhs.As<Expression>(),
                        op = Consume().type,
                        rhs = ParseExpression(Token.ExpressionMinPrec)
                    };
                    break;
                case TokenType.EQUALS:
                    Logger.Assert(lhs.Is<Expression>(), $"Can't apply relational operator to statement in line {line}");
                    stmt = new RelationalOperator()
                    {
                        lhs = lhs.As<Expression>(),
                        op = Consume().type,
                        rhs = ParseExpression(Token.ExpressionMinPrec),
                    };
                    break;
                case TokenType.IMPLIES:
                    Logger.Assert(lhs.Is<Statement>(), $"Can't apply logical operator to expression in line {line}");
                    stmt = new LogicalOperator()
                    {
                        lhs = lhs.As<Statement>(),
                        op = Consume().type,
                        rhs = ParseStatement(prec + 1)
                    };
                    break;
                default:
                    Logger.Error($"Invalid token {Peek()} in line {line}");
                    throw new();
            }

            lhs = stmt;
        }
        Logger.Assert(lhs.Is<Statement>(), $"Expected statement but only found expression in line {line}");
        return lhs.As<Statement>();
    }
    private QuantifiedStatement ParseQuantifiedStatement()
    {
        QuantifiedStatement stmt = new() { op = Consume().type };
        Statement rules = new();
        List<string> tmp = new();
        while (true)
        {
            string objName = ConsumeExpect(TokenType.STRING).GetString();
            stmt.objects.Add(objName);
            tmp.Add(objName);

            if (Peek().type == TokenType.ELEMENT_OF)
            {
                Consume();
                Expression set = ParseExpression();

                foreach (string str in tmp)
                {
                    SetStatement setStmt = new()
                    {
                        lhs = new Expression(new Term(str)),
                        op = TokenType.ELEMENT_OF,
                        rhs = set,
                    };
                    if (rules.HasValue())
                        rules = new LogicalOperator()
                        {
                            lhs = setStmt,
                            op = TokenType.AND,
                            rhs = rules
                        };
                    else
                        rules = setStmt;
                }
                tmp.Clear();
            }
            if (Peek().type == TokenType.COLON)
            {
                Consume();
                break;
            }
            ConsumeExpect(TokenType.COMMA);
        }
        if (rules.HasValue())
            stmt.stmt = new LogicalOperator()
            {
                lhs = rules,
                op = TokenType.IMPLIES,
                rhs = ParseStatement()
            };
        else
            stmt.stmt = ParseStatement();

        return stmt;
    }

    private Expression ParseExpression(int minPrec = 0)
    {
        Expression lhs = new(ParseTerm());

        while (true)
        {
            BinExpr binExpr = new();
            int prec = Token.GetPrecedence(Peek().type);

            if (prec < minPrec)
                break;

            binExpr.lhs = lhs;
            binExpr.op = Consume();
            binExpr.rhs = ParseExpression(prec + 1);
            lhs.expr = binExpr;
        }
        return lhs;
    }
    private Term ParseTerm()
    {
        switch (Peek().type)
        {
            case TokenType.BRACKET_OPEN:
                Consume();
                Term term = new(ParseExpression());
                ConsumeExpect(TokenType.BRACKET_CLOSE);
                return term;
            case TokenType.NUMBER:
                return new(Consume().GetDouble());
            case TokenType.STRING:
                return new(Consume().GetString());
            default:
                Logger.Error($"Invalid term \"{Peek()}\" in line {line}");
                throw new();
        }
    }
};