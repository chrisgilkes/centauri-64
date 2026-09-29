using System;
using System.Collections.Generic;

using Centauri64.Basic;
using Centauri64.Graphics;
using Centauri64.Machine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Centauri64.Game;

public sealed partial class SoftwareShelf
{
    private enum InlayField
    {
        Description,
        Author,
        Kind
    }

    private const int Columns = 2;
    private const int Rows = 3;
    private const int PageSize = Columns * Rows;
    private const int CassetteWidth = 272;
    private const int CassetteHeight = 88;
    private const int OriginX = 32;
    private const int OriginY = 104;
    private const int GapX = 32;
    private const int GapY = 16;
    private const int CharacterWidth = 8;
    private const int ScrollGap = 3;
    private const double ScrollHold = 1.0;
    private const double ScrollInterval = 0.12;
    private const double RepeatDelay = 0.35;
    private const double RepeatInterval = 0.05;

    private static readonly char[] ShiftedNumbers =
    {
        ')', '!', '"', '#', '$',
        '%', '^', '&', '*', '('
    };

    private readonly BitmapFont _font;
    private readonly Texture2D _whitePixel;
    private readonly BasicMachine _machine;

    private static readonly Color Background = new(22, 55, 72);
    private static readonly Color Header = new(36, 72, 110);
    private static readonly Color Cyan = new(91, 214, 205);
    private static readonly Color Cream = new(238, 232, 190);
    private static readonly Color Yellow = new(232, 205, 92);
    private static readonly Color Muted = new(130, 165, 170);
    private static readonly Color Dark = new(14, 28, 38);
    private static readonly Color Shell = new(36, 44, 52);

    private KeyboardState _previousKeyboard;
    private List<string> _tapes = new();
    private List<TapeLabel> _labels = new();
    private List<TapeCover> _covers = new();
    private int _page;
    private int _selected;
    private bool _editing;
    private InlayField _field;
    private TapeLabel? _label;
    private Keys? _repeatKey;
    private double _repeatTimer;
    private double _cursorTimer;
    private int _descriptionScroll;
    private double _descriptionScrollTimer;
    private bool _descriptionHolding = true;

    public event Action? ExitSelected;

    public SoftwareShelf(
        BitmapFont font,
        Texture2D whitePixel,
        BasicMachine machine)
    {
        _font = font;
        _whitePixel = whitePixel;
        _machine = machine;
    }

    public void Show(IReadOnlyList<string> tapeNames)
    {
        _tapes = new List<string>(tapeNames);
        _labels = new List<TapeLabel>(_tapes.Count);
        _covers = new List<TapeCover>(_tapes.Count);

        foreach (var name in _tapes)
        {
            _labels.Add(_machine.GetTapeLabel(name));
            _covers.Add(_machine.GetTapeCover(name));
        }

        _page = 0;
        _selected = 0;
        _editing = false;
        _paintingCover = false;
        _label = null;
        _cover = null;
        ResetDescriptionScroll();
    }

    public void Update(GameTime gameTime, MouseState mouse)
    {
        var keyboard = Keyboard.GetState();
        var delta = gameTime.ElapsedGameTime.TotalSeconds;
        _cursorTimer += delta;

        if (_paintingCover)
        {
            UpdateCover(keyboard, mouse);
        }
        else if (_editing)
        {
            UpdateInlay(keyboard, delta);
        }
        else
        {
            UpdateShelf(keyboard);
            AdvanceDescriptionScroll(delta);
        }

        _previousKeyboard = keyboard;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(
            samplerState: SamplerState.PointClamp);

        DrawBox(
            spriteBatch,
            new Rectangle(
                0,
                0,
                CentauriMachine.DEVELOPMENT_WIDTH,
                CentauriMachine.DEVELOPMENT_HEIGHT),
            Background);

        DrawBox(
            spriteBatch,
            new Rectangle(16, 16, 608, 48),
            Header);

        if (_paintingCover && _cover != null)
        {
            DrawCoverEditor(spriteBatch);
        }
        else if (_editing && _label != null)
        {
            DrawInlay(spriteBatch);
        }
        else
        {
            DrawShelf(spriteBatch);
        }

        spriteBatch.End();
    }

