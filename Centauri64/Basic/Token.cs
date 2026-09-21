namespace Centauri64.Basic;

public sealed class Token
{
    public TokenType Type { get; }

    public string Text { get; }

    public int Start { get; }

    public int Length { get; }

    public Token(
        TokenType type,
        string text,
        int start,
        int length)
    {
        Type = type;
        Text = text;
        Start = start;
        Length = length;
    }

    public override string ToString()
    {
        return $"{Type}: {Text} [{Start},{Length}]";
    }
}