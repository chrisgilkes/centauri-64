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

        throw new InvalidOperationException(
            $"Unexpected token: {token.Type}");
    }

    private PrintStatement ParsePrintStatement()
    {
        Expect(TokenType.Print);

        var stringToken = Expect(TokenType.String);

        return new PrintStatement(stringToken.Text);
    }

    private Token Current()
    {
        return _tokens[_position];
    }

    private void Advance()
    {
        _position++;
    }
}