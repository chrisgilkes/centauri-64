using System;
using System.Collections.Generic;
using System.Linq;

using Centauri64.Analysis;
using Centauri64.Basic;
using Centauri64.Graphics;
using Centauri64.Machine;
using Centauri64.Progression;
using Centauri64.Publishing;
using Centauri64.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Centauri64.Game;

public sealed partial class MagazinesScreen
{
    private enum View
    {
        Shelf,
        Issue,
        List,
        Detail,
        SelectTape,
        Result,
        Sent
    }

    private readonly BitmapFont _font;
    private readonly Texture2D _whitePixel;
    private readonly BasicMachine _machine;
    private readonly CareerService _career;
    private readonly SoftwareAnalyser _analyser = new();

    private static readonly Color Background = new(22, 55, 72);
    private static readonly Color Header = new(36, 72, 110);
    private static readonly Color Cyan = new(91, 214, 205);
    private static readonly Color Cream = new(238, 232, 190);
    private static readonly Color Yellow = new(232, 205, 92);
    private static readonly Color Muted = new(130, 165, 170);
    private static readonly Color Dark = new(14, 28, 38);

    private KeyboardState _previousKeyboard;
    private View _view = View.List;
    private List<SubmissionContract> _contracts = new();
    private List<string> _tapes = new();
    private int _selected;
    private int _tapeSelected;
    private int _scroll;
    private SubmissionContract? _contract;
    private SubmissionResult? _result;
    private SoftwareAnalysis? _analysis;
    private string _sentMessage = string.Empty;
    private bool _referenceLibrary;
    private MagazineIssue? _issue;
    private int _issueSelected;
    private int _issueScroll;

    public event Action? ExitSelected;

    public MagazinesScreen(
        BitmapFont font,
        Texture2D whitePixel,
        BasicMachine machine,
        CareerService career)
    {
        _font = font;
        _whitePixel = whitePixel;
        _machine = machine;
        _career = career;
    }

    public void Open(bool referenceLibrary = false)
    {
        _referenceLibrary = referenceLibrary;
        _previousKeyboard = Keyboard.GetState();
        _view = View.Shelf;
        _issueSelected = 0;
        _issueScroll = 0;
        _selected = 0;
        _scroll = 0;
        _contract = null;
        _result = null;
        _analysis = null;
        _issue = null;

        if (!_referenceLibrary && GameSession.Career != null)
        {
            MagazineProgression.EnsureStartingIssue(GameSession.Career);
            PersistCareer();
        }
    }

    public void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();

        switch (_view)
        {
            case View.Shelf:
                UpdateShelf(keyboard);
                break;
            case View.Issue:
                UpdateIssue(keyboard);
                break;
            case View.List:
                UpdateList(keyboard);
                break;
            case View.Detail:
                UpdateDetail(keyboard);
                break;
            case View.SelectTape:
                UpdateSelectTape(keyboard);
                break;
            case View.Result:
                UpdateResult(keyboard);
                break;
            case View.Sent:
                UpdateSent(keyboard);
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

        DrawBox(spriteBatch, new Rectangle(16, 16, 608, 48), Header);

        switch (_view)
        {
            case View.Shelf:
                DrawShelf(spriteBatch);
                break;
            case View.Issue:
                DrawIssue(spriteBatch);
                break;
            case View.List:
                DrawList(spriteBatch);
                break;
            case View.Detail:
                DrawDetail(spriteBatch);
                break;
            case View.SelectTape:
                DrawSelectTape(spriteBatch);
                break;
            case View.Result:
                DrawResult(spriteBatch);
                break;
            case View.Sent:
                DrawSent(spriteBatch);
                break;
        }

        spriteBatch.End();
    }

    private void RefreshList()
    {
        var progress = _career.LoadProgress();
        _contracts = _career.GetVisibleContracts(progress).ToList();

        if (_selected >= _contracts.Count)
            _selected = Math.Max(0, _contracts.Count - 1);
    }

