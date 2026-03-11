public struct LineInfo(int _line, string _fileName)
{
    public int line = _line;
    public string fileName = _fileName;
    public static LineInfo Invalid => new(-1, "Invalid!");
    public readonly bool IsInvalid => line == -1;
    public override string ToString()
    {
        if (IsInvalid)
            return $"[Invalid line info]";

        return $"line {line}, file {fileName}";
    }
}

public struct TokenLine(List<Token> _tokens, LineInfo _lineInfo)
{
    public List<Token> tokens = _tokens;
    public LineInfo lineInfo = _lineInfo;
}