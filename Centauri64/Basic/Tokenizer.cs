using System;
using System.Collections.Generic;

namespace Centauri64.Basic;

public sealed class Tokenizer
{
    private string _source = string.Empty;
    private int _position;

    public List<Token> Tokenize(string source)
    {
        _source = source;
        _position = 0;

        var tokens = new List<Token>();

        while (!IsAtEnd())
        {
            var character = Current();

            if (char.IsWhiteSpace(character))
            {
                Advance();
                continue;
            }

            if (char.IsDigit(character))
            {
                tokens.Add(ReadNumber());
                continue;
            }

            if (char.IsLetter(character))
            {
                var token = ReadWord();

                tokens.Add(token);

                if (token.Type == TokenType.Rem)
                {
                    break;
                }

                continue;
            }

            if (character == '(')
            {
                tokens.Add(
                    new Token(TokenType.LeftParenthesis, "("));

                Advance();
                continue;
            }

            if (character == ')')
            {
                tokens.Add(
                    new Token(TokenType.RightParenthesis, ")"));

                Advance();
                continue;
            }

            if (character == '"')
            {
                tokens.Add(ReadString());
                continue;
            }

            if (character == '<')
            {
                if (Peek() == '=')
                {
                    tokens.Add(
                        new Token(TokenType.LessThanOrEqual, "<="));

                    Advance();
                    Advance();
                }
                else if (Peek() == '>')
                {
                    tokens.Add(
                        new Token(TokenType.NotEqual, "<>"));

                    Advance();
                    Advance();
                }
                else
                {
                    tokens.Add(
                        new Token(TokenType.LessThan, "<"));

                    Advance();
                }

                continue;
            }

            if (character == '>')
            {
                if (Peek() == '=')
                {
                    tokens.Add(
                        new Token(TokenType.GreaterThanOrEqual, ">="));

                    Advance();
                    Advance();
                }
                else
                {
                    tokens.Add(
                        new Token(TokenType.GreaterThan, ">"));

                    Advance();
                }

                continue;
            }

            if (character == '=')
            {
                tokens.Add(new Token(TokenType.Equals, "="));
                Advance();
                continue;
            }

            if (character == '+')
            {
                tokens.Add(new Token(TokenType.Plus, "+"));
                Advance();
                continue;
            }

            if (character == '-')
            {
                tokens.Add(new Token(TokenType.Minus, "-"));
                Advance();
                continue;
            }

            if (character == '*')
            {
                tokens.Add(new Token(TokenType.Multiply, "*"));
                Advance();
                continue;
            }

            if (character == '/')
            {
                tokens.Add(new Token(TokenType.Divide, "/"));
                Advance();
                continue;
            }

            if (character == ',')
            {
                tokens.Add(
                    new Token(TokenType.Comma, ","));

                Advance();
                continue;
            }

            throw new InvalidOperationException($"Unexpected character '{character}' at position {_position}.");
        }

        tokens.Add(new Token(TokenType.EndOfLine, string.Empty));

        return tokens;
    }

    private Token ReadRem()
    {
        while (!IsAtEnd() && char.IsWhiteSpace(Current()))
        {
            Advance();
        }

        var start = _position;

        while (!IsAtEnd())
        {
            Advance();
        }

        var text = _source[start.._position];

        return new Token(TokenType.Rem, text);
    }

    private Token ReadWord()
    {
        var start = _position;

        while (!IsAtEnd() && char.IsLetterOrDigit(Current()))
        {
            Advance();
        }

        var text = _source[start.._position];

        var type = text switch
        {
            "PRINT" => TokenType.Print,
            "GOTO"  => TokenType.Goto,
            "IF" => TokenType.If,
            "THEN" => TokenType.Then,
            "YIELD" => TokenType.Yield,
            "PRINTAT" => TokenType.PrintAt,
            "CLS" => TokenType.Cls,
            "INK" => TokenType.Ink,
            "PAPER" => TokenType.Paper,
            "BORDER" => TokenType.Border,
            "SPRITE" => TokenType.Sprite,
            "SPRITEPOS" => TokenType.SpritePosition,
            "RESET" => TokenType.Reset,
            "GOSUB" => TokenType.Gosub,
            "RETURN" => TokenType.Return,
            "BEEP" => TokenType.Beep,
            "WAIT" => TokenType.Wait,
            "MODE" => TokenType.Mode,
            "FOR" => TokenType.For,
            "TO" => TokenType.To,
            "STEP" => TokenType.Step,
            "NEXT" => TokenType.Next,
            "PLOT"   => TokenType.Plot,
            "LINE"   => TokenType.Line,
            "RECT"   => TokenType.Rect,
            "CIRCLE" => TokenType.Circle,
            "FILL" => TokenType.Fill,
            "SPRITESHOW" => TokenType.SpriteShow,
            "SPRITEHIDE" => TokenType.SpriteHide,
            "DIM" => TokenType.Dim,
            "REM" => TokenType.Rem,
            _ => TokenType.Identifier
        };

        return new Token(type, text);
    }

    private char Peek()
    {
        if (_position + 1 >= _source.Length)
            return '\0';

        return _source[_position + 1];
    }

    private Token ReadString()
    {
        Advance();

        var start = _position;

        while (!IsAtEnd() && Current() != '"')
        {
            Advance();
        }

        if (IsAtEnd())
        {
            throw new InvalidOperationException(
                "Unterminated string.");
        }

        var text = _source[start.._position];

        Advance();

        return new Token(TokenType.String, text);
    }

    private bool IsAtEnd()
    {
        return _position >= _source.Length;
    }

    private char Current()
    {
        return _source[_position];
    }

    private void Advance()
    {
        _position++;
    }

    private Token ReadNumber()
    {
        var start = _position;

        while (!IsAtEnd() && char.IsDigit(Current()))
        {
            Advance();
        }

        var text = _source[start.._position];

        return new Token(TokenType.Number, text);
    }
}