    private void UpdateList(KeyboardState keyboard)
    {
        if (Pressed(keyboard, Keys.Escape))
        {
            _view = View.Shelf;
            return;
        }

        if (_contracts.Count == 0)
            return;

        if (Pressed(keyboard, Keys.Up))
            _selected = Math.Max(0, _selected - 1);

        if (Pressed(keyboard, Keys.Down))
            _selected = Math.Min(_contracts.Count - 1, _selected + 1);

        if (Pressed(keyboard, Keys.Enter))
        {
            _contract = _contracts[_selected];
            _view = View.Detail;
            _scroll = 0;
        }
    }

    private void UpdateDetail(KeyboardState keyboard)
    {
        if (Pressed(keyboard, Keys.Escape))
        {
            _view = View.List;
            RefreshList();
            return;
        }

        if (_contract == null)
            return;

        var availability = _career.GetAvailability(_contract);

        if (Pressed(keyboard, Keys.S) &&
            availability == ContractAvailability.Available)
        {
            _tapes = _machine.GetTapeNames().ToList();
            _tapeSelected = 0;
            _view = View.SelectTape;
        }
    }

    private void UpdateSelectTape(KeyboardState keyboard)
    {
        if (Pressed(keyboard, Keys.Escape))
        {
            _view = View.Detail;
            return;
        }

        if (_tapes.Count == 0)
            return;

        if (Pressed(keyboard, Keys.Up))
            _tapeSelected = Math.Max(0, _tapeSelected - 1);

        if (Pressed(keyboard, Keys.Down))
            _tapeSelected = Math.Min(_tapes.Count - 1, _tapeSelected + 1);

        if (Pressed(keyboard, Keys.Enter) && _contract != null)
        {
            var tape = _tapes[_tapeSelected];
            _analysis = _analyser.AnalyseTape(tape);
            var label = _machine.GetTapeLabel(tape);
            _result = _career.Evaluate(_contract, _analysis, label);
            _view = View.Result;
            _scroll = 0;
        }
    }

    private void UpdateResult(KeyboardState keyboard)
    {
        if (Pressed(keyboard, Keys.Escape) || Pressed(keyboard, Keys.N))
        {
            _view = View.Detail;
            return;
        }

        if (Pressed(keyboard, Keys.Up))
            _scroll = Math.Max(0, _scroll - 1);

        if (Pressed(keyboard, Keys.Down))
            _scroll++;

        if (_contract == null || _result == null || _analysis == null)
            return;

        if (_result.AlreadyCompleted)
            return;

        if (_result.Accepted &&
            (Pressed(keyboard, Keys.Y) || Pressed(keyboard, Keys.Enter)))
        {
            var tape = _tapes[_tapeSelected];
            var label = _machine.GetTapeLabel(tape);

            if (_career.TrySubmit(_contract, tape, label, _analysis, out _))
            {
                var org = PublisherCatalog.GetOrganisation(_contract.OrganisationId);
                _sentMessage = org?.SubmissionReceivedText ??
                               "YOUR TAPE HAS BEEN SENT.\nGOOD LUCK!";
                _view = View.Sent;
            }
        }
    }

    private void UpdateSent(KeyboardState keyboard)
    {
        if (Pressed(keyboard, Keys.Escape) ||
            Pressed(keyboard, Keys.Enter) ||
            Pressed(keyboard, Keys.Space))
        {
            ExitSelected?.Invoke();
        }
    }

    private void DrawList(SpriteBatch spriteBatch)
    {
        DrawText(spriteBatch, "CLASSIFIEDS — SOFTWARE WANTED", 160, 24, Cream);

        var progress = _career.LoadProgress();
        var cash = "CASH " + PlayerProgress.FormatPounds(progress.CashPennies);
        DrawText(spriteBatch, cash, 48, 44, Yellow);

        if (_contracts.Count == 0)
        {
            DrawText(spriteBatch, "NO OPPORTUNITIES YET.", 216, 180, Cream);
            DrawText(spriteBatch, "CHECK BACK AFTER YOUR FIRST PROGRAM.", 160, 212, Muted);
            DrawFooter(spriteBatch, "ESC BACK");
            return;
        }

        var y = 80;
        string? lastOrg = null;

        for (var i = 0; i < _contracts.Count; i++)
        {
            var contract = _contracts[i];
            var org = PublisherCatalog.GetOrganisation(contract.OrganisationId);
            var orgName = org?.Name ?? contract.OrganisationId;

            if (orgName != lastOrg)
            {
                y += 8;
                DrawText(spriteBatch, orgName, 48, y, Cyan);
                y += 20;
                lastOrg = orgName;
            }

            var availability = _career.GetAvailability(contract, progress);
            var marker = i == _selected ? ">" : " ";
            var status = availability switch
            {
                ContractAvailability.Completed => " [DONE]",
                ContractAvailability.Pending => " [SENT]",
                ContractAvailability.ResponseReady => " [MAIL]",
                _ => ""
            };

            var colour = i == _selected
                ? Yellow
                : availability == ContractAvailability.Completed
                    ? Muted
                    : Cream;

            DrawText(
                spriteBatch,
                $"{marker} {contract.Title}{status}",
                56,
                y,
                colour);
            y += 18;

            if (y > 390)
                break;
        }

        DrawFooter(spriteBatch, "UP DOWN SELECT    ENTER OPEN    ESC BACK");
    }

