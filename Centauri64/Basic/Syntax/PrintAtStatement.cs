namespace Centauri64.Basic.Syntax;

public sealed class PrintAtStatement : Statement
{
    public Expression X { get; }
    public Expression Y { get; }
    public Expression Text { get; }

    public PrintAtStatement(
        Expression x,
        Expression y,
        Expression text)
    {
        X = x;
        Y = y;
        Text = text;
    }
}