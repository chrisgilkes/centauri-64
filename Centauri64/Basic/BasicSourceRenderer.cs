using System;
using System.Collections.Generic;
using Centauri64.Console;

namespace Centauri64.Basic;

public sealed class BasicSourceRenderer
{
    private readonly Tokenizer _tokenizer;
    private readonly BasicEditorTheme _theme;

    public BasicSourceRenderer(Tokenizer tokenizer,BasicEditorTheme theme)
    {
        _tokenizer = tokenizer;
        _theme = theme;
    }

    public void WriteLine(TextConsole console, string source)
    {
        var tokens = _tokenizer.Tokenize(source, allowIncomplete: true);

        var position = 0;

        foreach (var token in tokens)
        {
            if (token.Type == TokenType.EndOfLine)
                break;

            // Write whitespace/gaps between tokens normally.
            if (token.Start > position)
            {
                console.Write(
                    source[position..token.Start],
                    _theme.TextColour);
            }

            // Write the exact source characters belonging to this token.
            console.Write(
                source.Substring(
                    token.Start,
                    token.Length),
                GetTokenColour(token.Type));

            position = token.Start + token.Length;
        }

        // Anything not covered by tokens.
        if (position < source.Length)
        {
            console.Write(
                source[position..],
                _theme.TextColour);
        }

        console.WriteLine("");
    }

    public void ColourExistingLine(TextConsole console,string source,int row,int startColumn)
    {
        // First reset the line to normal text colour.
        for (var i = 0; i < source.Length; i++)
        {
            console.SetCellForeground(
                startColumn + i,
                row,
                _theme.TextColour);
        }

        var tokens = _tokenizer.Tokenize(source,allowIncomplete: true);

        foreach (var token in tokens)
        {
            if (token.Type == TokenType.EndOfLine)
                break;

            var colour = GetTokenColour(token.Type);

            for (var i = 0; i < token.Length; i++)
            {
                console.SetCellForeground(
                    startColumn + token.Start + i,
                    row,
                    colour);
            }
        }
    }

    private int GetTokenColour(TokenType type)
    {
        if (type == TokenType.String)
            return _theme.StringColour;

        if (type == TokenType.Rem)
            return _theme.CommentColour;

        if (IsKeyword(type))
            return _theme.KeywordColour;

        return _theme.TextColour;
    }

    private static bool IsKeyword(TokenType type)
    {
        return type is
            TokenType.Print or
            TokenType.Goto or
            TokenType.If or
            TokenType.Then or
            TokenType.Yield or
            TokenType.PrintAt or
            TokenType.Cls or
            TokenType.Ink or
            TokenType.Paper or
            TokenType.Border or
            TokenType.Sprite or
            TokenType.SpritePosition or
            TokenType.SpriteShow or
            TokenType.SpriteHide or
            TokenType.Reset or
            TokenType.Gosub or
            TokenType.Return or
            TokenType.Beep or
            TokenType.Wait or
            TokenType.Mode or
            TokenType.For or
            TokenType.To or
            TokenType.Step or
            TokenType.Next or
            TokenType.Plot or
            TokenType.Line or
            TokenType.Rect or
            TokenType.Circle or
            TokenType.Fill or
            TokenType.Dim or
            TokenType.Rem or 
            TokenType.End;
    }
}