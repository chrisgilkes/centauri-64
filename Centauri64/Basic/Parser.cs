using System;
using System.Collections.Generic;
using Centauri64.Basic.Syntax;

namespace Centauri64.Basic;

public sealed class Parser
{
    private IReadOnlyList<Token> _tokens = [];
    private int _position;

    public ProgramLine ParseLine(IReadOnlyList<Token> tokens, string source)
    {
        _tokens     = tokens;
        _position   = 0;

        var lineNumber = ParseLineNumber();
        var statement  = ParseStatement();

        Expect(TokenType.EndOfLine);

        return new ProgramLine(lineNumber, statement, source);
    }

    private Token Expect(TokenType type)
    {
        var token = Current();

        if (token.Type != type)
        {
            throw new InvalidOperationException(
                $"Expected {type}, but found {token.Type}.");
        }

        Advance();

        return token;
    }

    private int ParseLineNumber()
    {
        var token = Expect(TokenType.Number);

        return int.Parse(token.Text);
    }

    private Statement ParseStatement()
    {
        var token = Current();

        if (token.Type == TokenType.Print)
        {
            return ParsePrintStatement();
        }

        if (token.Type == TokenType.Goto)
        {
            return ParseGotoStatement();    
        }

        if (token.Type == TokenType.If)
        {
            return ParseIfStatement();
        }

        if (token.Type == TokenType.Yield)
        {
            return ParseYieldStatement();
        }

        if (token.Type == TokenType.PrintAt)
        {
            return ParsePrintAtStatement();
        }

        if (token.Type == TokenType.Cls)
        {
            return ParseClsStatement();
        }

        if (token.Type == TokenType.Ink)
        {
            return ParseInkStatement();
        }

        if (token.Type == TokenType.Paper)
        {
            return ParsePaperStatement();
        }

        if (token.Type == TokenType.Border)
        {
            return ParseBorderStatement();
        }

        if (token.Type == TokenType.Sprite)
        {
            return ParseSpriteStatement();
        }

        if (token.Type == TokenType.SpritePosition)
        {
            return ParseSpritePositionStatement();
        }

        if (token.Type == TokenType.Reset)
        {
            return ParseResetStatement();
        }

        if (token.Type == TokenType.Gosub)
        {
            return ParseGosubStatement();
        }

        if (token.Type == TokenType.Return)
        {
            return ParseReturnStatement();
        }

        if (token.Type == TokenType.Beep)
        {
            return ParseBeepStatement();
        }

        if (token.Type == TokenType.Wait)
        {
            return ParseWaitStatement();
        }

        if (token.Type == TokenType.Mode)
        {
            return ParseModeStatement();
        }

        if (token.Type == TokenType.For)
        {
            return ParseForStatement();
        }

        if (token.Type == TokenType.Next)
        {
            return ParseNextStatement();
        }

        if (token.Type == TokenType.Identifier)
        {
            return ParseAssignmentStatement();
        }

        throw new InvalidOperationException(
            $"Unexpected token: {token.Type}");
    }

    private ForStatement ParseForStatement()
    {
        Expect(TokenType.For);

        var variable = Expect(TokenType.Identifier);

        Expect(TokenType.Equals);

        var start = ParseExpression();

        Expect(TokenType.To);

        var end = ParseExpression();

        Expression? step = null;

        if (Current().Type == TokenType.Step)
        {
            Advance();

            step = ParseExpression();
        }

        return new ForStatement(
            variable.Text,
            start,
            end,
            step);
    }

    private NextStatement ParseNextStatement()
    {
        Expect(TokenType.Next);

        var variable = Expect(TokenType.Identifier);

        return new NextStatement(variable.Text);
    }

    private Statement ParseModeStatement()
    {
        Expect(TokenType.Mode);

        var mode = ParseExpression();

        return new ModeStatement(mode);
    }

    private Statement ParseWaitStatement()
    {
        Expect(TokenType.Wait);

        var duration = ParseExpression();

        return new WaitStatement(duration);
    }

    private Statement ParseBeepStatement()
    {
        Expect(TokenType.Beep);

        var frequency = ParseExpression();

        Expect(TokenType.Comma);

        var duration = ParseExpression();

        return new BeepStatement(
            frequency,
            duration);
    }

    private Statement ParseGosubStatement()
    {
        Expect(TokenType.Gosub);

        var lineNumber =
            Expect(TokenType.Number);

        return new GosubStatement(
            int.Parse(lineNumber.Text));
    }

    private Statement ParseReturnStatement()
    {
        Expect(TokenType.Return);

        return new ReturnStatement();
    }

    private ResetStatement ParseResetStatement()
    {
        Expect(TokenType.Reset);

        return new ResetStatement();
    }

    private SpriteStatement ParseSpriteStatement()
    {
        Expect(TokenType.Sprite);

        var spriteIndex = ParseExpression();

        Expect(TokenType.Comma);

        var assetName = ParseExpression();

        return new SpriteStatement(
            spriteIndex,
            assetName);
    }

    private SpritePositionStatement ParseSpritePositionStatement()
    {
        Expect(TokenType.SpritePosition);

        var spriteIndex = ParseExpression();

        Expect(TokenType.Comma);

        var x = ParseExpression();

        Expect(TokenType.Comma);

        var y = ParseExpression();

        return new SpritePositionStatement(
            spriteIndex,
            x,
            y);
    }

