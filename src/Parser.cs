using MathVerifier.AST;
using MathVerifier.Tokens;

namespace MathVerifier.Services;

public class Parser(List<TokenLine> _tokens)
{
    private List<TokenLine> tokens = _tokens;
    private LineInfo lineInfo;
    private int lineIndex = 0;
    private int index = 0;

    private Token Peek()
    {
        return tokens[lineIndex].tokens[index];
    }
    private Token Consume()
    {
        Token token = Peek();
        lineInfo = tokens[lineIndex].lineInfo;

        index++;

        // If the end of the line has been reached, skip lines until the next token is found
        while (index >= tokens[lineIndex].tokens.Count)
        {
            index = 0;
            lineIndex++;
        }

        // If the token is of type NEWLINE, skip all additional NEWLINE tokens
        if (token.type == TokenType.NEWLINE)
        {
            while (Peek().type == TokenType.NEWLINE)
            {
                lineIndex++;
                index = 0;
            }
        }

        return token;
    }
    private Token ConsumeExpect(TokenType type)
    {
        Token consumed = Consume();
        if (consumed.type != type)
            Logger.Error($"Expected token of type \"{type}\" but found \"{consumed}\" ({lineInfo})");
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
                    Logger.Error($"Invalid token \"{Peek()}\" outside of theorem/definition ({lineInfo})");
                    break;
            }
        }
        return data;
    }
    private Definition ParseDefinition()
    {
        ConsumeExpect(TokenType.DEFINE);
        string name = ConsumeExpect(TokenType.STRING).GetString();

        // Parameters
        List<string> parameters = [];
        ConsumeExpect(TokenType.BRACKET_OPEN);
        while (Peek().type == TokenType.STRING)
        {
            string paramName = ConsumeExpect(TokenType.STRING).GetString();
            parameters.Add(paramName);

            if (Peek().type != TokenType.BRACKET_CLOSE)
                ConsumeExpect(TokenType.COMMA);
        }
        ConsumeExpect(TokenType.BRACKET_CLOSE);
        ConsumeExpect(TokenType.COLON);
        ConsumeExpect(TokenType.NEWLINE);

        ExpressionLine expressionLine = ParseExpressionLine();

        return new Definition(name, parameters, expressionLine.Expr, expressionLine.LineInfo);
    }
    private Theorem ParseTheorem()
    {
        ConsumeExpect(TokenType.THEOREM);
        string name = ConsumeExpect(TokenType.STRING).GetString();

        // Parameters
        List<string> parameters = [];
        ConsumeExpect(TokenType.BRACKET_OPEN);
        while (Peek().type == TokenType.STRING)
        {
            string paramName = ConsumeExpect(TokenType.STRING).GetString();
            parameters.Add(paramName);

            if (Peek().type != TokenType.BRACKET_CLOSE)
                ConsumeExpect(TokenType.COMMA);
        }
        ConsumeExpect(TokenType.BRACKET_CLOSE);
        ConsumeExpect(TokenType.COLON);
        ConsumeExpect(TokenType.NEWLINE);

        // Required Statements
        List<ExpressionLine> requirements = [];
        while (Peek().type != TokenType.IMPLIES)
            requirements.Add(ParseExpressionLine());

        // Hypothesis
        ConsumeExpect(TokenType.IMPLIES);
        ExpressionLine hypothesis = ParseExpressionLine();

        // Proof
        Scope proof = ParseScope();

        return new Theorem(name, parameters, requirements, hypothesis, proof, lineInfo);
    }
    private Scope ParseScope()
    {
        ConsumeExpect(TokenType.CURLY_OPEN);
        ConsumeExpect(TokenType.NEWLINE);

        List<StatementLine> statements = [];
        while (Peek().type != TokenType.CURLY_CLOSE)
            statements.Add(ParseStatementLine());

        ConsumeExpect(TokenType.CURLY_CLOSE);
        ConsumeExpect(TokenType.NEWLINE);
        return new Scope(statements);
    }
    private ExpressionLine ParseExpressionLine()
    {
        Expression expr = ParseExpression();
        ConsumeExpect(TokenType.NEWLINE);
        return new ExpressionLine(expr, lineInfo);
    }
    private StatementLine ParseStatementLine()
    {
        if (Peek().type == TokenType.SORRY) // Parse sorry statement
        {
            Consume();
            ConsumeExpect(TokenType.NEWLINE);
            return new StatementLine(new SorryStatement(), null, lineInfo);
        }
        else if (Peek().type == TokenType.IF) // Parse conditional statement
        {
            Consume();

            ConsumeExpect(TokenType.BRACKET_OPEN);
            ExpressionLine condition = new(ParseExpression(), lineInfo);
            ConsumeExpect(TokenType.BRACKET_CLOSE);
            ConsumeExpect(TokenType.NEWLINE);

            Scope ifScope = ParseScope();

            ConsumeExpect(TokenType.ELSE);
            ConsumeExpect(TokenType.NEWLINE);
            Scope elseScope = ParseScope();

            ConsumeExpect(TokenType.BOTH);
            ConsumeExpect(TokenType.NEWLINE);
            Scope bothScope = ParseScope();

            return new StatementLine(new ConditionalStatement(
                Condition: condition,
                If: ifScope,
                Else: elseScope,
                Both: bothScope
            ), null, lineInfo);
        }

        Statement stmt;
        if (Peek().type == TokenType.LET) // Parse definition statement
        {
            Consume();
            string obj = ConsumeExpect(TokenType.STRING).GetString();
            ConsumeExpect(TokenType.COLON);
            Expression expr = ParseExpression();
            stmt = new DefinitionStatement(obj, expr);
        }
        else // Parse expression statement
            stmt = ParseExpression();

        // Parse proof
        IProof? proof = null;
        if (Peek().type == TokenType.PIPE)
        {
            Consume();
            if (Peek().type == TokenType.SORRY)
            {
                Consume();
                proof = new SorryStatement();
            }
            else if (Peek().type == TokenType.AT)
            {
                Consume();
                proof = new DefinitionReference(ConsumeExpect(TokenType.STRING).GetString());
            }
            else
                proof = ParseFuncCall();
        }

        var stmtLine = new StatementLine(stmt, proof, lineInfo);
        ConsumeExpect(TokenType.NEWLINE);
        return stmtLine;
    }
    private FuncCall ParseFuncCall()
    {
        string name = ConsumeExpect(TokenType.STRING).GetString();

        List<Expression> args = [];
        ConsumeExpect(TokenType.BRACKET_OPEN);
        while (Peek().type != TokenType.BRACKET_CLOSE)
        {
            args.Add(ParseExpression());
            if (Peek().type != TokenType.BRACKET_CLOSE)
                ConsumeExpect(TokenType.COMMA);
        }
        ConsumeExpect(TokenType.BRACKET_CLOSE);

        return new FuncCall(name, args);
    }
    private Expression ParseExpression(int minPrec = 0)
    {
        Expression lhs = ParseTerm();

        while (true)
        {
            int prec = Token.GetPrecedence(Peek().type);

            if (prec < minPrec)
                break;

            Token op = Consume();
            Expression rhs = ParseExpression(prec + 1);
            lhs = new BinExpr(lhs, op, rhs);
        }
        return lhs;
    }
    private Expression ParseTerm()
    {
        Expression expr;
        List<Expression> elements;
        switch (Peek().type)
        {
            case TokenType.TRUE:
            case TokenType.FALSE:
                return new TruthValue(Consume().type == TokenType.TRUE);
            case TokenType.FOR_ALL:
            case TokenType.EXISTS:
                TokenType op = Consume().type;

                List<string> objs = [];
                while (true)
                {
                    objs.Add(ConsumeExpect(TokenType.STRING).GetString());
                    if (Peek().type != TokenType.COMMA) break;
                    ConsumeExpect(TokenType.COMMA);
                }

                ConsumeExpect(TokenType.BRACKET_OPEN);
                expr = ParseExpression();
                ConsumeExpect(TokenType.BRACKET_CLOSE);

                return new QuantifiedStatement(op, objs, expr);

            case TokenType.STRING:
                string str = Consume().GetString();
                if (Peek().type == TokenType.BRACKET_OPEN)
                {
                    Consume();

                    List<Expression> args = [];
                    while (Peek().type != TokenType.BRACKET_CLOSE)
                    {
                        args.Add(ParseExpression());
                        if (Peek().type != TokenType.BRACKET_CLOSE)
                            ConsumeExpect(TokenType.COMMA);
                    }
                    ConsumeExpect(TokenType.BRACKET_CLOSE);
                    return new FuncCall(str, args);
                }
                else
                    return new Variable(str);
            case TokenType.NOT:
                return new UnaryExpr(Op: Consume(), Expr: ParseTerm());
            case TokenType.BRACKET_OPEN:
                Consume();
                expr = ParseExpression();
                ConsumeExpect(TokenType.BRACKET_CLOSE);
                return expr;
            case TokenType.SQUARE_OPEN:
                Consume();
                elements = [];
                while (true)
                {
                    elements.Add(ParseExpression());
                    if (Peek().type == TokenType.SQUARE_CLOSE)
                        break;
                    ConsumeExpect(TokenType.COMMA);
                }
                ConsumeExpect(TokenType.SQUARE_CLOSE);
                return new AST.Tuple(elements);
            case TokenType.CURLY_OPEN:
                Consume();
                if (Peek().type == TokenType.CURLY_CLOSE)
                {
                    Consume();
                    return new SetEnumNotation([]);
                }

                Expression e = ParseExpression(); // Must be parsed first so that the next token can be looked at

                Token token = Consume();
                if (token.type == TokenType.COMMA)
                {
                    elements = [];
                    elements.Add(e);
                    while (Peek().type != TokenType.CURLY_CLOSE)
                    {
                        elements.Add(ParseExpression());
                        if (Peek().type != TokenType.CURLY_CLOSE)
                            ConsumeExpect(TokenType.COMMA);
                    }
                    ConsumeExpect(TokenType.CURLY_CLOSE);
                    return new SetEnumNotation(elements);
                }
                else if (token.type == TokenType.COLON)
                {
                    Logger.Assert(e is Variable, $"Expected variable instead of expression \"{Utility.Expr2Str(e)}\" ({lineInfo})");
                    SetBuilder set = new(((Variable)e).Str, ParseExpression());
                    ConsumeExpect(TokenType.CURLY_CLOSE);
                    return set;
                }
                else
                {
                    Logger.Error($"Expected \",\" or \":\" but found \"{token.type}\" ({lineInfo})");
                    throw new();
                }
            default:
                Logger.Error($"Invalid term \"{Peek()}\" ({lineInfo})");
                throw new();
        }
    }
};