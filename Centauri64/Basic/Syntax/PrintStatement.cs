namespace Centauri64.Basic.Syntax;

public sealed class PrintStatement : Statement
{
    public string Text { get; }

    public PrintStatement(string text)
    {
        Text = text;
    }
}