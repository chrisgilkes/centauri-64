using System;
using System.Linq;

using Centauri64.Progression;
using Centauri64.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Centauri64.Game;

public sealed partial class MagazinesScreen
{
    private const int CoverColumns = 5;
    private const int CoverWidth = 216;
    private const int CoverHeight = 300;
    private const int CoverGapX = 22;
    private const int CoverGapY = 62;
    private const int CoverOriginX = 48;
    private const int CoverOriginY = 152;

    public event Action? CareerChanged;

    private void PersistCareer()
    {
        CareerChanged?.Invoke();
    }

    private Rectangle CoverBounds(int index)
    {
        var col = index % CoverColumns;
        var row = index / CoverColumns;
        var x = CoverOriginX + col * (CoverWidth + CoverGapX);
        var y = CoverOriginY + row * (CoverHeight + CoverGapY);
        return new Rectangle(x, y, CoverWidth, CoverHeight);
    }

    private void UpdateShelf(KeyboardState keyboard, MouseState mouse)
    {
        if (Pressed(keyboard, Keys.Escape))
        {
            ExitSelected?.Invoke();
            return;
        }

        var issues = MagazineCatalog.Issues;
        var columns = CoverColumns;

        if (Pressed(keyboard, Keys.Left))
            _issueSelected = Math.Max(0, _issueSelected - 1);

        if (Pressed(keyboard, Keys.Right))
            _issueSelected = Math.Min(issues.Length - 1, _issueSelected + 1);

        if (Pressed(keyboard, Keys.Up))
            _issueSelected = Math.Max(0, _issueSelected - columns);

        if (Pressed(keyboard, Keys.Down))
            _issueSelected = Math.Min(issues.Length - 1, _issueSelected + columns);

        HandleShelfMouse(mouse, issues.Length);

        if (Pressed(keyboard, Keys.Enter) || Pressed(keyboard, Keys.Space))
            OpenSelectedIssue();

        if (!_referenceLibrary && Pressed(keyboard, Keys.C))
        {
            RefreshList();
            _view = View.List;
        }
    }

    private void HandleShelfMouse(MouseState mouse, int issueCount)
    {
        for (var i = 0; i < issueCount; i++)
        {
            if (!CoverBounds(i).Contains(mouse.X, mouse.Y))
                continue;

            _issueSelected = i;
            var clicked = mouse.LeftButton == ButtonState.Pressed &&
                          _previousMouse.LeftButton == ButtonState.Released;
            if (clicked)
                OpenSelectedIssue();
            return;
        }
    }

    private void OpenSelectedIssue()
    {
        _issue = MagazineCatalog.Issues[_issueSelected];
        _purchaseNotice = string.Empty;
        _view = View.Issue;
    }

    private void UpdateIssue(KeyboardState keyboard, MouseState mouse)
    {
        if (Pressed(keyboard, Keys.Escape))
        {
            _view = View.Shelf;
            _issue = null;
            _purchaseNotice = string.Empty;
            return;
        }

        if (_referenceLibrary || _issue == null)
            return;

        if (CurrentState(_issue) != MagazineIssueState.OnSale)
            return;

        if (!Pressed(keyboard, Keys.Y) && !Pressed(keyboard, Keys.Enter))
            return;

        TryBuySelected();
    }

    private void TryBuySelected()
    {
        if (_issue == null)
            return;

        if (CareerProgressOverride.IsActive)
        {
            _purchaseNotice = "DEV OVERRIDE ACTIVE — PURCHASE BLOCKED";
            return;
        }

        var result = _career.TryPurchaseIssue(_issue.Id);
        _purchaseNotice = result switch
        {
            MagazinePurchaseResult.Purchased => string.Empty,
            MagazinePurchaseResult.CannotAfford => "NOT ENOUGH MONEY",
            MagazinePurchaseResult.AlreadyOwned => string.Empty,
            _ => "NOT ON SALE"
        };

        if (result == MagazinePurchaseResult.Purchased)
            PersistCareer();
    }

    private MagazineIssueState CurrentState(MagazineIssue issue) =>
        MagazineProgression.StateOf(GameSession.EffectiveCareer, issue, _referenceLibrary);

    private void DrawShelf(SpriteBatch spriteBatch)
    {
        var kicker = _referenceLibrary
            ? "REFERENCE LIBRARY"
            : "YOUR CENTAURI64 MAGAZINES";
        PrintTheme.DrawMasthead(
            spriteBatch,
            _whitePixel,
            _print,
            kicker,
            "YEAR ONE",
            MagazineProgression.EraLabel(GameSession.EffectiveCareer, _referenceLibrary));

        var issues = MagazineCatalog.Issues;
        DrawSelectedIssueStrip(spriteBatch, issues[_issueSelected]);

        for (var i = 0; i < issues.Length; i++)
            DrawShelfCover(spriteBatch, issues[i], CoverBounds(i), i == _issueSelected);

        var hint = _referenceLibrary
            ? "ARROWS    ENTER OPEN    ESC BACK"
            : "ARROWS    ENTER OPEN    C CLASSIFIEDS    ESC BACK";
        PrintTheme.Footer(spriteBatch, _whitePixel, _print, hint);
    }

