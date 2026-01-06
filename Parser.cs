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
        ConsumeExpect(TokenType.DEFINE);
        Definition definition = new()
        {
            name = ConsumeExpect(TokenType.STRING).GetString()
        };
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

            if (Peek().type != TokenType.BRACKET_CLOSE)
                ConsumeExpect(TokenType.COMMA);
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
                stmt = new(ParseExpression()),
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