    private void DrawDetail(SpriteBatch spriteBatch)
    {
        if (_contract == null)
            return;

        var org = PublisherCatalog.GetOrganisation(_contract.OrganisationId);
        var progress = _career.LoadProgress();
        var availability = _career.GetAvailability(_contract, progress);
        var submission = _career.GetLatestSubmission(_contract.Id, progress);

        DrawText(spriteBatch, org?.Name ?? "ORGANISATION", 48, 24, Cream);
        DrawText(spriteBatch, _contract.Subtitle, 48, 44, Cyan);

        var y = 80;
        DrawText(spriteBatch, _contract.Title, 48, y, Yellow);
        y += 28;

        if (availability is ContractAvailability.Pending
            or ContractAvailability.ResponseReady)
        {
            DrawText(spriteBatch, "STATUS:", 48, y, Cyan);
            y += 20;
            DrawText(spriteBatch, "TAPE SUBMITTED", 48, y, Cream);
            y += 20;

            var tapeLabel = submission?.TapeName ?? "?";
            DrawText(spriteBatch, "\"" + tapeLabel + "\"", 48, y, Yellow);
            y += 28;

            if (availability == ContractAvailability.Pending)
            {
                DrawText(spriteBatch, "AWAITING RESPONSE...", 48, y, Muted);
            }
            else
            {
                DrawText(spriteBatch, "CHECK THE NOTICE BOARD", 48, y, Yellow);
                y += 20;
                DrawText(spriteBatch, "FOR YOUR MAIL.", 48, y, Yellow);
            }

            DrawFooter(spriteBatch, "ESC BACK");
            return;
        }

        if (availability == ContractAvailability.Completed)
        {
            DrawText(spriteBatch, "STATUS: COMPLETED", 48, y, Muted);
            y += 24;
            if (submission != null)
            {
                DrawText(
                    spriteBatch,
                    "SUBMITTED: \"" + submission.TapeName + "\"",
                    48,
                    y,
                    Cream);
            }

            DrawFooter(spriteBatch, "ESC BACK");
            return;
        }

        // AVAILABLE — opportunity only (never acceptance/rejection text).
        foreach (var line in _contract.AdvertText.Split('\n'))
        {
            DrawText(spriteBatch, line, 48, y, Cream);
            y += 16;
        }

        y += 12;
        DrawText(spriteBatch, "REQUIREMENTS", 48, y, Cyan);
        y += 20;

        foreach (var line in CareerService.DescribeRequirements(_contract.Requirements)
                     .Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            DrawText(spriteBatch, line, 48, y, Cream);
            y += 16;
        }

        y += 12;
        DrawText(
            spriteBatch,
            "PAYMENT: " + PlayerProgress.FormatPounds(_contract.RewardPennies),
            48,
            y,
            Yellow);

        DrawFooter(spriteBatch, "S SUBMIT SOFTWARE    ESC BACK");
    }

    private void DrawSelectTape(SpriteBatch spriteBatch)
    {
        DrawText(spriteBatch, "SELECT SOFTWARE TO SUBMIT", 184, 24, Cream);

        if (_tapes.Count == 0)
        {
            DrawText(spriteBatch, "NO TAPES SAVED YET.", 232, 180, Cream);
            DrawText(spriteBatch, "SAVE A PROGRAM FIRST.", 224, 212, Muted);
            DrawFooter(spriteBatch, "ESC BACK");
            return;
        }

        var y = 88;

        for (var i = 0; i < _tapes.Count; i++)
        {
            var marker = i == _tapeSelected ? ">" : " ";
            var colour = i == _tapeSelected ? Yellow : Cream;
            DrawText(spriteBatch, $"{marker} {_tapes[i]}", 48, y, colour);
            y += 18;

            if (y > 390)
                break;
        }

        DrawFooter(spriteBatch, "UP DOWN    ENTER SELECT    ESC CANCEL");
    }