    private void UpdateShelf(KeyboardState keyboard)
    {
        if (Pressed(keyboard, Keys.Escape))
        {
            ExitSelected?.Invoke();
            return;
        }

        if (_tapes.Count == 0)
            return;

        if (Pressed(keyboard, Keys.Left))
            MoveSelection(-1, 0);

        if (Pressed(keyboard, Keys.Right))
            MoveSelection(1, 0);

        if (Pressed(keyboard, Keys.Up))
            MoveSelection(0, -1);

        if (Pressed(keyboard, Keys.Down))
            MoveSelection(0, 1);

        if (Pressed(keyboard, Keys.Enter))
        {
            _editing = true;
            _field = InlayField.Description;
            _label = _labels[_selected];
            _repeatKey = null;
            return;
        }

        if (Pressed(keyboard, Keys.C))
            OpenCover();
    }

    private void UpdateInlay(KeyboardState keyboard, double delta)
    {
        if (Pressed(keyboard, Keys.Escape) ||
            Pressed(keyboard, Keys.Enter))
        {
            SaveInlay();
            return;
        }

        if (Pressed(keyboard, Keys.Up))
        {
            _field = _field == InlayField.Description
                ? InlayField.Kind
                : _field - 1;
        }

        if (Pressed(keyboard, Keys.Down))
        {
            _field = _field == InlayField.Kind
                ? InlayField.Description
                : _field + 1;
        }

        if (_field == InlayField.Kind)
        {
            if (Pressed(keyboard, Keys.Left))
                CycleKind(-1);

            if (Pressed(keyboard, Keys.Right))
                CycleKind(1);

            return;
        }

        var typed = false;

        foreach (var key in keyboard.GetPressedKeys())
        {
            if (!Pressed(keyboard, key))
                continue;

            if (!TypeKey(keyboard, key))
                continue;

            typed = true;
            _repeatKey = key;
            _repeatTimer = RepeatDelay;
        }

        if (_repeatKey.HasValue)
        {
            if (keyboard.IsKeyUp(_repeatKey.Value))
            {
                _repeatKey = null;
            }
            else
            {
                _repeatTimer -= delta;

                if (_repeatTimer <= 0)
                {
                    TypeKey(keyboard, _repeatKey.Value);
                    _repeatTimer = RepeatInterval;
                }
            }
        }

        if (typed)
            _cursorTimer = 0;
    }

    private void SaveInlay()
    {
        if (_label != null)
        {
            _machine.SaveTapeLabel(_tapes[_selected], _label);
        }

        _editing = false;
        _repeatKey = null;
        ResetDescriptionScroll();
    }

    private void MoveSelection(int columnStep, int rowStep)
    {
        var column = _selected % Columns;
        var row = _selected / Columns;
        var nextColumn = column + columnStep;
        var nextRow = row + rowStep;

        if (nextColumn < 0 || nextColumn >= Columns || nextRow < 0)
            return;

        var next = (nextRow * Columns) + nextColumn;

        if (next >= _tapes.Count)
            return;

        _selected = next;
        _page = _selected / PageSize;
        ResetDescriptionScroll();
    }

    private void CycleKind(int direction)
    {
        if (_label == null)
            return;

        var count = Enum.GetValues<TapeKind>().Length;
        var next = ((int)_label.Kind + direction + count) % count;
        _label.Kind = (TapeKind)next;
    }

    private bool TypeKey(KeyboardState keyboard, Keys key)
    {
        if (_label == null)
            return false;

        if (key == Keys.Back)
        {
            Backspace();
            return true;
        }

        var character = ReadCharacter(keyboard, key);

        if (character == null)
            return false;

        if (_field == InlayField.Description)
        {
            _label.Description = Append(
                _label.Description,
                character.Value,
                TapeLabel.MaxDescriptionLength);
        }
        else if (_field == InlayField.Author)
        {
            _label.Author = Append(
                _label.Author,
                character.Value,
                TapeLabel.MaxAuthorLength);
        }

        return true;
    }

    private void Backspace()
    {
        if (_label == null)
            return;

        if (_field == InlayField.Description &&
            _label.Description.Length > 0)
        {
            _label.Description =
                _label.Description[..^1];
        }
        else if (_field == InlayField.Author &&
                 _label.Author.Length > 0)
        {
            _label.Author = _label.Author[..^1];
        }
    }

    private static string Append(string value, char character, int maxLength)
    {
        if (value.Length >= maxLength)
            return value;

        return value + character;
    }

    private static char? ReadCharacter(KeyboardState keyboard, Keys key)
    {
        var shift =
            keyboard.IsKeyDown(Keys.LeftShift) ||
            keyboard.IsKeyDown(Keys.RightShift);

        if (key >= Keys.A && key <= Keys.Z)
            return (char)('A' + (key - Keys.A));

        if (key >= Keys.D0 && key <= Keys.D9)
        {
            var offset = key - Keys.D0;

            return shift
                ? ShiftedNumbers[offset]
                : (char)('0' + offset);
        }

        if (key == Keys.Space)
            return ' ';

        return key switch
        {
            Keys.OemPeriod => shift ? '>' : '.',
            Keys.OemComma => shift ? '<' : ',',
            Keys.OemQuestion => shift ? '?' : '/',
            Keys.OemMinus => shift ? '_' : '-',
            Keys.OemQuotes => shift ? '@' : '\'',
            _ => null
        };
    }

