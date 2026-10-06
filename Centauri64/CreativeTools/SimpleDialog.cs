using System;

using Centauri64.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Centauri64.CreativeTools;

public enum DialogKind
{
    None,
    TextEntry,
    Confirm,
    ModeChoice,
    Help
}

public sealed class SimpleDialog
{
    public DialogKind Kind { get; private set; }
    public string Title { get; private set; } = "";
    public string Message { get; private set; } = "";
    public string Text { get; private set; } = "";
    public string ExtraLabel { get; private set; } = "";
    public int ExtraIndex { get; private set; }
    public string[] ExtraOptions { get; private set; } = Array.Empty<string>();

    private Action<string, int>? _onAccept;
    private Action? _onCancel;
    private const int MaxText = 16;

    public bool IsOpen => Kind != DialogKind.None;

    public void ShowTextEntry(
        string title,
        string message,
        string initial,
        Action<string, int> onAccept,
        Action? onCancel = null,
        string extraLabel = "",
        string[]? extraOptions = null,
        int extraIndex = 0)
    {
        Kind = DialogKind.TextEntry;
        Title = title;
        Message = message;
        Text = initial ?? "";
        ExtraLabel = extraLabel;
        ExtraOptions = extraOptions ?? Array.Empty<string>();
        ExtraIndex = Math.Clamp(extraIndex, 0, Math.Max(0, ExtraOptions.Length - 1));
        _onAccept = onAccept;
        _onCancel = onCancel;
    }

    public void ShowConfirm(
        string title,
        string message,
        Action onYes,
        Action? onNo = null)
    {
        Kind = DialogKind.Confirm;
        Title = title;
        Message = message;
        Text = "";
        _onAccept = (_, _) => onYes();
        _onCancel = onNo;
    }

    public void ShowHelp(string title, string message)
    {
        Kind = DialogKind.Help;
        Title = title;
        Message = message;
        Text = "";
        _onAccept = null;
        _onCancel = null;
    }

    public void Close()
    {
        Kind = DialogKind.None;
        _onAccept = null;
        _onCancel = null;
    }

    public bool HandleKeyboard(KeyboardState keyboard, KeyboardState previous)
    {
        if (!IsOpen)
            return false;

        if (WasPressed(keyboard, previous, Keys.Escape))
        {
            var cancel = _onCancel;
            Close();
            cancel?.Invoke();
            return true;
        }

        if (Kind == DialogKind.Help)
        {
            if (WasPressed(keyboard, previous, Keys.Enter) ||
                WasPressed(keyboard, previous, Keys.Escape))
            {
                Close();
            }

            return true;
        }

        if (Kind == DialogKind.Confirm)
        {
            if (WasPressed(keyboard, previous, Keys.Y) ||
                WasPressed(keyboard, previous, Keys.Enter))
            {
                var accept = _onAccept;
                Close();
                accept?.Invoke("", 0);
            }
            else if (WasPressed(keyboard, previous, Keys.N))
            {
                var cancel = _onCancel;
                Close();
                cancel?.Invoke();
            }

            return true;
        }

        if (Kind == DialogKind.TextEntry)
        {
            if (ExtraOptions.Length > 0)
            {
                if (WasPressed(keyboard, previous, Keys.Left))
                    ExtraIndex = (ExtraIndex - 1 + ExtraOptions.Length) % ExtraOptions.Length;
                if (WasPressed(keyboard, previous, Keys.Right))
                    ExtraIndex = (ExtraIndex + 1) % ExtraOptions.Length;
            }

            if (WasPressed(keyboard, previous, Keys.Enter))
            {
                var accept = _onAccept;
                var text = Text;
                var extra = ExtraIndex;
                Close();
                accept?.Invoke(text, extra);
                return true;
            }

            foreach (var key in keyboard.GetPressedKeys())
            {
                if (!previous.IsKeyUp(key))
                    continue;

                if (key == Keys.Back && Text.Length > 0)
                {
                    Text = Text[..^1];
                    continue;
                }

                var ch = KeyToChar(key, keyboard);
                if (ch != null && Text.Length < MaxText)
                    Text += ch;
            }
        }

        return true;
    }

    public bool HandleMouse(MouseState mouse, MouseState previous, int screenWidth, int screenHeight)
    {
        if (!IsOpen)
            return false;

        if (mouse.LeftButton != ButtonState.Pressed ||
            previous.LeftButton != ButtonState.Released)
            return true;

        var box = GetBox(screenWidth, screenHeight);

        if (Kind == DialogKind.Confirm)
        {
            var yes = new Rectangle(box.X + 24, box.Bottom - 28, 48, 16);
            var no = new Rectangle(box.X + 90, box.Bottom - 28, 48, 16);
            if (yes.Contains(mouse.X, mouse.Y))
            {
                var accept = _onAccept;
                Close();
                accept?.Invoke("", 0);
                return true;
            }

            if (no.Contains(mouse.X, mouse.Y))
            {
                var cancel = _onCancel;
                Close();
                cancel?.Invoke();
                return true;
            }
        }

        if (Kind == DialogKind.TextEntry)
        {
            var create = new Rectangle(box.X + 24, box.Bottom - 28, 64, 16);
            var cancelRect = new Rectangle(box.X + 100, box.Bottom - 28, 64, 16);
            if (create.Contains(mouse.X, mouse.Y))
            {
                var accept = _onAccept;
                var text = Text;
                var extra = ExtraIndex;
                Close();
                accept?.Invoke(text, extra);
                return true;
            }

            if (cancelRect.Contains(mouse.X, mouse.Y))
            {
                var cancel = _onCancel;
                Close();
                cancel?.Invoke();
                return true;
            }

            if (ExtraOptions.Length > 0)
            {
                var left = new Rectangle(box.X + 80, box.Y + 72, 16, 16);
                var right = new Rectangle(box.X + 220, box.Y + 72, 16, 16);
                if (left.Contains(mouse.X, mouse.Y))
                    ExtraIndex = (ExtraIndex - 1 + ExtraOptions.Length) % ExtraOptions.Length;
                if (right.Contains(mouse.X, mouse.Y))
                    ExtraIndex = (ExtraIndex + 1) % ExtraOptions.Length;
            }
        }

        if (Kind == DialogKind.Help && !box.Contains(mouse.X, mouse.Y))
            Close();

        return true;
    }

