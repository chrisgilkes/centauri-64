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
            var empty = new PrintLayout(spriteBatch, _whitePixel, _print, 320, 360, 640);
            empty.BodyLine("NO SMALL ADS THIS MONTH.");
            empty.CaptionLine("THE SOFTWARE MARKET IS STILL WAKING UP.");
            PrintTheme.Footer(spriteBatch, _whitePixel, _print, "ESC BACK TO SHELF");
            return;
        }

        const int cardH = 168;
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
                var head = new PrintLayout(spriteBatch, _whitePixel, _print, 48, y, 400);
                head.Section("READER CHALLENGES", PrintTheme.Masthead);
                head.Rule();
                y = head.Y + 4;
                drewChallengeHead = true;
            }
            else if (!challenge && !drewWantedHead)
            {
                if (drewChallengeHead)
                    y += 8;
                var head = new PrintLayout(spriteBatch, _whitePixel, _print, 48, y, 400);
                head.Section("SOFTWARE WANTED", PrintTheme.SpotBlue);
                head.Rule();
                y = head.Y + 4;
                drewWantedHead = true;
            }

            var bounds = new Rectangle(48, y, 1184, cardH);
            DrawAdvert(spriteBatch, contract, bounds, i == _selected, progress);
            y += cardH + 14;
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
        var fill = selected ? PrintTheme.Highlight : Color.White;

        PrintTheme.Box(spriteBatch, _whitePixel, bounds, fill);
        var thickness = org?.AdvertStyle == PrintAdvertStyle.Bold ? 3 : 2;
        PrintTheme.Frame(spriteBatch, _whitePixel, bounds, accent, thickness);

        if (org?.AdvertStyle == PrintAdvertStyle.Ornate)
            PrintTheme.Frame(
                spriteBatch,
                _whitePixel,
                new Rectangle(bounds.X + 4, bounds.Y + 4, bounds.Width - 8, bounds.Height - 8),
                accent);

        if (org?.AdvertStyle == PrintAdvertStyle.Technical)
            PrintTheme.Box(
                spriteBatch,
                _whitePixel,
                new Rectangle(bounds.X, bounds.Y, 8, bounds.Height),
                accent);

        var textWidth = bounds.Width - 40;
        var layout = new PrintLayout(
            spriteBatch,
            _whitePixel,
            _print,
            bounds.X + 20,
            bounds.Y + 14,
            textWidth,
            bounds.Bottom - 44);

        layout.Section(org?.Name ?? contract.OrganisationId, accent);
        layout.Heading(contract.Title);
        layout.Paragraph(contract.AdvertText.Replace('\n', ' '), PrintTheme.InkMuted, maxLines: 2);

        _print.Draw(
            spriteBatch,
            _print.Caption,
            "WE PAY  " + PlayerProgress.FormatPounds(contract.RewardPennies),
            bounds.X + 20,
            bounds.Bottom - 34,
            PrintTheme.Masthead);

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
        var challenge = IsReaderChallenge(_contract);
        var kicker = challenge ? "READER CHALLENGE" : "CLASSIFIED ADVERT";
        var era = MagazineProgression.EraLabel(GameSession.Career, _referenceLibrary);

        PrintTheme.DrawMasthead(
            spriteBatch,
            _whitePixel,
            _print,
            kicker,
            org?.Name ?? "ORGANISATION",
            era);

        var column = new Rectangle(56, 120, 760, 740);
        var side = new Rectangle(860, 120, 360, 740);
        var layout = new PrintLayout(
            spriteBatch,
            _whitePixel,
            _print,
            column.X,
            column.Y,
            column.Width,
            860);

        layout.Heading(_contract.Title);
        layout.Rule();

        if (availability is ContractAvailability.Pending
            or ContractAvailability.ResponseReady)
        {
            layout.Section(FictionalStamp(availability) ?? "SUBMITTED", PrintTheme.Stamp);
            layout.BodyLine("TAPE ENCLOSED: \"" + (submission?.TapeName ?? "?") + "\"");
            layout.Space(8);
            layout.Paragraph(
                availability == ContractAvailability.Pending
                    ? "The editors have your cassette. A reply will follow by post."
                    : "A letter is waiting on the Notice Board.");
            PrintTheme.Footer(spriteBatch, _whitePixel, _print, "ESC BACK TO CLASSIFIEDS");
            return;
        }

        if (availability == ContractAvailability.Completed)
        {
            layout.Section("CONTRACT AWARDED", PrintTheme.Stamp);
            if (submission != null)
                layout.BodyLine("Published from tape \"" + submission.TapeName + "\".");
            else
                layout.Paragraph("This opportunity is no longer accepting submissions.");
            PrintTheme.Footer(spriteBatch, _whitePixel, _print, "ESC BACK TO CLASSIFIEDS");
            return;
        }

        layout.Paragraph(_contract.AdvertText, PrintTheme.Ink, maxLines: 14);
        layout.Space(8);
        layout.Section(challenge ? "WHAT WE'RE LOOKING FOR" : "WHAT THEY WANT", PrintTheme.Masthead);
        layout.BulletList(PrintRequirements.Describe(_contract.Requirements), PrintTheme.Ink, "* ");

        DrawPaymentCallout(spriteBatch, side, challenge);
        PrintTheme.Footer(
            spriteBatch,
            _whitePixel,
            _print,
            challenge
                ? "S - SEND YOUR TAPE    ESC BACK"
                : "S - SEND YOUR TAPE    ESC BACK");
    }

    private void DrawPaymentCallout(SpriteBatch spriteBatch, Rectangle side, bool challenge)
    {
        var box = new Rectangle(side.X, side.Y + 24, side.Width, 180);
        PrintTheme.Box(spriteBatch, _whitePixel, box, PrintTheme.Highlight);
        PrintTheme.Frame(spriteBatch, _whitePixel, box, PrintTheme.Masthead, 2);

        var layout = new PrintLayout(
            spriteBatch,
            _whitePixel,
            _print,
            box.X + 20,
            box.Y + 18,
            box.Width - 40,
            box.Bottom - 12);

        layout.Section(challenge ? "WE'LL PAY" : "PAYMENT", PrintTheme.Masthead);
        layout.Heading(PlayerProgress.FormatPounds(_contract!.RewardPennies));
        layout.CaptionLine(
            challenge
                ? "For every program we print."
                : "For software we accept.");

        var note = new Rectangle(side.X, box.Bottom + 24, side.Width, 220);
        PrintTheme.Box(spriteBatch, _whitePixel, note, Color.White);
        PrintTheme.Frame(spriteBatch, _whitePixel, note, PrintTheme.Rule, 1);
        var noteLayout = new PrintLayout(
            spriteBatch,
            _whitePixel,
            _print,
            note.X + 18,
            note.Y + 16,
            note.Width - 36,
            note.Bottom - 12);
        noteLayout.Section("HOW TO ENTER", PrintTheme.SpotBlue);
        noteLayout.Paragraph(
            "Save your program to cassette, then press S to choose a tape. " +
            "We check that it meets this advert before you post it. " +
            "Acceptance or rejection arrives later by letter.");
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

        var layout = new PrintLayout(spriteBatch, _whitePixel, _print, 80, 130, 1100, 860);

        if (_tapes.Count == 0)
        {
            layout.BodyLine("NO TAPES SAVED YET.");
            layout.CaptionLine("SAVE A PROGRAM FIRST.");
            PrintTheme.Footer(spriteBatch, _whitePixel, _print, "ESC BACK");
            return;
        }

        layout.CaptionLine("Choose the cassette you want to enclose with this advert.");
        layout.Space(12);

        for (var i = 0; i < _tapes.Count; i++)
        {
            var colour = i == _tapeSelected ? PrintTheme.Masthead : PrintTheme.Ink;
            var marker = i == _tapeSelected ? "> " : "  ";
            layout.BodyLine(marker + _tapes[i], colour);
            if (layout.Y > 820)
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
            org?.Name ?? "CLASSIFIEDS",
            "CHECKING YOUR TAPE",
            tape);

        var layout = new PrintLayout(spriteBatch, _whitePixel, _print, 80, 130, 900, 860);
        layout.CaptionLine("Factual check only. Editors reply by post.");
        layout.Space(10);
        layout.Rule();

        var checks = _result.Checks.Skip(_scroll).Take(10).ToList();
        foreach (var check in checks)
        {
            var mark = check.Passed ? "+" : "X";
            var colour = check.Passed ? PrintTheme.Ink : PrintTheme.Stamp;
            layout.BodyLine(mark + "  " + PrintRequirements.FriendlyCheckLabel(check.Label), colour);
            if (!string.IsNullOrWhiteSpace(check.Detail))
                layout.CaptionLine(check.Detail.Replace('\n', ' '));
        }

        layout.Space(16);

        if (_result.AlreadyCompleted)
        {
            var pending = _career.GetAvailability(_contract) is
                ContractAvailability.Pending or ContractAvailability.ResponseReady;
            layout.Section(
                pending ? "THIS TAPE IS ALREADY IN THE POST." : "THIS ADVERT HAS BEEN FILLED.",
                PrintTheme.Stamp);
            PrintTheme.Footer(spriteBatch, _whitePixel, _print, "ESC BACK");
            return;
        }

        if (_result.Accepted)
        {
            layout.Section("READY TO POST", PrintTheme.SpotBlue);
            layout.Paragraph(
                "Your tape meets this advert. Post it now, then watch the Notice Board for a reply.");
            PrintTheme.Footer(spriteBatch, _whitePixel, _print, "Y / ENTER SEND TAPE    N / ESC KEEP WORKING");
        }
        else
        {
            layout.Section("NOT READY YET", PrintTheme.Stamp);
            layout.Paragraph("Your program does not yet meet this challenge.");
            var failed = _result.FailedChecks.FirstOrDefault();
            if (failed != null)
            {
                layout.Space(6);
                layout.Section("MISSING", PrintTheme.Masthead);
                layout.BulletList(new[] { PrintRequirements.FriendlyCheckLabel(failed.Label) });
                layout.CaptionLine(HintFor(failed));
            }

            PrintTheme.Footer(spriteBatch, _whitePixel, _print, "ESC RETURN");
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

        var layout = new PrintLayout(spriteBatch, _whitePixel, _print, 120, 160, 1000, 860);
        layout.Paragraph(_sentMessage);
        layout.Space(24);
        layout.Section("WATCH THE NOTICE BOARD FOR A REPLY.", PrintTheme.Masthead);
        PrintTheme.Footer(spriteBatch, _whitePixel, _print, "ENTER / ESC RETURN TO BEDROOM");
    }

    private static string HintFor(SubmissionCheck check)
    {
        if (check.Label.Contains("COVER", StringComparison.OrdinalIgnoreCase))
            return "Create a cover for your cassette and try again.";

        if (check.Label.Contains("INPUT", StringComparison.OrdinalIgnoreCase))
            return "Try using KEY, KEYPRESSED or INPUT.";

        if (check.Label.Contains("NETWORK", StringComparison.OrdinalIgnoreCase))
            return "See Network BASIC in the Programming Manual.";

        if (check.Label.Contains("MAXIMUM", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(check.Detail))
        {
            return check.Detail.Replace('\n', ' ');
        }

        return "Fix the failed requirement and try again.";
    }

    private bool Pressed(KeyboardState keyboard, Keys key)
    {
        return keyboard.IsKeyDown(key) &&
               !_previousKeyboard.IsKeyDown(key);
    }
}