    private void DrawSelectedIssueStrip(SpriteBatch spriteBatch, MagazineIssue selected)
    {
        var month = string.IsNullOrEmpty(selected.FictionalMonth)
            ? string.Empty
            : selected.FictionalMonth;
        var strip = string.IsNullOrEmpty(month)
            ? "ISSUE " + selected.IssueNumber + "     " + selected.CoverHeadline
            : "ISSUE " + selected.IssueNumber + "     " + month + "     " + selected.CoverHeadline;

        _print.Draw(spriteBatch, _print.Caption, strip, 48, 114, PrintTheme.InkMuted);
        PrintTheme.RuleH(spriteBatch, _whitePixel, 48, 136, MetaUi.Width - 96);
    }

    private void DrawShelfCover(
        SpriteBatch spriteBatch,
        MagazineIssue issue,
        Rectangle bounds,
        bool selected)
    {
        var state = CurrentState(issue);
        DrawCover(spriteBatch, issue, bounds, CoverTint(state));

        if (selected)
            DrawSelectionMarker(spriteBatch, bounds);

        var label = FictionLabel(issue, state);
        var labelColour = CaptionColour(state, label);
        _print.Draw(spriteBatch, _print.Caption, "ISSUE " + issue.IssueNumber, bounds.X, bounds.Bottom + 8, PrintTheme.Ink);
        _print.Draw(spriteBatch, _print.Caption, label, bounds.X, bounds.Bottom + 28, labelColour);
    }

    private void DrawSelectionMarker(SpriteBatch spriteBatch, Rectangle bounds)
    {
        var outer = bounds;
        outer.Inflate(5, 5);
        var inner = bounds;
        inner.Inflate(2, 2);
        PrintTheme.Frame(spriteBatch, _whitePixel, outer, PrintTheme.Masthead, 2);
        PrintTheme.Frame(spriteBatch, _whitePixel, inner, PrintTheme.Masthead, 1);
    }

    private static Color CoverTint(MagazineIssueState state)
    {
        // Modest only — future covers must stay colourful and readable.
        if (state == MagazineIssueState.ComingLater)
            return new Color(228, 222, 212);
        if (state == MagazineIssueState.ComingNext)
            return new Color(246, 244, 238);
        return Color.White;
    }

    private static Color CaptionColour(MagazineIssueState state, string label)
    {
        if (state == MagazineIssueState.OnSale)
            return PrintTheme.Masthead;
        if (state == MagazineIssueState.ComingNext || label == "NEXT MONTH")
            return PrintTheme.Masthead;
        if (label == "CURRENT ISSUE")
            return PrintTheme.SpotBlue;
        return PrintTheme.InkMuted;
    }

    private string FictionLabel(MagazineIssue issue, MagazineIssueState state)
    {
        if (state == MagazineIssueState.OnSale)
            return issue.PricePennies > 0
                ? "ON SALE - " + PlayerProgress.FormatPounds(issue.PricePennies)
                : "ON SALE";
        if (state == MagazineIssueState.ComingNext)
            return "NEXT MONTH";
        if (state == MagazineIssueState.ComingLater)
            return "NOT YET ON SALE";

        if (_referenceLibrary)
            return "ON THE SHELF";

        // CURRENT ISSUE = fictional career month, not the selected cover.
        var currentMonth = MagazineProgression.CalendarIssueNumber(GameSession.EffectiveCareer, false);
        return issue.IssueNumber == currentMonth ? "CURRENT ISSUE" : "ON YOUR SHELF";
    }

    private void DrawCover(
        SpriteBatch spriteBatch,
        MagazineIssue issue,
        Rectangle bounds,
        Color tint)
    {
        var texture = _covers.Get(issue);
        if (texture != null)
        {
            spriteBatch.End();
            spriteBatch.Begin(
                samplerState: SamplerState.LinearClamp,
                transformMatrix: _uiTransform);
            spriteBatch.Draw(texture, bounds, tint);
            spriteBatch.End();
            spriteBatch.Begin(
                samplerState: SamplerState.PointClamp,
                transformMatrix: _uiTransform);
            PrintTheme.Frame(spriteBatch, _whitePixel, bounds, PrintTheme.Rule, 2);
            return;
        }

        PrintTheme.Box(spriteBatch, _whitePixel, bounds, PrintTheme.PaperDark);
        PrintTheme.Frame(spriteBatch, _whitePixel, bounds, PrintTheme.Masthead, 2);
        PrintTheme.Box(
            spriteBatch,
            _whitePixel,
            new Rectangle(bounds.X, bounds.Y, bounds.Width, 28),
            PrintTheme.Masthead);
        _print.Draw(spriteBatch, _print.Caption, "CENTAURI64", bounds.X + 12, bounds.Y + 6, PrintTheme.Paper);
        _print.Draw(
            spriteBatch,
            _print.Body,
            "ISSUE " + issue.IssueNumber.ToString("00"),
            bounds.X + 12,
            bounds.Y + 44,
            PrintTheme.Ink);

        var y = bounds.Y + 80;
        foreach (var line in PrintFonts.Wrap(_print.Title, issue.CoverHeadline, bounds.Width - 24).Take(4))
        {
            _print.Draw(spriteBatch, _print.Title, line, bounds.X + 12, y, PrintTheme.Ink);
            y += 34;
        }

        if (!string.IsNullOrEmpty(issue.FictionalMonth))
        {
            _print.Draw(
                spriteBatch,
                _print.Caption,
                issue.FictionalMonth,
                bounds.X + 12,
                bounds.Bottom - 28,
                PrintTheme.InkMuted);
        }
    }

