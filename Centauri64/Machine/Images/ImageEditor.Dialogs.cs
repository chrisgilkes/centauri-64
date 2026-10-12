using System;

using Centauri64.CreativeTools;
using Centauri64.Graphics;
using Centauri64.Machine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Centauri64.Machine.Images;

public sealed partial class ImageEditor
{
    private enum WizardKind
    {
        None,
        NewImage,
        Properties
    }

    private WizardKind _wizard;
    private string _wizardName = "";
    private int _wizardModeIndex;
    private string _wizardWidth = "16";
    private string _wizardHeight = "16";
    private int _wizardField; // NewImage: 0 name, 1 width, 2 height, 3 mode — Properties: 0 name
    private string _wizardError = "";

    private void BeginNewImage()
    {
        _wizard = WizardKind.NewImage;
        _wizardName = "";
        _wizardModeIndex = 0;
        _wizardWidth = "16";
        _wizardHeight = "16";
        _wizardField = 0;
        _wizardError = "";
    }

    private void BeginProperties()
    {
        if (_image == null)
            return;

        _wizard = WizardKind.Properties;
        _wizardName = _image.Name;
        _wizardModeIndex = _image.Mode == CentauriDisplayMode.Arcade ? 1 : 0;
        _wizardWidth = _image.Width.ToString();
        _wizardHeight = _image.Height.ToString();
        _wizardField = 0;
        _wizardError = "";
    }

    private void CloseWizard()
    {
        _wizard = WizardKind.None;
        _wizardError = "";
    }

    private bool WizardOpen => _wizard != WizardKind.None;

    private int WizardFieldCount =>
        _wizard == WizardKind.Properties
            ? 1
            : IsSpriteArtworkMode ? 1 : 4;

    private bool HandleWizardKeyboard(KeyboardState keyboard, KeyboardState previous)
    {
        if (!WizardOpen)
            return false;

        if (WasPressed(keyboard, previous, Keys.Escape))
        {
            CloseWizard();
            return true;
        }

        if (WasPressed(keyboard, previous, Keys.Enter))
        {
            CommitWizard();
            return true;
        }

        if (WasPressed(keyboard, previous, Keys.Up))
            _wizardField = Math.Max(0, _wizardField - 1);
        if (WasPressed(keyboard, previous, Keys.Down))
            _wizardField = Math.Min(WizardFieldCount - 1, _wizardField + 1);

        if (WasPressed(keyboard, previous, Keys.Left) ||
            WasPressed(keyboard, previous, Keys.Right))
        {
            if (_wizard == WizardKind.NewImage &&
                !IsSpriteArtworkMode &&
                _wizardField == 3)
            {
                _wizardModeIndex = 1 - _wizardModeIndex;
            }
        }

        if (WasPressed(keyboard, previous, Keys.Tab))
            _wizardField = (_wizardField + 1) % WizardFieldCount;

        foreach (var key in keyboard.GetPressedKeys())
        {
            if (!previous.IsKeyUp(key))
                continue;

            if (key == Keys.Back)
            {
                if (_wizardField == 0 && _wizardName.Length > 0)
                {
                    _wizardName = _wizardName[..^1];
                    _wizardError = "";
                }
                else if (_wizard == WizardKind.NewImage &&
                         !IsSpriteArtworkMode &&
                         _wizardField == 1 &&
                         _wizardWidth.Length > 0)
                {
                    _wizardWidth = _wizardWidth[..^1];
                    _wizardError = "";
                }
                else if (_wizard == WizardKind.NewImage &&
                         !IsSpriteArtworkMode &&
                         _wizardField == 2 &&
                         _wizardHeight.Length > 0)
                {
                    _wizardHeight = _wizardHeight[..^1];
                    _wizardError = "";
                }

                continue;
            }

            var ch = KeyToChar(key);
            if (ch == null)
                continue;

            if (_wizardField == 0 && _wizardName.Length < 16)
            {
                _wizardName += ch;
                _wizardError = "";
            }
            else if (_wizard == WizardKind.NewImage &&
                     !IsSpriteArtworkMode &&
                     _wizardField == 1 &&
                     _wizardWidth.Length < 4 &&
                     char.IsDigit(ch[0]))
            {
                _wizardWidth += ch;
                _wizardError = "";
            }
            else if (_wizard == WizardKind.NewImage &&
                     !IsSpriteArtworkMode &&
                     _wizardField == 2 &&
                     _wizardHeight.Length < 4 &&
                     char.IsDigit(ch[0]))
            {
                _wizardHeight += ch;
                _wizardError = "";
            }
        }

        return true;
    }