    private void DrawShelf(SpriteBatch spriteBatch)
    {
        DrawText(spriteBatch, "MY SOFTWARE", 272, 24, Cream);
        DrawText(spriteBatch, "TAPE SHELF", 280, 44, Cyan);

        DrawText(
            spriteBatch,
            "0101010101010101010101010101010101010101010101010101010101010101010101",
            24,
            72,
            Cyan);

        if (_tapes.Count == 0)
        {
            DrawEmpty(spriteBatch);
        }
        else
        {
            DrawTapes(spriteBatch);
        }

        DrawBox(
            spriteBatch,
            new Rectangle(16, 424, 608, 24),
            Cyan);

        var footer = _tapes.Count == 0
            ? "ESC BACK"
            : "ARROWS SELECT    ENTER INLAY    C COVER    ESC BACK";

        DrawText(spriteBatch, footer, 48, 432, Dark);
    }

    private void DrawEmpty(SpriteBatch spriteBatch)
    {
        DrawText(spriteBatch, "THE SHELF IS EMPTY.", 216, 180, Cream);
        DrawText(spriteBatch, "SAVE A PROGRAM ON THE CENTAURI64", 160, 212, Yellow);
        DrawText(spriteBatch, "AND THE TAPE WILL APPEAR HERE.", 168, 232, Yellow);
    }

    private void DrawTapes(SpriteBatch spriteBatch)
    {
        var countLabel = _tapes.Count == 1
            ? "1 TAPE"
            : $"{_tapes.Count} TAPES";

        DrawText(spriteBatch, countLabel, 48, 84, Yellow);

        if (PageCount() > 1)
        {
            var pageLabel = $"PAGE {_page + 1}/{PageCount()}";
            var pageX = 592 - (pageLabel.Length * CharacterWidth);

            DrawText(spriteBatch, pageLabel, pageX, 84, Muted);
        }

        var start = _page * PageSize;
        var end = Math.Min(start + PageSize, _tapes.Count);

        for (var index = start; index < end; index++)
        {
            var slot = index - start;
            var column = slot % Columns;
            var row = slot / Columns;
            var x = OriginX + (column * (CassetteWidth + GapX));
            var y = OriginY + (row * (CassetteHeight + GapY));

            if (index == _selected)
            {
                DrawBox(
                    spriteBatch,
                    new Rectangle(x - 4, y - 4, CassetteWidth + 8, CassetteHeight + 8),
                    Yellow);
            }

            DrawCassette(
                spriteBatch,
                _tapes[index],
                _labels[index],
                _covers[index],
                x,
                y,
                index == _selected);
        }
    }

    private void DrawCassette(
        SpriteBatch spriteBatch,
        string name,
        TapeLabel label,
        TapeCover cover,
        int x,
        int y,
        bool selected)
    {
        DrawBox(
            spriteBatch,
            new Rectangle(x, y, CassetteWidth, CassetteHeight),
            Shell);

        DrawBox(
            spriteBatch,
            new Rectangle(x + 8, y + 8, CassetteWidth - 16, CassetteHeight - 16),
            Cream);

        var hasArt = cover.HasArt;
        var textX = hasArt ? x + 64 : x + 44;
        var reserved = hasArt ? 120 : 88;
        var maxCharacters = (CassetteWidth - reserved) / CharacterWidth;

        DrawBox(spriteBatch, new Rectangle(x + 20, y + 34, 16, 16), Dark);
        DrawBox(
            spriteBatch,
            new Rectangle(x + CassetteWidth - 36, y + 34, 16, 16),
            Dark);

        if (hasArt)
        {
            DrawCoverPreview(
                spriteBatch,
                cover,
                x + 8,
                y + 8,
                CoverPreviewWidth,
                CoverPreviewHeight);
        }

        var title = Fit(name, maxCharacters);
        var subtitle = Subtitle(label, maxCharacters, selected);

        DrawText(spriteBatch, title, textX, y + 28, Dark);
        DrawText(spriteBatch, subtitle, textX, y + 48, Header);
    }