    public void Draw(
        SpriteBatch spriteBatch,
        BitmapFont font,
        Texture2D pixel,
        int screenWidth,
        int screenHeight)
    {
        if (!IsOpen)
            return;

        spriteBatch.Draw(
            pixel,
            new Rectangle(0, 0, screenWidth, screenHeight),
            new Color(0, 0, 0, 160));

        var box = GetBox(screenWidth, screenHeight);
        spriteBatch.Draw(pixel, box, CreativeUiTheme.Panel);
        spriteBatch.Draw(pixel, new Rectangle(box.X, box.Y, box.Width, 1), CreativeUiTheme.Border);
        spriteBatch.Draw(pixel, new Rectangle(box.X, box.Bottom - 1, box.Width, 1), CreativeUiTheme.Border);
        spriteBatch.Draw(pixel, new Rectangle(box.X, box.Y, 1, box.Height), CreativeUiTheme.Border);
        spriteBatch.Draw(pixel, new Rectangle(box.Right - 1, box.Y, 1, box.Height), CreativeUiTheme.Border);

        font.Draw(spriteBatch, Title, new Vector2(box.X + 12, box.Y + 10), CreativeUiTheme.Highlight);

        var lines = Message.Split('\n');
        for (var i = 0; i < lines.Length; i++)
            font.Draw(spriteBatch, lines[i], new Vector2(box.X + 12, box.Y + 32 + i * 14), CreativeUiTheme.Text);

        if (Kind == DialogKind.TextEntry)
        {
            font.Draw(spriteBatch, ">" + Text + "_", new Vector2(box.X + 12, box.Y + 52), CreativeUiTheme.Accent);
            if (ExtraOptions.Length > 0)
            {
                font.Draw(spriteBatch, ExtraLabel + ":", new Vector2(box.X + 12, box.Y + 76), CreativeUiTheme.Muted);
                font.Draw(spriteBatch, "< " + ExtraOptions[ExtraIndex] + " >", new Vector2(box.X + 96, box.Y + 76), CreativeUiTheme.Text);
            }

            font.Draw(spriteBatch, "CREATE", new Vector2(box.X + 28, box.Bottom - 24), CreativeUiTheme.Accent);
            font.Draw(spriteBatch, "CANCEL", new Vector2(box.X + 104, box.Bottom - 24), CreativeUiTheme.Muted);
        }
        else if (Kind == DialogKind.Confirm)
        {
            font.Draw(spriteBatch, "YES", new Vector2(box.X + 36, box.Bottom - 24), CreativeUiTheme.Accent);
            font.Draw(spriteBatch, "NO", new Vector2(box.X + 102, box.Bottom - 24), CreativeUiTheme.Muted);
        }
        else if (Kind == DialogKind.Help)
        {
            font.Draw(spriteBatch, "ENTER / ESC CLOSE", new Vector2(box.X + 12, box.Bottom - 24), CreativeUiTheme.Muted);
        }
    }

    private Rectangle GetBox(int screenWidth, int screenHeight)
    {
        var height = Kind == DialogKind.Help ? 220 : 140;
        var width = 320;
        return new Rectangle(
            (screenWidth - width) / 2,
            (screenHeight - height) / 2,
            width,
            height);
    }

    private static string? KeyToChar(Keys key, KeyboardState keyboard)
    {
        var shift =
            keyboard.IsKeyDown(Keys.LeftShift) ||
            keyboard.IsKeyDown(Keys.RightShift);

        if (key is >= Keys.A and <= Keys.Z)
            return key.ToString();

        if (key is >= Keys.D0 and <= Keys.D9)
            return ((char)('0' + (key - Keys.D0))).ToString();

        if (key is >= Keys.NumPad0 and <= Keys.NumPad9)
            return ((char)('0' + (key - Keys.NumPad0))).ToString();

        if (key == Keys.OemMinus || key == Keys.Subtract)
            return shift ? null : "-";

        return key == Keys.OemComma ? null : null;
    }

    private static bool WasPressed(
        KeyboardState keyboard,
        KeyboardState previous,
        Keys key)
    {
        return keyboard.IsKeyDown(key) && previous.IsKeyUp(key);
    }
}