    /// <summary>
    /// Temporary contents/teaser page. Later replaced by real magazine pages
    /// using the same cover asset, print type, and meta UI transform.
    /// </summary>
    private void DrawIssue(SpriteBatch spriteBatch)
    {
        if (_issue == null)
            return;

        var state = CurrentState(_issue);
        PrintTheme.DrawMasthead(
            spriteBatch,
            _whitePixel,
            _print,
            "CENTAURI64 MAGAZINE",
            "ISSUE #" + _issue.IssueNumber,
            _issue.FictionalMonth);

        var cover = new Rectangle(48, 120, 420, 592);
        DrawCover(spriteBatch, _issue, cover, CoverTint(state));

        var textX = 500;
        var textWidth = MetaUi.Width - textX - 48;
        var layout = new PrintLayout(spriteBatch, _whitePixel, _print, textX, 120, textWidth, 860);
        layout.Heading(_issue.CoverHeadline);
        layout.Section(FictionLabel(_issue, state), PrintTheme.Masthead);
        layout.Space(8);

        if (state == MagazineIssueState.Owned)
            DrawOwnedIssue(layout);
        else if (state == MagazineIssueState.OnSale)
            DrawOnSaleIssue(layout);
        else
            DrawFutureIssue(layout, state);

        var footer = state == MagazineIssueState.OnSale && !_referenceLibrary
            ? "Y BUY THIS ISSUE    ESC BACK TO SHELF"
            : "ESC BACK TO SHELF";
        PrintTheme.Footer(spriteBatch, _whitePixel, _print, footer);
    }

    private void DrawOnSaleIssue(PrintLayout layout)
    {
        var cash = GameSession.Career?.Progress.CashPennies ?? 0;
        var price = Math.Max(0, _issue!.PricePennies);

        layout.Paragraph(_issue.Teaser, PrintTheme.Ink, maxLines: 5);
        layout.Space(8);
        layout.Section("COVER TAPE", PrintTheme.Masthead);
        layout.Heading(_issue.CoverGameTitle);
        layout.Space(8);
        layout.Section("NOW ON SALE", PrintTheme.Masthead);
        layout.BodyLine("PRICE ........ " + PlayerProgress.FormatPounds(price));
        layout.BodyLine("YOU HAVE ..... " + PlayerProgress.FormatPounds(cash));
        layout.Space(8);

        if (cash < price)
            layout.BodyLine("NOT ENOUGH MONEY", PrintTheme.Stamp);
        else
            layout.BodyLine("Y  BUY THIS ISSUE", PrintTheme.Ink);

        if (!string.IsNullOrEmpty(_purchaseNotice))
            layout.BodyLine(_purchaseNotice, PrintTheme.Stamp);
    }

    private void DrawOwnedIssue(PrintLayout layout)
    {
        layout.Section("CONTENTS", PrintTheme.SpotBlue);
        layout.Paragraph(_issue!.FullDescription, PrintTheme.Ink, maxLines: 8);
        layout.Space(8);
        layout.Section("COVER TAPE", PrintTheme.Masthead);
        layout.Heading(_issue.CoverGameTitle);
        layout.Paragraph(_issue.CoverGameDescription, PrintTheme.InkMuted, maxLines: 4);
        layout.Space(12);
        layout.CaptionLine("Pages of this issue will appear here in a later printing.");
    }

    private void DrawFutureIssue(PrintLayout layout, MagazineIssueState state)
    {
        layout.Section(
            state == MagazineIssueState.ComingNext ? "COMING SOON" : "NOT YET ON SALE",
            PrintTheme.Stamp);
        layout.Paragraph(_issue!.Teaser, PrintTheme.Ink, maxLines: 5);
        layout.Space(8);
        layout.Section("COVER TAPE", PrintTheme.Masthead);
        layout.Heading(_issue.CoverGameTitle);
        layout.CaptionLine("NOT YET ON SALE");
        layout.CaptionLine("Keep programming. New issues go on sale with your career.");
    }
}
