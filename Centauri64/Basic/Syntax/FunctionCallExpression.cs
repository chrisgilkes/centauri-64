using System.Collections.Generic;
namespace Centauri64.Basic.Syntax;

public sealed class FunctionCallExpression : Expression
{
    public string Name { get; }
    public IReadOnlyList<Expression> Arguments { get; }

    public FunctionCallExpression(
        string name,
        IReadOnlyList<Expression> arguments)
    {
        Name = name;
        Arguments = arguments;
    }
}