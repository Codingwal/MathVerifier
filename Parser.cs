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
            Logger.Error($"Expected token of type \"{type}\"");
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
                    Logger.Error($"Invalid token \"{Peek()}\"");
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
            theorem.proof.Add(ParseStatement());
        }
        ConsumeExpect(TokenType.CURLY_CLOSE);
        ConsumeExpect(TokenType.NEWLINE);

        return theorem;
    }

    private Statement ParseStatement()
    {
        while (Peek().type != TokenType.NEWLINE)
            Consume();

        ConsumeExpect(TokenType.NEWLINE);

        return new Statement();
    }
};