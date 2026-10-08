using System;
using System.Collections.Generic;

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
    private int _wizardTemplateIndex;
    private int _wizardCategoryIndex;
    private string _wizardWidth = "16";
    private string _wizardHeight = "16";
    private int _wizardField; // 0 name, 1 width, 2 height for custom

    private static readonly string[] CategoryLabels =
    {
        "GENERAL", "SPRITE", "TILESET", "BACKGROUND"
    };

    private void BeginNewImage()
    {
        _wizard = WizardKind.NewImage;
        _wizardName = "";
        _wizardModeIndex = 0;
        _wizardTemplateIndex = 1; // 16x16 sprite default
        _wizardCategoryIndex = (int)ImageCategory.Sprite;
        _wizardWidth = "16";
        _wizardHeight = "16";
        _wizardField = 0;

        if (IsSpriteArtworkMode)
        {
            // Issue #3: only 16×16 Sprite artwork may be created.
            _wizardTemplateIndex = FindTemplateIndex("16x16 SPRITE");
            _wizardCategoryIndex = (int)ImageCategory.Sprite;
            _wizardWidth = "16";
            _wizardHeight = "16";
        }

        SyncTemplateDefaults();
    }

    private int FindTemplateIndex(string label)
    {
        var templates = CurrentTemplates();
        for (var i = 0; i < templates.Count; i++)
        {
            if (templates[i].Label == label)
                return i;
        }

        return 1;
    }

    private void BeginProperties()
    {
        if (_image == null)
            return;

        _wizard = WizardKind.Properties;
        _wizardName = _image.Name;
        _wizardModeIndex = _image.Mode == CentauriDisplayMode.Arcade ? 1 : 0;
        _wizardCategoryIndex = (int)_image.Category;
        _wizardWidth = _image.Width.ToString();
        _wizardHeight = _image.Height.ToString();
        _wizardField = 0;
    }

    private void CloseWizard()
    {
        _wizard = WizardKind.None;
    }

    private bool WizardOpen => _wizard != WizardKind.None;

    private IReadOnlyList<ImageTemplate> CurrentTemplates()
    {
        var mode = _wizardModeIndex == 1
            ? CentauriDisplayMode.Arcade
            : CentauriDisplayMode.HighResolution;
        return ImageTemplate.ForMode(mode);
    }

    private void SyncTemplateDefaults()
    {
        var templates = CurrentTemplates();
        _wizardTemplateIndex = Math.Clamp(_wizardTemplateIndex, 0, templates.Count - 1);
        var template = templates[_wizardTemplateIndex];
        _wizardCategoryIndex = (int)template.Category;
        if (!template.IsCustom)
        {
            _wizardWidth = template.Width.ToString();
            _wizardHeight = template.Height.ToString();
        }
    }

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

        var templates = CurrentTemplates();

        if (WasPressed(keyboard, previous, Keys.Up))
            _wizardField = Math.Max(0, _wizardField - 1);
        if (WasPressed(keyboard, previous, Keys.Down))
            _wizardField = Math.Min(4, _wizardField + 1);

        if (WasPressed(keyboard, previous, Keys.Left) ||
            WasPressed(keyboard, previous, Keys.Right))
        {
            var dir = WasPressed(keyboard, previous, Keys.Right) ? 1 : -1;
            if (_wizard == WizardKind.NewImage)
            {
                if (IsSpriteArtworkMode)
                {
                    // Template / category / size locked in Sprite Artwork mode.
                }
                else if (_wizardField == 1)
                {
                    _wizardTemplateIndex =
                        (_wizardTemplateIndex + dir + templates.Count) % templates.Count;
                    SyncTemplateDefaults();
                }
                else if (_wizardField == 2)
                {
                    _wizardCategoryIndex =
                        (_wizardCategoryIndex + dir + CategoryLabels.Length) % CategoryLabels.Length;
                }
                else if (_wizardField == 3)
                {
                    _wizardModeIndex = 1 - _wizardModeIndex;
                    SyncTemplateDefaults();
                }
            }
            else if (_wizardField == 1 && !IsSpriteArtworkMode)
            {
                _wizardCategoryIndex =
                    (_wizardCategoryIndex + dir + CategoryLabels.Length) % CategoryLabels.Length;
            }
        }

        foreach (var key in keyboard.GetPressedKeys())
        {
            if (!previous.IsKeyUp(key))
                continue;

            if (key == Keys.Back)
            {
                if (_wizardField == 0 && _wizardName.Length > 0)
                    _wizardName = _wizardName[..^1];
                else if (_wizardField == 4 && _wizardWidth.Length > 0)
                    _wizardWidth = _wizardWidth[..^1];
                else if (_wizardField == 5 && _wizardHeight.Length > 0)
                    _wizardHeight = _wizardHeight[..^1];
                // Custom width/height use fields 4/5 when template is custom — remap:
                continue;
            }

            var ch = KeyToChar(key);
            if (ch == null)
                continue;

            if (_wizardField == 0 && _wizardName.Length < 16)
                _wizardName += ch;
            else if (IsCustomTemplate() && _wizardField == 4 && _wizardWidth.Length < 4 && char.IsDigit(ch[0]))
                _wizardWidth += ch;
            else if (IsCustomTemplate() && _wizardField == 5 && _wizardHeight.Length < 4 && char.IsDigit(ch[0]))
                _wizardHeight += ch;
        }

        // Remap field indices: 0 name, 1 template, 2 category, 3 mode, 4 width, 5 height
        if (WasPressed(keyboard, previous, Keys.Tab))
        {
            var max = _wizard == WizardKind.Properties
                ? 1
                : IsCustomTemplate() ? 5 : 3;
            _wizardField = (_wizardField + 1) % (max + 1);
        }

        return true;
    }

    private bool IsCustomTemplate()
    {
        if (_wizard != WizardKind.NewImage)
            return false;
        var templates = CurrentTemplates();
        return templates[_wizardTemplateIndex].IsCustom;
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

        // Cycle hit zones for template/category/mode rows.
        var rowY = box.Y + 70;
        for (var row = 0; row < 4; row++)
        {
            var left = new Rectangle(box.X + 120, rowY + row * 18, 16, 14);
            var right = new Rectangle(box.X + 280, rowY + row * 18, 16, 14);
            if (left.Contains(mouse.X, mouse.Y) || right.Contains(mouse.X, mouse.Y))
            {
                var dir = right.Contains(mouse.X, mouse.Y) ? 1 : -1;
                _wizardField = row + 1;
                var dummyPrev = previous;
                // reuse left/right logic via field
                if (row == 0 && _wizard == WizardKind.NewImage)
                {
                    var templates = CurrentTemplates();
                    _wizardTemplateIndex =
                        (_wizardTemplateIndex + dir + templates.Count) % templates.Count;
                    SyncTemplateDefaults();
                }
                else if (row == 1)
                {
                    _wizardCategoryIndex =
                        (_wizardCategoryIndex + dir + CategoryLabels.Length) % CategoryLabels.Length;
                }
                else if (row == 2 && _wizard == WizardKind.NewImage)
                {
                    _wizardModeIndex = 1 - _wizardModeIndex;
                    SyncTemplateDefaults();
                }
            }
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
            Notice?.Invoke(error);
            return;
        }

        if (_assets.Contains(valid))
        {
            Notice?.Invoke("IMAGE NAME ALREADY EXISTS");
            return;
        }

        var mode = _wizardModeIndex == 1
            ? CentauriDisplayMode.Arcade
            : CentauriDisplayMode.HighResolution;
        var templates = CurrentTemplates();
        var template = templates[_wizardTemplateIndex];
        var category = (ImageCategory)_wizardCategoryIndex;

        int width;
        int height;
        int fill;

        if (IsSpriteArtworkMode)
        {
            width = CentauriSprite.WIDTH;
            height = CentauriSprite.HEIGHT;
            category = ImageCategory.Sprite;
            fill = ImageAsset.Transparent;
        }
        else if (template.IsCustom)
        {
            if (!int.TryParse(_wizardWidth, out width) ||
                !int.TryParse(_wizardHeight, out height))
            {
                Notice?.Invoke("INVALID SIZE");
                return;
            }

            fill = ImageAsset.Transparent;
        }
        else
        {
            width = template.Width;
            height = template.Height;
            fill = template.FillColour;
        }

        try
        {
            ImageAsset.ValidateSize(mode, width, height);
        }
        catch (Exception ex)
        {
            Notice?.Invoke(ex.Message.ToUpperInvariant());
            return;
        }

        var image = new ImageAsset(valid, mode, width, height, category, fill);
        _assets.Add(image);
        _selectedIndex = _assets.Count - 1;
        _image = image;
        ClearHistory();
        _dirty = true;
        CloseWizard();
        FitZoom();
        _statusTip = $"CREATED {valid}";
    }

    private void CommitProperties()
    {
        if (_image == null)
            return;

        if (!TryNormalizeName(_wizardName, out var valid, out var error))
        {
            Notice?.Invoke(error);
            return;
        }

        if (!string.Equals(valid, _image.Name, StringComparison.Ordinal) &&
            _assets.Contains(valid))
        {
            Notice?.Invoke("IMAGE NAME ALREADY EXISTS");
            return;
        }

        _image.Name = valid;
        if (!IsSpriteArtworkMode)
            _image.Category = (ImageCategory)_wizardCategoryIndex;
        _dirty = true;
        CloseWizard();
        _statusTip = "PROPERTIES UPDATED";
    }

    private Rectangle WizardBox()
    {
        var height = _wizard == WizardKind.NewImage ? 200 : 160;
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
            ? (IsSpriteArtworkMode ? "NEW SPRITE ARTWORK" : "NEW IMAGE")
            : (IsSpriteArtworkMode ? "ARTWORK PROPERTIES" : "IMAGE PROPERTIES");
        font.Draw(spriteBatch, title, new Vector2(box.X + 12, box.Y + 10), CreativeUiTheme.Highlight);

        font.Draw(spriteBatch, "NAME", new Vector2(box.X + 12, box.Y + 36), CreativeUiTheme.Muted);
        font.Draw(
            spriteBatch,
            ">" + _wizardName + (_wizardField == 0 ? "_" : ""),
            new Vector2(box.X + 60, box.Y + 36),
            CreativeUiTheme.Accent);

        if (_wizard == WizardKind.NewImage)
        {
            var templates = CurrentTemplates();
            var template = templates[_wizardTemplateIndex];
            DrawWizardRow(spriteBatch, font, box.X + 12, box.Y + 70, "TEMPLATE", template.Label, _wizardField == 1);
            DrawWizardRow(spriteBatch, font, box.X + 12, box.Y + 88, "CATEGORY", CategoryLabels[_wizardCategoryIndex], _wizardField == 2);
            DrawWizardRow(
                spriteBatch,
                font,
                box.X + 12,
                box.Y + 106,
                "MODE",
                _wizardModeIndex == 0 ? "STANDARD" : "ARCADE",
                _wizardField == 3);

            if (template.IsCustom)
            {
                font.Draw(spriteBatch, "WIDTH", new Vector2(box.X + 12, box.Y + 124), CreativeUiTheme.Muted);
                font.Draw(
                    spriteBatch,
                    ">" + _wizardWidth + (_wizardField == 4 ? "_" : ""),
                    new Vector2(box.X + 70, box.Y + 124),
                    CreativeUiTheme.Text);
                font.Draw(spriteBatch, "HEIGHT", new Vector2(box.X + 160, box.Y + 124), CreativeUiTheme.Muted);
                font.Draw(
                    spriteBatch,
                    ">" + _wizardHeight + (_wizardField == 5 ? "_" : ""),
                    new Vector2(box.X + 230, box.Y + 124),
                    CreativeUiTheme.Text);
            }
            else
            {
                font.Draw(
                    spriteBatch,
                    $"SIZE {template.Width}X{template.Height}",
                    new Vector2(box.X + 12, box.Y + 124),
                    CreativeUiTheme.Muted);
            }
        }
        else if (_image != null)
        {
            DrawWizardRow(spriteBatch, font, box.X + 12, box.Y + 70, "CATEGORY", CategoryLabels[_wizardCategoryIndex], _wizardField == 1);
            font.Draw(
                spriteBatch,
                $"SIZE {_image.Width}X{_image.Height}  MODE {(_image.Mode == CentauriDisplayMode.Arcade ? "ARCADE" : "STANDARD")}",
                new Vector2(box.X + 12, box.Y + 96),
                CreativeUiTheme.Muted);
            font.Draw(
                spriteBatch,
                $"FRAMES {_image.FrameCount}",
                new Vector2(box.X + 12, box.Y + 114),
                CreativeUiTheme.Muted);
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
        if (key == Keys.OemMinus || key == Keys.Subtract)
            return "-";
        return null;
    }
}
