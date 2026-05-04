using MathVerifier.Tokens;

namespace MathVerifier.AST;

// Expressions
public abstract record Expression : Statement;
public abstract record ObjectCtor : Expression;

public record BinExpr(Expression Lhs, Token Op, Expression Rhs) : Expression;
public record UnaryExpr(Token Op, Expression Expr) : Expression;
public record FuncCall(string Name, List<Expression> Args) : Expression, IProof;
public record QuantifiedStatement(TokenType Op, string Obj, Expression Stmt) : Expression;
public record Tuple(List<Expression> Elements) : ObjectCtor;
public record SetEnumNotation(List<Expression> Elements) : ObjectCtor;
public record SetBuilder(string Obj, Expression Requirement) : ObjectCtor;
public record Variable(string Str) : Expression;


// High-level
public abstract record Statement;
public record ExpressionLine(Expression Expr, LineInfo LineInfo);
public record Scope(List<StatementLine> Statements);
public record DefinitionStatement(string Obj, Expression Stmt) : Statement;
public record ConditionalStatement(ExpressionLine Condition, Scope If, Scope Else, Scope Both) : Statement;

public interface IProof;
public record DefinitionReference(string Name) : IProof;
public record SorryStatement : Statement, IProof;

public record StatementLine(Statement Stmt, IProof? Proof, LineInfo LineInfo);

public record Theorem(string Name, List<string> Params, List<ExpressionLine> Requirements, ExpressionLine Hypothesis, Scope Proof, LineInfo LineInfo);

public record Definition(string Name, List<ExpressionLine> Rules, Scope Proof, LineInfo LineInfo);

public record Data()
{
    public List<Variant<Theorem, Definition>> data = [];
}
