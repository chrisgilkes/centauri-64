namespace Centauri64.Basic.Syntax;

public sealed class TextAtStatement : Statement
{
    public Expression X { get; }
    public Expression Y { get; }
    public Expression Text { get; }

    public TextAtStatement(
        Expression x,
        Expression y,
        Expression text)
    {
        X = x;
        Y = y;
        Text = text;
    }
}