using System;
using System.Collections.Generic;

using Centauri64.Graphics;
using Centauri64.Machine;
using Centauri64.Publishing;
using Centauri64.Session;
using Centauri64.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Centauri64.Game;

public sealed class SettingsScreen
{
    private enum View
    {
        List,
        ResetConfirm,
        ResetType,
        DeleteConfirm,
        DeleteType
    }

    private enum RowKind
    {
        Header,
        Setting,
        Action,
        Disabled
    }

    private sealed class Row
    {
        public RowKind Kind { get; init; }
        public string Id { get; init; } = string.Empty;
        public string Label { get; init; } = string.Empty;
        public string Help { get; init; } = string.Empty;
    }

    // Settings screen palette — easy to retune later.
    private static readonly Color Background = new(24, 48, 96);
    private static readonly Color HeaderBar = new(36, 72, 128);
    private static readonly Color Cream = new(238, 232, 190);
    private static readonly Color Yellow = new(232, 205, 92);
    private static readonly Color Aqua = new(91, 214, 205);
    private static readonly Color Muted = new(130, 165, 190);
    private static readonly Color Dark = new(14, 28, 48);
    private static readonly Color Disabled = new(90, 110, 140);

    private readonly BitmapFont _font;
    private readonly Texture2D _whitePixel;
    private readonly SettingsService _settings;
    private readonly CareerService _career;
    private readonly Action<CentauriSettings> _apply;
    private readonly List<Row> _rows = new();

    private KeyboardState _previousKeyboard;
    private View _view = View.List;
    private int _selected;
    private int _scroll;
    private string _resetBuffer = string.Empty;
    private string _status = string.Empty;
    private bool _careerActions;
    private string _careerLabel = string.Empty;

    public event Action? ExitSelected;
    public event Action? CareerReset;
    public event Action? CareerDeleted;
    public event Action? ReplayIntroduction;
    public event Action? ProgressOverrideChanged;

    public SettingsScreen(
        BitmapFont font,
        Texture2D whitePixel,
        SettingsService settings,
        CareerService career,
        Action<CentauriSettings> applySettings)
    {
        _font = font;
        _whitePixel = whitePixel;
        _settings = settings;
        _career = career;
        _apply = applySettings;
        BuildRows();
    }

    public void Open(bool careerActions, string careerLabel)
    {
        _careerActions = careerActions;
        _careerLabel = careerLabel;
        _previousKeyboard = Keyboard.GetState();
        _view = View.List;
        BuildRows();
        _selected = FirstSelectableIndex();
        _scroll = 0;
        _resetBuffer = string.Empty;
        _status = "ARROW KEYS CHANGE VALUES. ESC BACK.";
    }

    public void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();

        switch (_view)
        {
            case View.List:
                UpdateList(keyboard);
                break;
            case View.ResetConfirm:
                UpdateResetConfirm(keyboard);
                break;
            case View.ResetType:
                UpdateResetType(keyboard);
                break;
            case View.DeleteConfirm:
                UpdateDeleteConfirm(keyboard);
                break;
            case View.DeleteType:
                UpdateDeleteType(keyboard);
                break;
        }

        _previousKeyboard = keyboard;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        DrawBox(
            spriteBatch,
            new Rectangle(
                0,
                0,
                CentauriMachine.DEVELOPMENT_WIDTH,
                CentauriMachine.DEVELOPMENT_HEIGHT),
            Background);

        DrawBox(spriteBatch, new Rectangle(16, 16, 608, 48), HeaderBar);
        DrawText(spriteBatch, "CENTAURI64 SETTINGS", 224, 24, Cream);

        if (_view == View.List)
            DrawList(spriteBatch);
        else if (_view == View.ResetConfirm)
            DrawResetConfirm(spriteBatch);
        else if (_view == View.ResetType)
            DrawResetType(spriteBatch);
        else if (_view == View.DeleteConfirm)
            DrawDeleteConfirm(spriteBatch);
        else
            DrawDeleteType(spriteBatch);

