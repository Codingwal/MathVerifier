using TokenType = Token.TokenType;

public class Parser
{
    private List<List<Token>> tokens;
    private int line;
    private int index;

    public Parser(List<List<Token>> tokens)
    {
        this.tokens = tokens;
        line = 0;
        index = 0;
    }

    private Token Peek()
    {
        return tokens[line][index];
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
            Logger.Error($"Expected token of type \"{type}\" but found \"{consumed}\" in line {line + 1}");
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
                    Logger.Error($"Invalid token \"{Peek()}\" outside of theorem/definition in line {line + 1}");
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
            definition.rules.Add(ParseStatement());
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
            theorem.requirements.Add(ParseStatement());
        }

        // Hypothesis
        ConsumeExpect(TokenType.IMPLIES);
        theorem.hypothesis = ParseStatement();

        // Proof
        ConsumeExpect(TokenType.CURLY_OPEN);
        ConsumeExpect(TokenType.NEWLINE);
        while (Peek().type != TokenType.CURLY_CLOSE)
        {
            if (Peek().type == TokenType.SORRY)
            {
                Consume();
                theorem.proof.Add(new() { stmt = new(line, new Command(Command.CommandType.SORRY)) });
                ConsumeExpect(TokenType.NEWLINE);
            }
            else
                theorem.proof.Add(ParseProvenStatement());
        }
        ConsumeExpect(TokenType.CURLY_CLOSE);
        ConsumeExpect(TokenType.NEWLINE);

        return theorem;
    }
    private ProvenStatement ParseProvenStatement()
    {
        if (Peek().type == TokenType.CHECK)
        {
            Consume();
            ConsumeExpect(TokenType.NEWLINE);
            return new() { stmt = new(line - 1, new Command(Command.CommandType.CHECK)), };
        }
        else
        {
            ProvenStatement stmt = new()
            {
                stmt = new(line, ParseExpression())
            };
            if (Peek().type == TokenType.PIPE)
            {
                Consume();
                if (Peek().type == TokenType.SORRY)
                {
                    Consume();
                    stmt.theorem = new Command(Command.CommandType.SORRY);
                }
                else
                    stmt.theorem = ParseFuncCall();
            }
            else
                stmt.theorem = new None();
                
            ConsumeExpect(TokenType.NEWLINE);
            return stmt;
        }
    }
    private Statement ParseStatement()
    {
        Statement stmt;
        if (Peek().type == TokenType.CHECK)
        {
            Consume();
            stmt = new(line, new Command(Command.CommandType.CHECK));
        }
        else
            stmt = new(line, ParseExpression());
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
                ConsumeExpect(TokenType.SEMICOLON);
        }
        ConsumeExpect(TokenType.BRACKET_CLOSE);
        return funcCall;
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

            binExpr.op = Consume();
            binExpr.lhs = lhs;
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
            case TokenType.ALL:
            case TokenType.EXISTS:
                return new(new Expression(ParseQuantifiedExpr()));
            default:
                Logger.Error($"Invalid term \"{Peek()}\" in line {line + 1}");
                throw new();
        }
    }
    private QuantifiedExpr ParseQuantifiedExpr()
    {
        QuantifiedExpr expr = new()
        {
            type = Consume().type switch
            {
                TokenType.ALL => QuantifiedExpr.QuantifiedExprType.ALL,
                TokenType.EXISTS => QuantifiedExpr.QuantifiedExprType.EXISTS,
                _ => throw new()
            }
        };
        List<string> tmp = new();
        while (true)
        {
            string objName = ConsumeExpect(TokenType.STRING).GetString();
            expr.objects.Add(objName);
            tmp.Add(objName);

            if (Peek().type == TokenType.ELEMENT_OF)
            {
                Consume();
                Expression set = ParseExpression();
                foreach (string str in tmp)
                {
                    BinExpr e = new()
                    {
                        lhs = new(new Term(str)),
                        op = new(TokenType.ELEMENT_OF),
                        rhs = set,
                    };
                    expr.rules.Add(new(line, new Expression(e)));
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
        expr.stmt = new(line, ParseExpression());
        return expr;
    }
};