    private BorderStatement ParseBorderStatement()
    {
        Expect(TokenType.Border);

        return new BorderStatement(
            ParseExpression());
    }

    private PaperStatement ParsePaperStatement()
    {
        Expect(TokenType.Paper);

        return new PaperStatement(
            ParseExpression());
    }

    private InkStatement ParseInkStatement()
    {
        Expect(TokenType.Ink);

        return new InkStatement(
            ParseExpression());
    }

    private ClsStatement ParseClsStatement()
    {
        Expect(TokenType.Cls);

        return new ClsStatement();
    }

    private PrintAtStatement ParsePrintAtStatement()
    {
        Expect(TokenType.PrintAt);

        var x = ParseExpression();

        Expect(TokenType.Comma);

        var y = ParseExpression();

        Expect(TokenType.Comma);

        var text = ParseExpression();

        return new PrintAtStatement(
            x,
            y,
            text);
    }

    private PrintStatement ParsePrintStatement()
    {
        Expect(TokenType.Print);

        var expression = ParseExpression();

        return new PrintStatement(expression);
    }

    private GotoStatement ParseGotoStatement()
    {
        Expect(TokenType.Goto);

        var lineNumberToken = Expect(TokenType.Number);

        return new GotoStatement(int.Parse(lineNumberToken.Text));
    }

    private YieldStatement ParseYieldStatement()
    {
        Expect(TokenType.Yield);

        return new YieldStatement();
    }

    private IfStatement ParseIfStatement()
    {
        Expect(TokenType.If);

        var condition = ParseExpression();

        Expect(TokenType.Then);

        var thenStatement = ParseStatement();

        return new IfStatement(
            condition,
            thenStatement);
    }

    private Token Current()
    {
        return _tokens[_position];
    }

    private void Advance()
    {
        _position++;
    }

    private Expression ParseMultiplicativeExpression()
    {
        var left = ParseUnaryExpression();

        while (Current().Type == TokenType.Multiply ||
            Current().Type == TokenType.Divide)
        {
            var operatorToken = Current();
            Advance();

            var right = ParseUnaryExpression();

            left = new BinaryExpression(
                left,
                operatorToken.Type,
                right);
        }

        return left;
    }

    private Expression ParseUnaryExpression()
    {
        if (Current().Type == TokenType.Minus)
        {
            Advance();

            return new UnaryExpression(
                TokenType.Minus,
                ParseUnaryExpression());
        }

        return ParsePrimaryExpression();
    }

    private Expression ParseAdditiveExpression()
    {
        var left = ParseMultiplicativeExpression();

        while (Current().Type == TokenType.Plus ||
            Current().Type == TokenType.Minus)
        {
            var operatorToken = Current();
            Advance();

            var right = ParseMultiplicativeExpression();

            left = new BinaryExpression(
                left,
                operatorToken.Type,
                right);
        }

        return left;
    }

    private Expression ParseExpression()
    {
        return ParseComparisonExpression();
    }

    private Expression ParseComparisonExpression()
    {
        var left = ParseAdditiveExpression();

        var tokenType = Current().Type;

        if (IsComparisonOperator(tokenType))
        {
            var operatorToken = Current();
            Advance();

            var right = ParseAdditiveExpression();

            return new BinaryExpression(
                left,
                operatorToken.Type,
                right);
        }

        return left;
    }

    private static bool IsComparisonOperator(TokenType type)
    {
        return type == TokenType.Equals ||
            type == TokenType.NotEqual ||
            type == TokenType.LessThan ||
            type == TokenType.GreaterThan ||
            type == TokenType.LessThanOrEqual ||
            type == TokenType.GreaterThanOrEqual;
    }

    private Expression ParsePrimaryExpression()
    {
        var token = Current();

        if (token.Type == TokenType.Number)
        {
            Advance();

            return new NumberExpression(
                int.Parse(token.Text));
        }

        if (token.Type == TokenType.String)
        {
            Advance();

            return new StringExpression(token.Text);
        }

        if (token.Type == TokenType.Identifier)
        {
            if (Peek().Type == TokenType.LeftParenthesis)
            {
                return ParseFunctionCallExpression();
            }

            Advance();

            return new VariableExpression(token.Text);
        }

        if (token.Type == TokenType.LeftParenthesis)
        {
            Advance();

            var expression = ParseExpression();

            Expect(TokenType.RightParenthesis);

            return expression;
        }

        throw new InvalidOperationException(
            $"Expected expression, but found {token.Type}.");
    }

    private FunctionCallExpression ParseFunctionCallExpression()
    {
        var name = Expect(TokenType.Identifier);

        Expect(TokenType.LeftParenthesis);

        var arguments = new List<Expression>();

        if (Current().Type != TokenType.RightParenthesis)
        {
            while (true)
            {
                arguments.Add(ParseExpression());

                if (Current().Type != TokenType.Comma)
                    break;

                Advance();
            }
        }

        Expect(TokenType.RightParenthesis);

        return new FunctionCallExpression(
            name.Text,
            arguments);
    }

    private AssignmentStatement ParseAssignmentStatement()
    {
        var identifier =
            Expect(TokenType.Identifier);

        Expect(TokenType.Equals);

        var value = ParseExpression();

        return new AssignmentStatement(
            identifier.Text,
            value);
    }

    private Token Peek()
    {
        var nextPosition = _position + 1;

        if (nextPosition >= _tokens.Count)
        {
            return _tokens[^1];
        }

        return _tokens[nextPosition];
    }
}