    private void DrawInlay(SpriteBatch spriteBatch)
    {
        if (_label == null)
            return;

        DrawText(spriteBatch, "TAPE INLAY", 280, 24, Cream);
        DrawText(spriteBatch, _tapes[_selected], 48, 44, Cyan);

        DrawText(spriteBatch, "DESCRIPTION", 48, 96, Yellow);
        DrawField(
            spriteBatch,
            _label.Description,
            TapeLabel.MaxDescriptionLength,
            48,
            116,
            _field == InlayField.Description);

        DrawText(spriteBatch, "AUTHOR", 48, 176, Yellow);
        DrawField(
            spriteBatch,
            _label.Author,
            TapeLabel.MaxAuthorLength,
            48,
            196,
            _field == InlayField.Author);

        DrawText(spriteBatch, "KIND", 48, 256, Yellow);
        DrawText(
            spriteBatch,
            "< " + _label.Kind.ToString().ToUpperInvariant() + " >",
            48,
            276,
            _field == InlayField.Kind ? Yellow : Cream);

        var machine = _label.MachineVersion <= 0
            ? "UNKNOWN"
            : _label.MachineVersion.ToString();

        DrawText(spriteBatch, "MACHINE " + machine, 48, 336, Muted);

        DrawInlayCover(spriteBatch);

        DrawBox(
            spriteBatch,
            new Rectangle(16, 424, 608, 24),
            Cyan);

        DrawText(
            spriteBatch,
            "UP DOWN FIELD    LEFT RIGHT KIND    ESC SAVE",
            48,
            432,
            Dark);
    }

    private void DrawField(
        SpriteBatch spriteBatch,
        string value,
        int maxLength,
        int x,
        int y,
        bool active)
    {
        var width = (maxLength * CharacterWidth) + 16;

        if (active)
        {
            DrawBox(
                spriteBatch,
                new Rectangle(x - 4, y - 4, width + 8, 24),
                Yellow);
        }

        DrawBox(spriteBatch, new Rectangle(x, y, width, 16), Cream);
        DrawText(spriteBatch, value, x + 8, y + 4, Dark);

        if (active && ((int)_cursorTimer % 2) == 0)
        {
            DrawBox(
                spriteBatch,
                new Rectangle(
                    x + 8 + (value.Length * CharacterWidth),
                    y + 4,
                    CharacterWidth,
                    8),
                Dark);
        }
    }

    private int PageCount()
    {
        if (_tapes.Count == 0)
            return 1;

        return (_tapes.Count + PageSize - 1) / PageSize;
    }

    private void AdvanceDescriptionScroll(double delta)
    {
        if (_tapes.Count == 0)
            return;

        var description = _labels[_selected].Description;
        var reserved = _covers[_selected].HasArt ? 120 : 88;
        var limit = (CassetteWidth - reserved) / CharacterWidth;

        if (description.Length <= limit)
            return;

        _descriptionScrollTimer += delta;

        if (_descriptionHolding)
        {
            if (_descriptionScrollTimer < ScrollHold)
                return;

            _descriptionHolding = false;
            _descriptionScrollTimer = 0;
            return;
        }

        if (_descriptionScrollTimer < ScrollInterval)
            return;

        _descriptionScrollTimer = 0;
        _descriptionScroll++;

        if (_descriptionScroll >= description.Length + ScrollGap)
        {
            _descriptionScroll = 0;
            _descriptionHolding = true;
        }
    }

    private void ResetDescriptionScroll()
    {
        _descriptionScroll = 0;
        _descriptionScrollTimer = 0;
        _descriptionHolding = true;
    }

    private string Subtitle(
        TapeLabel label,
        int maxCharacters,
        bool selected)
    {
        if (string.IsNullOrEmpty(label.Description))
            return label.Kind.ToString().ToUpperInvariant();

        if (!selected || label.Description.Length <= maxCharacters)
            return Fit(label.Description, maxCharacters);

        var band = label.Description + new string(' ', ScrollGap);
        var doubled = band + band;

        return doubled.Substring(
            _descriptionScroll,
            maxCharacters);
    }

    private static string Fit(string value, int maxCharacters)
    {
        if (value.Length <= maxCharacters)
            return value;

        return value[..maxCharacters];
    }

    private void DrawText(
        SpriteBatch spriteBatch,
        string text,
        int x,
        int y,
        Color colour)
    {
        _font.Draw(spriteBatch, text, new Vector2(x, y), colour);
    }

    private void DrawBox(
        SpriteBatch spriteBatch,
        Rectangle rectangle,
        Color colour)
    {
        spriteBatch.Draw(_whitePixel, rectangle, colour);
    }

    private bool Pressed(KeyboardState keyboard, Keys key)
    {
        return keyboard.IsKeyDown(key) &&
            !_previousKeyboard.IsKeyDown(key);
    }
}
