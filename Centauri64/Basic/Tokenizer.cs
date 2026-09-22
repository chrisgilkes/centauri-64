using System;
using System.Collections.Generic;

namespace Centauri64.Basic;

public sealed class Tokenizer
{
    private string _source = string.Empty;
    private int _position;

    private bool _allowIncomplete;

    public List<Token> Tokenize(string source, bool allowIncomplete = false)
    {
        _source = source;
        _position = 0;
        _allowIncomplete = allowIncomplete;

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

                if (token.Type == TokenType.Rem)
                {
                    tokens.Add(ReadRem(token.Start));
                    break;
                }

                tokens.Add(token);

                continue;
            }

            if (character == '(')
            {
                tokens.Add(new Token(TokenType.LeftParenthesis,"(",_position,1));
                Advance();
                continue;
            }

            if (character == ')')
            {
                tokens.Add(new Token(TokenType.RightParenthesis, ")", _position, 1));
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
                    tokens.Add(new Token(TokenType.LessThanOrEqual,"<=",_position,2));

                    Advance();
                    Advance();
                }
                else if (Peek() == '>')
                {
                    tokens.Add(new Token(TokenType.NotEqual,"<>",_position,2));

                    Advance();
                    Advance();
                }
                else
                {
                    tokens.Add(new Token(TokenType.LessThan,"<",_position,1));

                    Advance();
                }

                continue;
            }

            if (character == '>')
            {
                if (Peek() == '=')
                {
                    tokens.Add(
                        new Token(
                            TokenType.GreaterThanOrEqual,
                            ">=",
                            _position,
                            2));

                    Advance();
                    Advance();
                }
                else
                {
                    tokens.Add(
                        new Token(
                            TokenType.GreaterThan,
                            ">",
                            _position,
                            1));

                    Advance();
                }

                continue;
            }

            if (character == '=')
            {
                tokens.Add(new Token(TokenType.Equals, "=", _position, 1));
                Advance();
                continue;
            }

            if (character == '+')
            {
                tokens.Add(new Token(TokenType.Plus, "+", _position, 1));
                Advance();
                continue;
            }

            if (character == '-')
            {
                tokens.Add(new Token(TokenType.Minus, "-", _position, 1));
                Advance();
                continue;
            }

            if (character == '*')
            {
                tokens.Add(new Token(TokenType.Multiply, "*", _position, 1));
                Advance();
                continue;
            }

            if (character == '/')
            {
                tokens.Add(new Token(TokenType.Divide, "/", _position, 1));
                Advance();
                continue;
            }

            if (character == ',')
            {
                tokens.Add(new Token(TokenType.Comma, ",", _position, 1));

                Advance();
                continue;
            }

            if (_allowIncomplete)
            {
                Advance();
                continue;
            }

            throw new InvalidOperationException($"Unexpected character '{character}' at position {_position}.");
        }

        tokens.Add(new Token(TokenType.EndOfLine,string.Empty,_position,0));

        return tokens;
    }

    private Token ReadRem(int start)
    {
        while (!IsAtEnd())
        {
            Advance();
        }

        var text = _source[start.._position];

        return new Token(TokenType.Rem,text,start,_position - start);
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
            "END" => TokenType.End,
            _ => TokenType.Identifier
        };

        return new Token(type,text,start,_position - start);
    }

    private char Peek()
    {
        if (_position + 1 >= _source.Length)
            return '\0';

        return _source[_position + 1];
    }

    private Token ReadString()
    {
        var tokenStart = _position;

        // Skip opening quote.
        Advance();

        var textStart = _position;

        while (!IsAtEnd() && Current() != '"')
        {
            Advance();
        }

        if (IsAtEnd())
        {
            if (_allowIncomplete)
            {
                var incompleteText = _source[textStart.._position];

                return new Token(
                    TokenType.String,
                    incompleteText,
                    tokenStart,
                    _position - tokenStart);
            }

            throw new InvalidOperationException("Unterminated string.");
        }

        var text = _source[textStart.._position];

        // Skip closing quote.
        Advance();

        return new Token(
            TokenType.String,
            text,
            tokenStart,
            _position - tokenStart);
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

        return new Token(TokenType.Number,text,start,_position - start);
    }
}