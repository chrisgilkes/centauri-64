using System;
using System.Collections.Generic;
using System.Linq;

using Centauri64.Analysis;
using Centauri64.Basic;
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

    private readonly PrintFonts _print;
    private readonly Texture2D _whitePixel;
    private readonly BasicMachine _machine;
    private readonly CareerService _career;
    private readonly MagazineCoverStore _covers;
    private readonly SoftwareAnalyser _analyser = new();
    private Matrix _uiTransform = Matrix.Identity;

    private KeyboardState _previousKeyboard;
    private MouseState _previousMouse;
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
    private string _purchaseNotice = string.Empty;

    public event Action? ExitSelected;

    public MagazinesScreen(
        Texture2D whitePixel,
        BasicMachine machine,
        CareerService career,
        GraphicsDevice graphicsDevice,
        PrintFonts print)
    {
        _whitePixel = whitePixel;
        _machine = machine;
        _career = career;
        _covers = new MagazineCoverStore(graphicsDevice);
        _print = print;
    }

    public void Open(bool referenceLibrary = false)
    {
        _referenceLibrary = referenceLibrary;
        _previousKeyboard = Keyboard.GetState();
        _previousMouse = Mouse.GetState();
        _view = View.Shelf;
        _issueSelected = 0;
        _selected = 0;
        _scroll = 0;
        _contract = null;
        _result = null;
        _analysis = null;
        _issue = null;
        _purchaseNotice = string.Empty;

        if (!_referenceLibrary && GameSession.Career != null)
        {
            MagazineProgression.EnsureStartingIssue(GameSession.Career);
            PersistCareer();
        }
    }

    public void Update(GameTime gameTime, MouseState mouse)
    {
        var keyboard = Keyboard.GetState();

        switch (_view)
        {
            case View.Shelf:
                UpdateShelf(keyboard, mouse);
                break;
            case View.Issue:
                UpdateIssue(keyboard, mouse);
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
        _previousMouse = mouse;
    }

    public void Draw(SpriteBatch spriteBatch, Matrix transform)
    {
        _uiTransform = transform;
        spriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            transformMatrix: transform);
        PrintTheme.FillPaper(spriteBatch, _whitePixel);

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
        var visible = _career.GetVisibleContracts(progress);
        var challenges = visible.Where(IsReaderChallenge).ToList();
        var wanted = visible.Where(c => !IsReaderChallenge(c)).ToList();
        _contracts = challenges.Concat(wanted).ToList();

        if (_selected >= _contracts.Count)
            _selected = Math.Max(0, _contracts.Count - 1);
    }

    private static bool IsReaderChallenge(SubmissionContract contract)
    {
        var org = PublisherCatalog.GetOrganisation(contract.OrganisationId);
        return org?.Type == OrganisationType.Magazine;
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

        if (Pressed(keyboard, Keys.Up) || Pressed(keyboard, Keys.Left))
            _selected = Math.Max(0, _selected - 1);

        if (Pressed(keyboard, Keys.Down) || Pressed(keyboard, Keys.Right))
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
        var era = MagazineProgression.EraLabel(GameSession.Career, _referenceLibrary);
        var progress = _career.LoadProgress();
        var cash = PlayerProgress.FormatPounds(progress.CashPennies);
        PrintTheme.DrawMasthead(
            spriteBatch,
            _whitePixel,
            _print,
            "CENTAURI64 CLASSIFIEDS",
            era,
            cash);

        if (_contracts.Count == 0)
        {
            DrawText(spriteBatch, "NO SMALL ADS THIS MONTH.", 400, 360, PrintTheme.Ink);
            DrawText(
                spriteBatch,
                "THE SOFTWARE MARKET IS STILL WAKING UP.",
                320,
                400,
                PrintTheme.InkMuted);
            PrintTheme.Footer(spriteBatch, _whitePixel, _print, "ESC BACK TO SHELF");
            return;
        }

        const int cardH = 150;
        var visibleAds = 4;
        if (_selected < _scroll)
            _scroll = _selected;
        if (_selected >= _scroll + visibleAds)
            _scroll = _selected - visibleAds + 1;

        var y = 120;
        var drewChallengeHead = false;
        var drewWantedHead = false;

        for (var i = 0; i < _contracts.Count; i++)
        {
            if (i < _scroll)
                continue;
            if (y + cardH > 880)
                break;

            var contract = _contracts[i];
            var challenge = IsReaderChallenge(contract);
            if (challenge && !drewChallengeHead)
            {
                DrawText(spriteBatch, "READER CHALLENGES", 48, y, PrintTheme.Masthead);
                PrintTheme.RuleH(spriteBatch, _whitePixel, 48, y + 28, 360);
                y += 40;
                drewChallengeHead = true;
            }
            else if (!challenge && !drewWantedHead)
            {
                if (drewChallengeHead)
                    y += 12;
                DrawText(spriteBatch, "SOFTWARE WANTED", 48, y, PrintTheme.SpotBlue);
                PrintTheme.RuleH(spriteBatch, _whitePixel, 48, y + 28, 360);
                y += 40;
                drewWantedHead = true;
            }

            var bounds = new Rectangle(48, y, 1184, cardH);
            DrawAdvert(spriteBatch, contract, bounds, i == _selected, progress);
            y += cardH + 12;
        }

        PrintTheme.Footer(
            spriteBatch,
            _whitePixel,
            _print,
            "UP DOWN    ENTER OPEN ADVERT    ESC BACK TO SHELF");
    }

    private void DrawAdvert(
        SpriteBatch spriteBatch,
        SubmissionContract contract,
        Rectangle bounds,
        bool selected,
        PlayerProgress progress)
    {
        var org = PublisherCatalog.GetOrganisation(contract.OrganisationId);
        var accent = org == null
            ? PrintTheme.Rule
            : CentauriPalette.Get(org.PrintAccentIndex);
        var availability = _career.GetAvailability(contract, progress);
        var fill = selected ? PrintTheme.Highlight : PrintTheme.Paper;

        PrintTheme.Box(spriteBatch, _whitePixel, bounds, fill);
        var thickness = org?.AdvertStyle == PrintAdvertStyle.Bold ? 3 : 1;
        PrintTheme.Frame(spriteBatch, _whitePixel, bounds, accent, thickness);

        if (org?.AdvertStyle == PrintAdvertStyle.Ornate)
            PrintTheme.Frame(
                spriteBatch,
                _whitePixel,
                new Rectangle(bounds.X + 3, bounds.Y + 3, bounds.Width - 6, bounds.Height - 6),
                accent);

        if (org?.AdvertStyle == PrintAdvertStyle.Technical)
            PrintTheme.Box(
                spriteBatch,
                _whitePixel,
                new Rectangle(bounds.X, bounds.Y, 6, bounds.Height),
                accent);

        var textX = bounds.X + 20;
        DrawText(spriteBatch, org?.Name ?? contract.OrganisationId, textX, bounds.Y + 12, accent);
        DrawText(spriteBatch, contract.Title, textX, bounds.Y + 40, PrintTheme.Ink);

        var copyY = bounds.Y + 72;
        foreach (var line in PrintFonts.Wrap(_print.Caption, contract.AdvertText.Replace('\n', ' '), bounds.Width - 40).Take(2))
        {
            _print.Draw(spriteBatch, _print.Caption, line, textX, copyY, PrintTheme.InkMuted);
            copyY += 22;
        }

        DrawText(
            spriteBatch,
            "PAYMENT: " + PlayerProgress.FormatPounds(contract.RewardPennies),
            textX,
            bounds.Bottom - 32,
            PrintTheme.Ink);

        var stamp = FictionalStamp(availability);
        if (stamp != null)
            PrintTheme.DrawStamp(spriteBatch, _whitePixel, _print, bounds, stamp);
    }

    private static string? FictionalStamp(ContractAvailability availability) =>
        availability switch
        {
            ContractAvailability.Completed => "FILLED",
            ContractAvailability.Pending => "UNDER REVIEW",
            ContractAvailability.ResponseReady => "SUBMITTED",
            _ => null
        };

    private void DrawDetail(SpriteBatch spriteBatch)
    {
        if (_contract == null)
            return;

        var org = PublisherCatalog.GetOrganisation(_contract.OrganisationId);
        var progress = _career.LoadProgress();
        var availability = _career.GetAvailability(_contract, progress);
        var submission = _career.GetLatestSubmission(_contract.Id, progress);
        var kicker = IsReaderChallenge(_contract)
            ? "READER CHALLENGE"
            : "CLASSIFIED ADVERT";

        PrintTheme.DrawMasthead(
            spriteBatch,
            _whitePixel,
            _print,
            kicker,
            org?.Name ?? "ORGANISATION",
            MagazineProgression.EraLabel(GameSession.Career, _referenceLibrary));

        var y = 120;
        DrawText(spriteBatch, _contract.Title, 28, y, PrintTheme.Ink);
        y += 20;
        PrintTheme.RuleH(spriteBatch, _whitePixel, 28, y, 584);
        y += 12;

        if (availability is ContractAvailability.Pending
            or ContractAvailability.ResponseReady)
        {
            DrawText(spriteBatch, FictionalStamp(availability) ?? "SUBMITTED", 28, y, PrintTheme.Stamp);
            y += 20;
            var tapeLabel = submission?.TapeName ?? "?";
            DrawText(spriteBatch, "TAPE ENCLOSED: \"" + tapeLabel + "\"", 28, y, PrintTheme.Ink);
            y += 24;
            DrawText(
                spriteBatch,
                availability == ContractAvailability.Pending
                    ? "THE EDITORS HAVE YOUR CASSETTE. A REPLY WILL FOLLOW."
                    : "A LETTER IS WAITING ON THE NOTICE BOARD.",
                28,
                y,
                PrintTheme.InkMuted);
            PrintTheme.Footer(spriteBatch, _whitePixel, _print, "ESC BACK TO CLASSIFIEDS");
            return;
        }

        if (availability == ContractAvailability.Completed)
        {
            DrawText(spriteBatch, "CONTRACT AWARDED", 28, y, PrintTheme.Stamp);
            y += 20;
            if (submission != null)
            {
                DrawText(
                    spriteBatch,
                    "PUBLISHED FROM TAPE \"" + submission.TapeName + "\"",
                    28,
                    y,
                    PrintTheme.Ink);
            }

            PrintTheme.Footer(spriteBatch, _whitePixel, _print, "ESC BACK TO CLASSIFIEDS");
            return;
        }

        foreach (var line in _contract.AdvertText.Split('\n'))
        {
            DrawText(spriteBatch, line, 28, y, PrintTheme.Ink);
            y += 16;
        }

        y += 12;
        DrawText(spriteBatch, "WHAT THEY WANT", 28, y, PrintTheme.Masthead);
        y += 18;

        foreach (var line in CareerService.DescribeRequirements(_contract.Requirements)
                     .Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            DrawText(spriteBatch, line, 28, y, PrintTheme.InkMuted);
            y += 16;
        }

        y += 12;
        DrawText(
            spriteBatch,
            "PAYMENT: " + PlayerProgress.FormatPounds(_contract.RewardPennies),
            28,
            y,
            PrintTheme.Ink);

        PrintTheme.Footer(spriteBatch, _whitePixel, _print, "S SUBMIT SOFTWARE    ESC BACK");
    }

    private void DrawSelectTape(SpriteBatch spriteBatch)
    {
        PrintTheme.DrawMasthead(
            spriteBatch,
            _whitePixel,
            _print,
            "CLASSIFIEDS",
            "SELECT A CASSETTE",
            "");

        if (_tapes.Count == 0)
        {
            DrawText(spriteBatch, "NO TAPES SAVED YET.", 232, 180, PrintTheme.Ink);
            DrawText(spriteBatch, "SAVE A PROGRAM FIRST.", 224, 204, PrintTheme.InkMuted);
            PrintTheme.Footer(spriteBatch, _whitePixel, _print, "ESC BACK");
            return;
        }

        var y = 72;
        for (var i = 0; i < _tapes.Count; i++)
        {
            var colour = i == _tapeSelected ? PrintTheme.Masthead : PrintTheme.Ink;
            var marker = i == _tapeSelected ? ">" : " ";
            DrawText(spriteBatch, $"{marker} {_tapes[i]}", 40, y, colour);
            y += 18;
            if (y > 400)
                break;
        }

        PrintTheme.Footer(spriteBatch, _whitePixel, _print, "UP DOWN    ENTER SELECT    ESC CANCEL");
    }

    private void DrawResult(SpriteBatch spriteBatch)
    {
        if (_contract == null || _result == null)
            return;

        var org = PublisherCatalog.GetOrganisation(_contract.OrganisationId);
        var tape = _tapes.Count > 0 ? _tapes[_tapeSelected] : "";

        PrintTheme.DrawMasthead(
            spriteBatch,
            _whitePixel,
            _print,
            org?.Name ?? "",
            "CHECKING \"" + tape + "\"",
            "");

        var y = 120;
        var checks = _result.Checks.Skip(_scroll).Take(12).ToList();

        foreach (var check in checks)
        {
            var mark = check.Passed ? "+" : "X";
            var colour = check.Passed ? PrintTheme.Ink : PrintTheme.Stamp;
            DrawText(spriteBatch, $"{mark} {check.Label}", 28, y, colour);
            y += 16;

            if (!string.IsNullOrWhiteSpace(check.Detail))
            {
                foreach (var detail in check.Detail.Split('\n'))
                {
                    DrawText(spriteBatch, "  " + detail, 28, y, PrintTheme.InkMuted);
                    y += 14;
                }
            }
        }

        y += 8;
        DrawText(
            spriteBatch,
            "PAYMENT: " + PlayerProgress.FormatPounds(_contract.RewardPennies),
            28,
            y,
            PrintTheme.Ink);
        y += 24;

        if (_result.AlreadyCompleted)
        {
            var pending = _career.GetAvailability(_contract) is
                ContractAvailability.Pending or ContractAvailability.ResponseReady;
            DrawText(
                spriteBatch,
                pending ? "THIS TAPE IS ALREADY IN THE POST." : "THIS ADVERT HAS BEEN FILLED.",
                28,
                y,
                PrintTheme.Stamp);
            PrintTheme.Footer(spriteBatch, _whitePixel, _print, "ESC BACK");
            return;
        }

        if (_result.Accepted)
        {
            DrawText(spriteBatch, "READY TO POST.", 28, y, PrintTheme.Ink);
            PrintTheme.Footer(spriteBatch, _whitePixel, _print, "Y SEND TAPE    N CANCEL");
        }
        else
        {
            DrawText(spriteBatch, "NOT READY TO POST.", 28, y, PrintTheme.Stamp);
            y += 20;
            var failed = _result.FailedChecks.FirstOrDefault();
            if (failed != null)
                DrawText(spriteBatch, HintFor(failed), 28, y, PrintTheme.InkMuted);

            PrintTheme.Footer(spriteBatch, _whitePixel, _print, "ESC BACK");
        }
    }

    private void DrawSent(SpriteBatch spriteBatch)
    {
        PrintTheme.DrawMasthead(
            spriteBatch,
            _whitePixel,
            _print,
            "CLASSIFIEDS",
            "TAPE IN THE POST",
            "");

        var y = 120;
        foreach (var line in _sentMessage.Split('\n'))
        {
            DrawText(spriteBatch, line, 80, y, PrintTheme.Ink);
            y += 20;
        }

        DrawText(spriteBatch, "WATCH THE NOTICE BOARD FOR A REPLY.", 80, 320, PrintTheme.Masthead);
        PrintTheme.Footer(spriteBatch, _whitePixel, _print, "ENTER / ESC RETURN TO BEDROOM");
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

    private void DrawText(SpriteBatch spriteBatch, string text, int x, int y, Color colour)
    {
        _print.Draw(spriteBatch, _print.Body, text, x, y, colour);
    }

    private bool Pressed(KeyboardState keyboard, Keys key)
    {
        return keyboard.IsKeyDown(key) &&
               !_previousKeyboard.IsKeyDown(key);
    }
}
