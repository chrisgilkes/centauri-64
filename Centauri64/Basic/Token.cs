namespace Centauri64.Basic;

public sealed class Token
{
    public TokenType Type { get; }
    public string Text { get; }

    public Token(TokenType type, string text)
    {
        Type = type;
        Text = text;
    }

    public override string ToString()
    {
        return $"{Type}: {Text}";
    }
}