    private bool HandleWizardMouse(MouseState mouse, MouseState previous)
    {
        if (!WizardOpen)
            return false;

        if (mouse.LeftButton != ButtonState.Pressed ||
            previous.LeftButton != ButtonState.Released)
            return true;

        var box = WizardBox();
        var create = new Rectangle(box.X + 24, box.Bottom - 28, 64, 16);
        var cancel = new Rectangle(box.X + 100, box.Bottom - 28, 64, 16);
        if (create.Contains(mouse.X, mouse.Y))
        {
            CommitWizard();
            return true;
        }

        if (cancel.Contains(mouse.X, mouse.Y))
        {
            CloseWizard();
            return true;
        }

        if (_wizard == WizardKind.NewImage)
        {
            if (new Rectangle(box.X + 12, box.Y + 36, box.Width - 24, 16).Contains(mouse.X, mouse.Y))
                _wizardField = 0;

            if (!IsSpriteArtworkMode)
            {
                if (new Rectangle(box.X + 12, box.Y + 58, 140, 16).Contains(mouse.X, mouse.Y))
                    _wizardField = 1;
                if (new Rectangle(box.X + 160, box.Y + 58, 140, 16).Contains(mouse.X, mouse.Y))
                    _wizardField = 2;

                var modeLeft = new Rectangle(box.X + 120, box.Y + 82, 16, 14);
                var modeRight = new Rectangle(box.X + 280, box.Y + 82, 16, 14);
                if (modeLeft.Contains(mouse.X, mouse.Y) || modeRight.Contains(mouse.X, mouse.Y))
                {
                    _wizardField = 3;
                    _wizardModeIndex = 1 - _wizardModeIndex;
                }
                else if (new Rectangle(box.X + 12, box.Y + 82, box.Width - 24, 16).Contains(mouse.X, mouse.Y))
                {
                    _wizardField = 3;
                }
            }
        }
        else if (new Rectangle(box.X + 12, box.Y + 36, box.Width - 24, 16).Contains(mouse.X, mouse.Y))
        {
            _wizardField = 0;
        }

        return true;
    }

    private void CommitWizard()
    {
        if (_wizard == WizardKind.NewImage)
            CommitNewImage();
        else if (_wizard == WizardKind.Properties)
            CommitProperties();
    }

    private void CommitNewImage()
    {
        if (!TryNormalizeName(_wizardName, out var valid, out var error))
        {
            _wizardError = error;
            return;
        }

        if (_assets.Contains(valid))
        {
            _wizardError = "IMAGE NAME ALREADY EXISTS";
            return;
        }

        var mode = _wizardModeIndex == 1
            ? CentauriDisplayMode.Arcade
            : CentauriDisplayMode.HighResolution;

        int width;
        int height;

        if (IsSpriteArtworkMode)
        {
            width = CentauriSprite.WIDTH;
            height = CentauriSprite.HEIGHT;
        }
        else
        {
            if (!int.TryParse(_wizardWidth, out width) ||
                !int.TryParse(_wizardHeight, out height))
            {
                _wizardError = "INVALID SIZE";
                return;
            }
        }

        try
        {
            ImageAsset.ValidateSize(mode, width, height);
        }
        catch (Exception ex)
        {
            _wizardError = ex.Message.ToUpperInvariant();
            return;
        }

        var image = new ImageAsset(valid, mode, width, height, ImageAsset.Transparent);
        _assets.Add(image);
        _selectedIndex = _assets.Count - 1;
        _image = image;
        ClearHistory();
        CommitLibraryNow(valid);
        CloseWizard();
        FitZoom();
        _statusTip = $"CREATED {valid}  {width}X{height}";
    }

    private void CommitProperties()
    {
        if (_image == null)
            return;

        if (!TryNormalizeName(_wizardName, out var valid, out var error))
        {
            _wizardError = error;
            return;
        }

        if (!string.Equals(valid, _image.Name, StringComparison.Ordinal) &&
            _assets.Contains(valid))
        {
            _wizardError = "IMAGE NAME ALREADY EXISTS";
            return;
        }

        _image.Name = valid;
        CommitLibraryNow(valid);
        CloseWizard();
        _statusTip = "PROPERTIES UPDATED";
    }

    private Rectangle WizardBox()
    {
        var height = _wizard == WizardKind.NewImage
            ? (IsSpriteArtworkMode ? 154 : 184)
            : 154;
        return new Rectangle((ScreenW - 340) / 2, (ScreenH - height) / 2, 340, height);
    }