    private void DrawResult(SpriteBatch spriteBatch)
    {
        if (_contract == null || _result == null)
            return;

        var org = PublisherCatalog.GetOrganisation(_contract.OrganisationId);
        var tape = _tapes.Count > 0 ? _tapes[_tapeSelected] : "";

        DrawText(spriteBatch, tape, 48, 24, Cream);
        DrawText(spriteBatch, org?.Name ?? "", 48, 44, Cyan);

        var y = 80;
        DrawText(spriteBatch, "CHECKING \"" + tape + "\"...", 48, y, Yellow);
        y += 24;

        var checks = _result.Checks.Skip(_scroll).Take(12).ToList();

        foreach (var check in checks)
        {
            var mark = check.Passed ? "+" : "X";
            var colour = check.Passed ? Cream : Yellow;
            DrawText(spriteBatch, $"{mark} {check.Label}", 48, y, colour);
            y += 16;

            if (!string.IsNullOrWhiteSpace(check.Detail))
            {
                foreach (var detail in check.Detail.Split('\n'))
                {
                    DrawText(spriteBatch, "  " + detail, 48, y, Muted);
                    y += 14;
                }
            }
        }

        y += 8;
        DrawText(
            spriteBatch,
            "PAYMENT: " + PlayerProgress.FormatPounds(_contract.RewardPennies),
            48,
            y,
            Yellow);
        y += 24;

        if (_result.AlreadyCompleted)
        {
            var pending = _career.GetAvailability(_contract) is
                ContractAvailability.Pending or ContractAvailability.ResponseReady;
            DrawText(
                spriteBatch,
                pending ? "TAPE ALREADY SENT" : "ALREADY COMPLETED",
                48,
                y,
                Yellow);
            DrawFooter(spriteBatch, "ESC BACK");
            return;
        }

        if (_result.Accepted)
        {
            DrawText(spriteBatch, "READY TO SUBMIT", 48, y, Cyan);
            DrawFooter(spriteBatch, "Y SEND TAPE    N CANCEL");
        }
        else
        {
            DrawText(spriteBatch, "NOT READY TO SUBMIT", 48, y, Yellow);
            y += 20;

            var failed = _result.FailedChecks.FirstOrDefault();
            if (failed != null)
                DrawText(spriteBatch, HintFor(failed), 48, y, Muted);

            DrawFooter(spriteBatch, "ESC BACK");
        }
    }

    private void DrawSent(SpriteBatch spriteBatch)
    {
        DrawText(spriteBatch, "TAPE SENT", 280, 24, Cream);

        var y = 120;

        foreach (var line in _sentMessage.Split('\n'))
        {
            DrawText(spriteBatch, line, 160, y, Cream);
            y += 20;
        }

        DrawText(spriteBatch, "CHECK THE NOTICE BOARD FOR MAIL.", 160, 320, Yellow);
        DrawFooter(spriteBatch, "ENTER / ESC RETURN TO BEDROOM");
    }

    private static string HintFor(SubmissionCheck check)
    {
        if (check.Label.Contains("COVER", StringComparison.OrdinalIgnoreCase))
            return "CREATE A COVER FOR YOUR CASSETTE AND TRY AGAIN.";

        if (check.Label.Contains("INPUT", StringComparison.OrdinalIgnoreCase))
            return "TRY USING KEY, KEYPRESSED OR INPUT.";

        if (check.Label.Contains("NETWORK", StringComparison.OrdinalIgnoreCase))
            return "SEE NETWORK BASIC IN THE PROGRAMMING MANUAL.";

        if (check.Label.Contains("MAXIMUM", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(check.Detail))
        {
            return check.Detail.Replace('\n', ' ');
        }

        return "FIX THE FAILED REQUIREMENT AND TRY AGAIN.";
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
