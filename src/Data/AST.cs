// Miscellaneous
public enum Command
{
    NONE,
    CHECK,
    SORRY,
}

// Expressions
public interface IExpression { }
public interface IObjectCtor : IExpression { }

public struct BinExpr : IExpression
{
    public IExpression lhs;
    public Token op;
    public IExpression rhs;
}
public struct UnaryExpr : IExpression
{
    public Token op;
    public IExpression expr;
}
public struct FuncCall() : IExpression
{
    public string name = "";
    public List<IExpression> args = [];
}
public struct QuantifiedStatement : IExpression
{
    public TokenType op;
    public string obj;
    public IExpression stmt;
}
public struct Tuple() : IObjectCtor
{
    public List<IExpression> elements = [];
}
public struct SetEnumNotation() : IObjectCtor
{
    public List<IExpression> elements = [];
}
public struct SetBuilder : IObjectCtor
{
    public string obj;
    public IExpression requirement;
}

public struct DefinitionStatement
{
    public string obj;
    public IExpression stmt;
}
public struct Variable(string _str) : IExpression
{
    public string str = _str;
}

// High-level

public struct ExpressionLine
{
    public IExpression expr;
    public LineInfo lineInfo;
}
public struct Scope()
{
    public List<StatementLine> statements = [];
}
public struct ConditionalStatement
{
    public ExpressionLine condition;
    public Scope ifScope;
    public Scope elseScope;
    public Scope bothScope;
}
public struct StatementLine
{
    public LineInfo lineInfo;
    public Variant<IExpression, Command, DefinitionStatement, ConditionalStatement> stmt;
    public Variant<FuncCall, string, Command>? proof; // <theorem ref, definition ref, "sorry">
}

public struct Theorem()
{
    public string name = "";
    public List<string> parameters = [];
    public List<ExpressionLine> requirements = [];
    public ExpressionLine hypothesis = new();
    public Scope proof = new();
    public LineInfo lineInfo = default;
}

public struct Definition()
{
    public string name = "";
    public List<ExpressionLine> rules = [];
    public LineInfo lineInfo = default;
    public Scope proof = new(); // Proof that such an object exists
}

public struct Data()
{
    public List<Variant<Theorem, Definition>> data = [];
}