    private void DrawWizard(SpriteBatch spriteBatch, BitmapFont font, Texture2D pixel)
    {
        if (!WizardOpen)
            return;

        spriteBatch.Draw(
            pixel,
            new Rectangle(0, 0, ScreenW, ScreenH),
            new Color(0, 0, 0, 160));

        var box = WizardBox();
        spriteBatch.Draw(pixel, box, CreativeUiTheme.Panel);
        spriteBatch.Draw(pixel, new Rectangle(box.X, box.Y, box.Width, 1), CreativeUiTheme.Border);
        spriteBatch.Draw(pixel, new Rectangle(box.X, box.Bottom - 1, box.Width, 1), CreativeUiTheme.Border);
        spriteBatch.Draw(pixel, new Rectangle(box.X, box.Y, 1, box.Height), CreativeUiTheme.Border);
        spriteBatch.Draw(pixel, new Rectangle(box.Right - 1, box.Y, 1, box.Height), CreativeUiTheme.Border);

        var title = _wizard == WizardKind.NewImage
            ? (IsSpriteArtworkMode ? "NEW 16X16 ARTWORK" : "NEW IMAGE")
            : "IMAGE PROPERTIES";
        font.Draw(spriteBatch, title, new Vector2(box.X + 12, box.Y + 10), CreativeUiTheme.Highlight);

        font.Draw(spriteBatch, "NAME", new Vector2(box.X + 12, box.Y + 36), CreativeUiTheme.Muted);
        font.Draw(
            spriteBatch,
            ">" + _wizardName + (_wizardField == 0 ? "_" : ""),
            new Vector2(box.X + 60, box.Y + 36),
            CreativeUiTheme.Accent);

        if (_wizard == WizardKind.NewImage)
        {
            if (IsSpriteArtworkMode)
            {
                font.Draw(
                    spriteBatch,
                    $"SIZE {CentauriSprite.WIDTH}X{CentauriSprite.HEIGHT}  (LOCKED)",
                    new Vector2(box.X + 12, box.Y + 66),
                    CreativeUiTheme.Muted);
            }
            else
            {
                font.Draw(spriteBatch, "WIDTH", new Vector2(box.X + 12, box.Y + 58), CreativeUiTheme.Muted);
                font.Draw(
                    spriteBatch,
                    ">" + _wizardWidth + (_wizardField == 1 ? "_" : ""),
                    new Vector2(box.X + 70, box.Y + 58),
                    CreativeUiTheme.Text);
                font.Draw(spriteBatch, "HEIGHT", new Vector2(box.X + 160, box.Y + 58), CreativeUiTheme.Muted);
                font.Draw(
                    spriteBatch,
                    ">" + _wizardHeight + (_wizardField == 2 ? "_" : ""),
                    new Vector2(box.X + 230, box.Y + 58),
                    CreativeUiTheme.Text);

                DrawWizardRow(
                    spriteBatch,
                    font,
                    box.X + 12,
                    box.Y + 82,
                    "MODE",
                    _wizardModeIndex == 0 ? "STANDARD" : "ARCADE",
                    _wizardField == 3);

                var mode = _wizardModeIndex == 1
                    ? CentauriDisplayMode.Arcade
                    : CentauriDisplayMode.HighResolution;
                font.Draw(
                    spriteBatch,
                    $"MAX {ImageAsset.ModeWidth(mode)}X{ImageAsset.ModeHeight(mode)}",
                    new Vector2(box.X + 12, box.Y + 106),
                    CreativeUiTheme.Muted);
            }
        }
        else if (_image != null)
        {
            font.Draw(
                spriteBatch,
                $"SIZE {_image.Width}X{_image.Height}  MODE {(_image.Mode == CentauriDisplayMode.Arcade ? "ARCADE" : "STANDARD")}",
                new Vector2(box.X + 12, box.Y + 66),
                CreativeUiTheme.Muted);
            font.Draw(
                spriteBatch,
                $"FRAMES {_image.FrameCount}",
                new Vector2(box.X + 12, box.Y + 84),
                CreativeUiTheme.Muted);
        }

        if (_wizardError.Length > 0)
        {
            font.Draw(
                spriteBatch,
                _wizardError,
                new Vector2(box.X + 12, box.Bottom - 42),
                CreativeUiTheme.Highlight);
        }

        font.Draw(
            spriteBatch,
            _wizard == WizardKind.Properties ? "OK" : "CREATE",
            new Vector2(box.X + 28, box.Bottom - 24),
            CreativeUiTheme.Accent);
        font.Draw(spriteBatch, "CANCEL", new Vector2(box.X + 104, box.Bottom - 24), CreativeUiTheme.Muted);
    }

    private static void DrawWizardRow(
        SpriteBatch spriteBatch,
        BitmapFont font,
        int x,
        int y,
        string label,
        string value,
        bool hot)
    {
        font.Draw(spriteBatch, label, new Vector2(x, y), CreativeUiTheme.Muted);
        font.Draw(
            spriteBatch,
            "< " + value + " >",
            new Vector2(x + 100, y),
            hot ? CreativeUiTheme.Highlight : CreativeUiTheme.Text);
    }

    private static string? KeyToChar(Keys key)
    {
        if (key is >= Keys.A and <= Keys.Z)
            return key.ToString();
        if (key is >= Keys.D0 and <= Keys.D9)
            return ((char)('0' + (key - Keys.D0))).ToString();
        if (key is >= Keys.NumPad0 and <= Keys.NumPad9)
            return ((char)('0' + (key - Keys.NumPad0))).ToString();
        // Underscore is a valid name character; hyphen is not.
        if (key == Keys.OemMinus || key == Keys.Subtract)
            return "_";
        return null;
    }
}