        spriteBatch.End();
    }

    private void BuildRows()
    {
        _rows.Clear();
        _rows.Add(new Row { Kind = RowKind.Header, Label = "DISPLAY" });
        _rows.Add(Setting("window_mode", "WINDOW MODE", "WINDOWED OR FULLSCREEN DISPLAY."));
        _rows.Add(Setting("window_scale", "WINDOW SIZE", "INTEGER SCALE OF THE 640X480 DISPLAY."));
        _rows.Add(Setting("scaling", "SCALING", "PIXEL PERFECT USES INTEGER SCALE. FIT FILLS THE WINDOW."));
        _rows.Add(Setting("vsync", "VSYNC", "SYNCHRONISE FRAMES WITH THE DISPLAY."));
        _rows.Add(Setting("crt", "CRT FILTER", "CRT EFFECT APPLIES TO THE CENTAURI64 DISPLAY ONLY."));

        _rows.Add(new Row { Kind = RowKind.Header, Label = "EDITOR" });
        _rows.Add(Setting("editor_exp", "EDITOR EXPERIENCE", "MODERN PROVIDES VISIBLE TOOLS AND CONTEXTUAL HELP."));
        _rows.Add(Setting("auto_indent", "AUTO INDENT", "PREFERENCE FOR THE UPCOMING BASIC EDITOR."));
        _rows.Add(Setting("syntax", "SYNTAX COLOURS", "PREFERENCE FOR THE UPCOMING BASIC EDITOR."));
        _rows.Add(Setting("line_hl", "LINE HIGHLIGHT", "PREFERENCE FOR THE UPCOMING BASIC EDITOR."));
        _rows.Add(Setting("tooltips", "TOOLTIPS", "PREFERENCE FOR UPCOMING CREATIVE TOOLS UI."));
        _rows.Add(Setting("grid", "GRID", "DEFAULT GRID PREFERENCE FOR VISUAL EDITORS."));

        _rows.Add(new Row { Kind = RowKind.Header, Label = "AUDIO" });
        _rows.Add(Setting("master_vol", "MASTER VOLUME", "OVERALL CENTAURI64 VOLUME."));
        _rows.Add(Setting("music_vol", "MUSIC VOLUME", "RESERVED FOR FUTURE MUSIC."));
        _rows.Add(Setting("sfx_vol", "SFX VOLUME", "BEEPS AND SOUND EFFECTS."));

        _rows.Add(new Row { Kind = RowKind.Header, Label = "ABOUT" });
        _rows.Add(new Row
        {
            Kind = RowKind.Action,
            Id = "replay_intro",
            Label = "REPLAY INTRODUCTION",
            Help = "WATCH THE 1986 CENTAURI64 INTRODUCTION AGAIN."
        });

        _rows.Add(new Row { Kind = RowKind.Header, Label = "CAREER" });
        if (_careerActions)
        {
            _rows.Add(new Row
            {
                Kind = RowKind.Action,
                Id = "reset_career",
                Label = "RESET CAREER...",
                Help = "RESET THIS CAREER SLOT WITHOUT DELETING YOUR SOFTWARE."
            });
            _rows.Add(new Row
            {
                Kind = RowKind.Action,
                Id = "delete_career",
                Label = "DELETE CAREER SLOT...",
                Help = "ERASE THIS CAREER SLOT. YOUR PROGRAMS ARE KEPT."
            });
        }

        _rows.Add(new Row
        {
            Kind = RowKind.Disabled,
            Id = "delete_all",
            Label = "DELETE ALL USER DATA      [DISABLED]",
            Help = "NOT AVAILABLE IN THIS VERSION."
        });

        if (DeveloperTools.Available)
            AppendDeveloperTestingRows();
    }

    private void AppendDeveloperTestingRows()
    {
        _rows.Add(new Row { Kind = RowKind.Header, Label = "DEVELOPER TESTING" });

        if (!_careerActions)
        {
            _rows.Add(new Row
            {
                Kind = RowKind.Disabled,
                Id = "dev_need_career",
                Label = "OPEN A BEDROOM CAREER TO TEST",
                Help = "PROGRESSION OVERRIDE REQUIRES AN ACTIVE BEDROOM CAREER."
            });
            return;
        }

        _rows.Add(Setting(
            "dev_override",
            "CAREER PROGRESSION OVERRIDE",
            "ON = SIMULATE PROGRESSION. DOES NOT CHANGE YOUR SAVE."));
        _rows.Add(Setting(
            "dev_issue",
            "SIMULATED MAGAZINE ISSUE",
            "0 = BEFORE FIRST COMPUTER. 1-" + DevMaxIssue() + " = MAGAZINE STAGES."));
        _rows.Add(Setting(
            "dev_stage",
            "PROGRESSION STAGE",
            "START = AT ISSUE. COMPLETE = ISSUE CHALLENGES DONE."));
        _rows.Add(new Row
        {
            Kind = RowKind.Action,
            Id = "dev_disable",
            Label = "DISABLE OVERRIDE / RETURN TO SAVED",
            Help = "TURN OFF SIMULATION AND USE THE REAL CAREER SAVE."
        });

        if (CareerProgressOverride.IsActive)
        {
            _rows.Add(new Row { Kind = RowKind.Header, Label = "UNLOCK DIAGNOSTICS" });
            foreach (var row in CareerProgressOverride.Diagnostics())
            {
                _rows.Add(new Row
                {
                    Kind = RowKind.Disabled,
                    Id = "diag_" + row.Label,
                    Label = PadDiag(row.Label, row.Status),
                    Help = "READ-ONLY STATUS FROM THE EFFECTIVE PROGRESSION RULES."
                });
            }
        }
    }

    private static int DevMaxIssue() =>
        Math.Min(8, CareerProgressOverride.MaxIssueNumber);

    private static string PadDiag(string label, string status)
    {
        var left = label.Length > 28 ? label[..28] : label.PadRight(28);
        return left + " " + status;
    }

    private static Row Setting(string id, string label, string help) =>
        new()
        {
            Kind = RowKind.Setting,
            Id = id,
            Label = label,
            Help = help
        };

    private void UpdateList(KeyboardState keyboard)
    {
        if (Pressed(keyboard, Keys.Escape))
        {
            _settings.Save();
            ExitSelected?.Invoke();
            return;
        }

        if (Pressed(keyboard, Keys.Up))
            MoveSelection(-1);

        if (Pressed(keyboard, Keys.Down))
            MoveSelection(1);

        var row = _rows[_selected];
        _status = row.Help;

        if (row.Kind == RowKind.Disabled)
            return;

        if (row.Kind == RowKind.Action)
        {
            if (Pressed(keyboard, Keys.Enter))
            {
                switch (row.Id)
                {
                    case "reset_career":
                        _view = View.ResetConfirm;
                        _resetBuffer = string.Empty;
                        break;
                    case "delete_career":
                        _view = View.DeleteConfirm;
                        _resetBuffer = string.Empty;
                        break;
                    case "replay_intro":
                        ReplayIntroduction?.Invoke();
                        break;
                    case "dev_disable":
                        ApplyDevOverride(false, CareerProgressOverride.IssueNumber, CareerProgressOverride.Stage);
                        _status = "OVERRIDE DISABLED. SAVED PROGRESSION RESTORED.";
                        break;
                }
            }

            return;
        }

        if (Pressed(keyboard, Keys.Left))
            ChangeSetting(row.Id, -1);

        if (Pressed(keyboard, Keys.Right))
            ChangeSetting(row.Id, 1);
    }

    private void UpdateResetConfirm(KeyboardState keyboard)
    {
        if (Pressed(keyboard, Keys.Escape) || Pressed(keyboard, Keys.N))
        {
            _view = View.List;
            _status = "RESET CANCELLED.";
            return;
        }

        if (Pressed(keyboard, Keys.Y) || Pressed(keyboard, Keys.Enter))
        {
            _view = View.ResetType;
            _resetBuffer = string.Empty;
        }
    }

    private void UpdateResetType(KeyboardState keyboard)
    {
        if (Pressed(keyboard, Keys.Escape))
        {
            _view = View.List;
            _status = "RESET CANCELLED.";
            return;
        }

        if (Pressed(keyboard, Keys.Back) && _resetBuffer.Length > 0)
            _resetBuffer = _resetBuffer[..^1];

        foreach (var key in keyboard.GetPressedKeys())
        {
            if (!Pressed(keyboard, key))
                continue;

            if (key >= Keys.A && key <= Keys.Z && _resetBuffer.Length < 16)
                _resetBuffer += (char)('A' + (key - Keys.A));
        }

        if (Pressed(keyboard, Keys.Enter))
        {
            if (_resetBuffer == "RESET")
            {
                CareerReset?.Invoke();
                _view = View.List;
                _status = "CAREER RESET. YOUR PROGRAMS WERE KEPT.";
            }
            else
            {
                _status = "TYPE RESET EXACTLY, OR PRESS ESC TO CANCEL.";
            }
        }
    }

    private void UpdateDeleteConfirm(KeyboardState keyboard)
    {
        if (Pressed(keyboard, Keys.N) || Pressed(keyboard, Keys.Escape))
        {
            _view = View.List;
            _status = "DELETE CANCELLED.";
            return;
        }

        if (Pressed(keyboard, Keys.Y) || Pressed(keyboard, Keys.Enter))
        {
            _view = View.DeleteType;
            _resetBuffer = string.Empty;
        }
    }

    private void UpdateDeleteType(KeyboardState keyboard)
    {
        if (Pressed(keyboard, Keys.Escape))
        {
            _view = View.List;
            _status = "DELETE CANCELLED.";
            return;
        }

        if (Pressed(keyboard, Keys.Back) && _resetBuffer.Length > 0)
            _resetBuffer = _resetBuffer[..^1];

        foreach (var key in keyboard.GetPressedKeys())
        {
            if (!Pressed(keyboard, key))
                continue;

            if (key >= Keys.A && key <= Keys.Z && _resetBuffer.Length < 16)
                _resetBuffer += (char)('A' + (key - Keys.A));
        }

        if (Pressed(keyboard, Keys.Enter))
        {
            if (_resetBuffer == "DELETE")
            {
                CareerDeleted?.Invoke();
                _view = View.List;
                _status = "CAREER SLOT DELETED.";
            }
            else
            {
                _status = "TYPE DELETE EXACTLY, OR PRESS ESC TO CANCEL.";
            }
        }
    }

    private void MoveSelection(int direction)
    {
        var index = _selected;

        do
        {
            index = (index + direction + _rows.Count) % _rows.Count;
        }
        while (_rows[index].Kind is RowKind.Header or RowKind.Disabled);

        _selected = index;
        EnsureVisible();
    }

    private int FirstSelectableIndex()
    {
        for (var i = 0; i < _rows.Count; i++)
        {
            if (_rows[i].Kind is RowKind.Setting or RowKind.Action)
                return i;
        }

        return 0;
    }

    private void EnsureVisible()
    {
        const int visible = 14;
        if (_selected < _scroll)
            _scroll = _selected;

        if (_selected >= _scroll + visible)
            _scroll = _selected - visible + 1;
    }

    private void ChangeSetting(string id, int direction)
    {
        var s = _settings.Current;
        var maxScale = GetMaxWindowScale();

        switch (id)
        {
            case "window_mode":
                s.WindowMode = s.WindowMode == WindowModeSetting.Windowed
                    ? WindowModeSetting.Fullscreen
                    : WindowModeSetting.Windowed;
                break;

            case "window_scale":
                s.WindowScale = CentauriSettings.ClampScale(
                    s.WindowScale + direction,
                    maxScale);
                break;

            case "scaling":
                s.ScalingMode = s.ScalingMode == ScalingModeSetting.PixelPerfect
                    ? ScalingModeSetting.FitWindow
                    : ScalingModeSetting.PixelPerfect;
                break;

            case "vsync":
                s.VSync = !s.VSync;
                break;

            case "crt":
                s.CrtFilter = CycleEnum(s.CrtFilter, direction);
                break;

            case "editor_exp":
                s.EditorExperience = s.EditorExperience == EditorExperience.Modern
                    ? EditorExperience.Classic
                    : EditorExperience.Modern;
                break;

            case "auto_indent":
                s.AutoIndent = !s.AutoIndent;
                break;

            case "syntax":
                s.SyntaxColours = !s.SyntaxColours;
                break;

            case "line_hl":
                s.LineHighlight = !s.LineHighlight;
                break;

            case "tooltips":
                s.Tooltips = !s.Tooltips;
                break;

            case "grid":
                s.Grid = !s.Grid;
                break;

            case "master_vol":
                s.MasterVolume = CentauriSettings.ClampPercent(
                    s.MasterVolume + (direction * 10));
                break;

            case "music_vol":
                s.MusicVolume = CentauriSettings.ClampPercent(
                    s.MusicVolume + (direction * 10));
                break;

            case "sfx_vol":
                s.SfxVolume = CentauriSettings.ClampPercent(
                    s.SfxVolume + (direction * 10));
                break;

            case "dev_override":
                ApplyDevOverride(
                    !CareerProgressOverride.Enabled,
                    CareerProgressOverride.IssueNumber,
                    CareerProgressOverride.Stage);
                return;

            case "dev_issue":
            {
                var next = Math.Clamp(
                    CareerProgressOverride.IssueNumber + direction,
                    0,
                    DevMaxIssue());
                ApplyDevOverride(true, next, CareerProgressOverride.Stage);
                return;
            }

            case "dev_stage":
                ApplyDevOverride(
                    true,
                    Math.Max(1, CareerProgressOverride.IssueNumber),
                    CareerProgressOverride.Stage == ProgressionStage.StartOfIssue
                        ? ProgressionStage.IssueCompleted
                        : ProgressionStage.StartOfIssue);
                return;
        }

        _apply(s);
        _settings.Save();
    }

    private void ApplyDevOverride(bool enabled, int issueNumber, ProgressionStage stage)
    {
        if (issueNumber <= 0)
            stage = ProgressionStage.StartOfIssue;

        GameSession.ApplyProgressOverride(enabled, issueNumber, stage);
        ProgressOverrideChanged?.Invoke();

        var keepId = _rows[_selected].Id;
        BuildRows();
        var restored = IndexOfRow(keepId);
        _selected = restored >= 0 ? restored : FirstSelectableIndex();
        EnsureVisible();

        _status = CareerProgressOverride.IsActive
            ? CareerProgressOverride.StatusLabel() + " — SAVE UNCHANGED."
            : "USING SAVED CAREER PROGRESSION.";
    }

    private int IndexOfRow(string id)
    {
        for (var i = 0; i < _rows.Count; i++)
        {
            if (_rows[i].Id == id)
                return i;
        }

        return -1;
    }

    private static T CycleEnum<T>(T value, int direction) where T : struct, Enum
    {
        var values = Enum.GetValues<T>();
        var index = Array.IndexOf(values, value);
        if (index < 0)
            index = 0;

        index = (index + direction + values.Length) % values.Length;
        return values[index];
    }

    private static int GetMaxWindowScale()
    {
        try
        {
            var display = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            var maxW = display.Width / CentauriMachine.DEVELOPMENT_WIDTH;
            var maxH = display.Height / CentauriMachine.DEVELOPMENT_HEIGHT;
            var max = Math.Min(maxW, maxH);
            return Math.Clamp(max, 1, 5);
        }
        catch
        {
            return 4;
        }
    }

    private string FormatValue(string id)
    {
        var s = _settings.Current;

        return id switch
        {
            "window_mode" => s.WindowMode == WindowModeSetting.Fullscreen
                ? "FULLSCREEN"
                : "WINDOWED",
            "window_scale" => s.WindowScale + "X",
            "scaling" => s.ScalingMode == ScalingModeSetting.PixelPerfect
                ? "PIXEL PERFECT"
                : "FIT WINDOW",
            "vsync" => s.VSync ? "ON" : "OFF",
            "crt" => s.CrtFilter switch
            {
                CrtPreset.Subtle => "SUBTLE",
                CrtPreset.NineteenEightySix => "1986",
                _ => "OFF"
            },
            "editor_exp" => s.EditorExperience == EditorExperience.Classic
                ? "CLASSIC"
                : "MODERN",
            "auto_indent" => OnOff(s.AutoIndent),
            "syntax" => OnOff(s.SyntaxColours),
            "line_hl" => OnOff(s.LineHighlight),
            "tooltips" => OnOff(s.Tooltips),
            "grid" => OnOff(s.Grid),
            "master_vol" => s.MasterVolume + "%",
            "music_vol" => s.MusicVolume + "%",
            "sfx_vol" => s.SfxVolume + "%",
            "dev_override" => OnOff(CareerProgressOverride.Enabled),
            "dev_issue" => CareerProgressOverride.IssueNumber <= 0
                ? "0 PRE-PC"
                : CareerProgressOverride.IssueNumber.ToString(),
            "dev_stage" => CareerProgressOverride.IssueNumber <= 0
                ? "N/A"
                : CareerProgressOverride.Stage == ProgressionStage.IssueCompleted
                    ? "COMPLETED"
                    : "START",
            _ => ""
        };
    }

    private static string OnOff(bool value) => value ? "ON" : "OFF";

    private void DrawList(SpriteBatch spriteBatch)
    {
        const int visible = 14;
        var y = 80;
        var end = Math.Min(_scroll + visible, _rows.Count);

        for (var i = _scroll; i < end; i++)
        {
            var row = _rows[i];
            var selected = i == _selected;

            if (row.Kind == RowKind.Header)
            {
                y += 4;
                DrawText(spriteBatch, row.Label, 48, y, Aqua);
                y += 18;
                DrawBox(spriteBatch, new Rectangle(48, y, 544, 1), Aqua);
                y += 10;
                continue;
            }

            if (row.Kind == RowKind.Disabled)
            {
                DrawText(spriteBatch, "  " + row.Label, 48, y, Disabled);
                y += 20;
                continue;
            }

            var marker = selected ? ">" : " ";
            var colour = selected ? Yellow : Cream;

            if (row.Kind == RowKind.Action)
            {
                DrawText(spriteBatch, $"{marker} {row.Label}", 48, y, colour);
            }
            else
            {
                var value = "< " + FormatValue(row.Id) + " >";
                DrawText(spriteBatch, $"{marker} {row.Label}", 48, y, colour);
                DrawText(spriteBatch, value, 360, y, selected ? Yellow : Cream);
            }

            y += 20;
        }

        DrawHelpFooter(spriteBatch);
    }

    private void DrawResetConfirm(SpriteBatch spriteBatch)
    {
        DrawText(spriteBatch, "RESET CAREER", 256, 80, Yellow);
        var lines = new[]
        {
            "THIS WILL RESET:",
            "",
            "* CASH",
            "* CONTRACT PROGRESS",
            "* SUBMISSIONS",
            "* MAIL",
            "* CAREER UNLOCKS",
            "",
            "YOUR PROGRAMS, TAPES AND",
            "CREATED ASSETS WILL NOT",
            "BE DELETED.",
            "",
            "CONTINUE?",
            "",
            "[Y] YES    [N] NO"
        };

        var y = 120;
        foreach (var line in lines)
        {
            DrawText(spriteBatch, line, 160, y, Cream);
            y += 18;
        }
    }

    private void DrawResetType(SpriteBatch spriteBatch)
    {
        DrawText(spriteBatch, "TYPE RESET TO CONFIRM:", 200, 160, Yellow);
        DrawText(spriteBatch, "> " + _resetBuffer + "_", 200, 200, Cream);
        DrawText(spriteBatch, "ENTER CONFIRM    ESC CANCEL", 184, 260, Muted);

        if (!string.IsNullOrWhiteSpace(_status))
            DrawText(spriteBatch, _status, 48, 380, Yellow);
    }

    private void DrawDeleteConfirm(SpriteBatch spriteBatch)
    {
        DrawText(spriteBatch, "DELETE CAREER?", 248, 80, Yellow);
        var name = string.IsNullOrWhiteSpace(_careerLabel) ? "THIS SLOT" : _careerLabel;
        var lines = new[]
        {
            "THIS WILL DELETE CAREER PROGRESS FOR:",
            "",
            name,
            "",
            "YOUR PROGRAMS, TAPES AND",
            "CREATED ASSETS WILL NOT",
            "BE DELETED.",
            "",
            "CONTINUE?",
            "",
            "[Y] YES    [N] NO"
        };

        var y = 120;
        foreach (var line in lines)
        {
            DrawText(spriteBatch, line, 160, y, Cream);
            y += 18;
        }
    }

    private void DrawDeleteType(SpriteBatch spriteBatch)
    {
        DrawText(spriteBatch, "TYPE DELETE TO CONFIRM:", 184, 160, Yellow);
        DrawText(spriteBatch, "> " + _resetBuffer + "_", 200, 200, Cream);
        DrawText(spriteBatch, "ENTER CONFIRM    ESC CANCEL", 184, 260, Muted);

        if (!string.IsNullOrWhiteSpace(_status))
            DrawText(spriteBatch, _status, 48, 380, Yellow);
    }

    private void DrawHelpFooter(SpriteBatch spriteBatch)
    {
        DrawBox(spriteBatch, new Rectangle(16, 400, 608, 48), HeaderBar);
        DrawText(spriteBatch, _status, 32, 412, Cream);
        DrawText(spriteBatch, "UP DOWN  LEFT RIGHT  ENTER  ESC BACK", 32, 432, Muted);
    }

    private void DrawText(SpriteBatch spriteBatch, string text, int x, int y, Color colour)
    {
        _font.Draw(spriteBatch, text, new Vector2(x, y), colour);
    }

    private void DrawBox(SpriteBatch spriteBatch, Rectangle rectangle, Color colour)
    {
        spriteBatch.Draw(_whitePixel, rectangle, colour);
    }

    private bool Pressed(KeyboardState keyboard, Keys key)
    {
        return keyboard.IsKeyDown(key) &&
               !_previousKeyboard.IsKeyDown(key);
    }
}
