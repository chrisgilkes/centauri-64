using System;
using System.Collections.Generic;
using System.Linq;

using Centauri64.Graphics;
using Centauri64.Machine;
using Centauri64.Progression;
using Centauri64.Publishing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Centauri64.Game;

public sealed class MailScreen
{
    private enum View
    {
        List,
        Read
    }

    private readonly BitmapFont _font;
    private readonly Texture2D _whitePixel;
    private readonly CareerService _career = new();

    private static readonly Color Background = new(22, 55, 72);
    private static readonly Color Header = new(36, 72, 110);
    private static readonly Color Cyan = new(91, 214, 205);
    private static readonly Color Cream = new(238, 232, 190);
    private static readonly Color Yellow = new(232, 205, 92);
    private static readonly Color Muted = new(130, 165, 170);
    private static readonly Color Dark = new(14, 28, 38);

    private KeyboardState _previousKeyboard;
    private View _view = View.List;
    private List<MailMessage> _mail = new();
    private int _selected;
    private int _scroll;
    private MailMessage? _current;
    private string _claimNotice = string.Empty;
    private string _balanceNotice = string.Empty;

    public event Action? ExitSelected;

    public MailScreen(BitmapFont font, Texture2D whitePixel)
    {
        _font = font;
        _whitePixel = whitePixel;
    }

    public void Open()
    {
        _previousKeyboard = Keyboard.GetState();
        // Deliver any bedroom-pending responses; does not mark mail read.
        _career.DeliverPendingResponses();
        _view = View.List;
        _selected = 0;
        _scroll = 0;
        _current = null;
        _claimNotice = string.Empty;
        _balanceNotice = string.Empty;
        Refresh();
    }

    public void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();

        if (_view == View.List)
            UpdateList(keyboard);
        else
            UpdateRead(keyboard);

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

        DrawBox(spriteBatch, new Rectangle(16, 16, 608, 48), Header);

        if (_view == View.List)
            DrawList(spriteBatch);
        else
            DrawRead(spriteBatch);

