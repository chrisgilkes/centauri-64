namespace Centauri64.Basic.Syntax;

public sealed class MapStatement : Statement
{
    public string ArrayName { get; }
    public Expression Columns { get; }
    public Expression Rows { get; }

    public MapStatement(
        string arrayName,
        Expression columns,
        Expression rows)
    {
        ArrayName = arrayName;
        Columns = columns;
        Rows = rows;
    }
}
