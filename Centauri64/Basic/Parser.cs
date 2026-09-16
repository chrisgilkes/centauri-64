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

        if (token.Type == TokenType.Identifier)
        {
            return ParseAssignmentStatement();
        }

        throw new InvalidOperationException(
            $"Unexpected token: {token.Type}");
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
        var left = ParsePrimaryExpression();

        while (Current().Type == TokenType.Multiply ||
            Current().Type == TokenType.Divide)
        {
            var operatorToken = Current();
            Advance();

            var right = ParsePrimaryExpression();

            left = new BinaryExpression(
                left,
                operatorToken.Type,
                right);
        }

        return left;
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
}