        spriteBatch.End();
    }

    private void Refresh()
    {
        var progress = _career.LoadProgress();
        _mail = progress.Mail
            .OrderByDescending(m => !m.Read)
            .ThenBy(m => m.FromOrganisationId)
            .ToList();

        if (_selected >= _mail.Count)
            _selected = Math.Max(0, _mail.Count - 1);
    }

    private void UpdateList(KeyboardState keyboard)
    {
        if (Pressed(keyboard, Keys.Escape))
        {
            ExitSelected?.Invoke();
            return;
        }

        if (_mail.Count == 0)
            return;

        if (Pressed(keyboard, Keys.Up))
            _selected = Math.Max(0, _selected - 1);

        if (Pressed(keyboard, Keys.Down))
            _selected = Math.Min(_mail.Count - 1, _selected + 1);

        if (Pressed(keyboard, Keys.Enter))
        {
            _current = _mail[_selected];
            _scroll = 0;
            _claimNotice = string.Empty;
            _balanceNotice = string.Empty;

            // Opening the letter is the reveal + claim moment.
            if (_career.OpenMail(
                    _current.Id,
                    out var awarded,
                    out var cashBefore,
                    out var cashAfter))
            {
                if (awarded > 0)
                {
                    _claimNotice =
                        "PAYMENT ENCLOSED: " +
                        PlayerProgress.FormatPounds(awarded);
                    _balanceNotice =
                        "CASH: " +
                        PlayerProgress.FormatPounds(cashBefore) +
                        " -> " +
                        PlayerProgress.FormatPounds(cashAfter);
                }

                Refresh();
                _current = _mail.FirstOrDefault(m => m.Id == _current.Id) ?? _current;
            }

            _view = View.Read;
        }
    }

    private void UpdateRead(KeyboardState keyboard)
    {
        if (Pressed(keyboard, Keys.Escape) ||
            Pressed(keyboard, Keys.Enter))
        {
            _view = View.List;
            _claimNotice = string.Empty;
            _balanceNotice = string.Empty;
            Refresh();
            return;
        }

        if (Pressed(keyboard, Keys.Up))
            _scroll = Math.Max(0, _scroll - 1);

        if (Pressed(keyboard, Keys.Down))
            _scroll++;
    }

    private void DrawList(SpriteBatch spriteBatch)
    {
        DrawText(spriteBatch, "MAIL", 296, 24, Cream);

        var progress = _career.LoadProgress();
        var unread = progress.Mail.Count(m => !m.Read);

        DrawText(
            spriteBatch,
            "CASH " + PlayerProgress.FormatPounds(progress.CashPennies),
            48,
            44,
            Yellow);

        if (unread > 0)
        {
            DrawText(
                spriteBatch,
                unread == 1 ? "NEW! 1 UNREAD" : $"NEW! {unread} UNREAD",
                400,
                44,
                Yellow);
        }

        if (_mail.Count == 0)
        {
            DrawText(spriteBatch, "NO MAIL YET.", 264, 180, Cream);
            DrawText(
                spriteBatch,
                "SUBMIT SOFTWARE FROM MAGAZINES.",
                168,
                212,
                Muted);
            DrawFooter(spriteBatch, "ESC BACK");
            return;
        }

        var y = 80;

        for (var i = 0; i < _mail.Count; i++)
        {
            var mail = _mail[i];
            var org = PublisherCatalog.GetOrganisation(mail.FromOrganisationId);
            var from = org?.Name ?? "MAIL";
            var marker = i == _selected ? ">" : " ";
            var colour = i == _selected
                ? Yellow
                : mail.Read
                    ? Muted
                    : Cream;

            // Inbox must not spoil ACCEPTED / REJECTED / payment.
            if (!mail.Read)
            {
                DrawText(spriteBatch, $"{marker} NEW  {from}", 40, y, colour);
                y += 16;
                DrawText(spriteBatch, $"       {mail.Subject}", 40, y, colour);
            }
            else
            {
                DrawText(spriteBatch, $"{marker}      {from}", 40, y, colour);
                y += 16;
                DrawText(spriteBatch, $"       {mail.Subject}", 40, y, Muted);
            }

            y += 22;

            if (y > 390)
                break;
        }

        DrawFooter(spriteBatch, "UP DOWN    ENTER OPEN    ESC BACK");
    }

    private void DrawRead(SpriteBatch spriteBatch)
    {
        if (_current == null)
            return;

        var org = PublisherCatalog.GetOrganisation(_current.FromOrganisationId);
        DrawText(spriteBatch, org?.Name ?? "MAIL", 48, 24, Cream);
        DrawText(spriteBatch, _current.Subject, 48, 44, Cyan);

        var y = 80;
        var lines = _current.Body.Split('\n');
        var visible = lines.Skip(_scroll).Take(14).ToList();

        foreach (var line in visible)
        {
            DrawText(spriteBatch, line, 48, y, Cream);
            y += 18;
        }

        if (!string.IsNullOrWhiteSpace(_claimNotice))
        {
            DrawText(spriteBatch, _claimNotice, 48, 360, Yellow);
            if (!string.IsNullOrWhiteSpace(_balanceNotice))
                DrawText(spriteBatch, _balanceNotice, 48, 380, Yellow);
        }
        else if (_current.RewardClaimed && _current.RewardPence > 0)
        {
            DrawText(
                spriteBatch,
                "PAYMENT CLAIMED " +
                PlayerProgress.FormatPounds(_current.RewardPence),
                48,
                380,
                Muted);
        }

        DrawFooter(spriteBatch, "UP DOWN SCROLL    ENTER/ESC BACK");
    }

    private void DrawFooter(SpriteBatch spriteBatch, string text)
    {
        DrawBox(spriteBatch, new Rectangle(16, 424, 608, 24), Cyan);
        DrawText(spriteBatch, text, 48, 432, Dark);
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
