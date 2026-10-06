using System;

using Centauri64.Machine;
using Centauri64.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Centauri64.Game;

public sealed partial class MagazinesScreen
{
    public event Action? CareerChanged;

    private void PersistCareer()
    {
        CareerChanged?.Invoke();
    }

    private void UpdateShelf(KeyboardState keyboard)
    {
        if (Pressed(keyboard, Keys.Escape))
        {
            ExitSelected?.Invoke();
            return;
        }

        var issues = MagazineCatalog.Issues;
        if (Pressed(keyboard, Keys.Up))
            _issueSelected = Math.Max(0, _issueSelected - 1);

        if (Pressed(keyboard, Keys.Down))
            _issueSelected = Math.Min(issues.Length - 1, _issueSelected + 1);

        const int visible = 8;
        if (_issueSelected < _issueScroll)
            _issueScroll = _issueSelected;
        if (_issueSelected >= _issueScroll + visible)
            _issueScroll = _issueSelected - visible + 1;

        if (Pressed(keyboard, Keys.Enter) || Pressed(keyboard, Keys.Space))
        {
            _issue = issues[_issueSelected];
            _view = View.Issue;
        }

        if (!_referenceLibrary && Pressed(keyboard, Keys.C))
        {
            RefreshList();
            _view = View.List;
        }
    }

    private void UpdateIssue(KeyboardState keyboard)
    {
        if (Pressed(keyboard, Keys.Escape))
        {
            _view = View.Shelf;
            _issue = null;
            return;
        }

        if (_issue == null)
            return;

        var state = CurrentState(_issue);
        if (state == MagazineIssueState.Owned && Pressed(keyboard, Keys.Enter))
        {
            // Summary is the readable content for this foundation phase.
        }
    }

    private MagazineIssueState CurrentState(MagazineIssue issue) =>
        MagazineProgression.StateOf(GameSession.Career, issue, _referenceLibrary);

    private void DrawShelf(SpriteBatch spriteBatch)
    {
        DrawText(spriteBatch, "CENTAURI64 MAGAZINE", 216, 24, Cream);
        var subtitle = _referenceLibrary
            ? "YEAR ONE  /  REFERENCE LIBRARY"
            : "YEAR ONE  /  THE BEDROOM CODER";
        DrawText(spriteBatch, subtitle, 200, 44, Cyan);

        var career = GameSession.Career;
        var y = 72;
        var issues = MagazineCatalog.Issues;
        const int visible = 8;

        for (var i = _issueScroll; i < issues.Length && i < _issueScroll + visible; i++)
        {
            var issue = issues[i];
            var state = CurrentState(issue);
            var selected = i == _issueSelected;
            var marker = selected ? ">" : " ";
            var colour = selected
                ? Yellow
                : state == MagazineIssueState.Owned
                    ? Cream
                    : state == MagazineIssueState.ComingNext
                        ? Cyan
                        : Muted;

            var badge = state switch
            {
                MagazineIssueState.Owned => "IN",
                MagazineIssueState.ComingNext => "NEXT",
                _ => "LATER"
            };

            DrawText(
                spriteBatch,
                $"{marker} #{issue.IssueNumber}  {issue.CoverHeadline}",
                32,
                y,
                colour);
            DrawText(spriteBatch, badge, 560, y, colour);
            y += 18;

            if (selected)
            {
                var line = state == MagazineIssueState.Owned
                    ? "COVER: " + issue.CoverGameTitle
                    : state == MagazineIssueState.ComingNext
                        ? issue.Teaser
                        : DistantTeaser(issue);
                DrawText(spriteBatch, WrapOne(line, 68), 48, y, Muted);
                y += 20;
            }
        }

        var hint = _referenceLibrary
            ? "UP DOWN    ENTER OPEN    ESC BACK"
            : "UP DOWN    ENTER OPEN    C CLASSIFIEDS    ESC BACK";
        DrawFooter(spriteBatch, hint);

        if (career != null && !_referenceLibrary)
        {
            var owned = MagazineProgression.HighestOwnedNumber(career);
            DrawText(spriteBatch, "ISSUES ON THE SHELF: " + owned + " / 10", 400, 44, Yellow);
        }
    }

    private void DrawIssue(SpriteBatch spriteBatch)
    {
        if (_issue == null)
            return;

        var state = CurrentState(_issue);
        DrawText(spriteBatch, "ISSUE #" + _issue.IssueNumber, 48, 24, Cream);
        if (!string.IsNullOrEmpty(_issue.FictionalMonth))
            DrawText(spriteBatch, _issue.FictionalMonth, 480, 24, Muted);

        DrawText(spriteBatch, _issue.CoverHeadline, 48, 44, Yellow);

        var y = 80;
        var status = state switch
        {
            MagazineIssueState.Owned => _referenceLibrary ? "IN THE LIBRARY" : "AVAILABLE NOW",
            MagazineIssueState.ComingNext => "COMING NEXT",
            _ => "COMING LATER"
        };
        DrawText(spriteBatch, status, 48, y, Cyan);
        y += 24;

        if (state == MagazineIssueState.Owned)
            DrawOwnedIssue(spriteBatch, ref y);
        else if (state == MagazineIssueState.ComingNext)
            DrawNextIssue(spriteBatch, ref y);
        else
            DrawLaterIssue(spriteBatch, ref y);

        DrawFooter(spriteBatch, "ESC BACK TO SHELF");
    }

    private void DrawOwnedIssue(SpriteBatch spriteBatch, ref int y)
    {
        DrawText(spriteBatch, "COVER TAPE", 48, y, Cyan);
        y += 18;
        DrawText(spriteBatch, _issue!.CoverGameTitle, 48, y, Yellow);
        y += 18;
        foreach (var line in Wrap(_issue.CoverGameDescription, 68))
        {
            DrawText(spriteBatch, line, 48, y, Cream);
            y += 16;
        }

        y += 8;
        DrawText(spriteBatch, "THIS ISSUE:", 48, y, Cyan);
        y += 18;
        foreach (var line in Wrap(_issue.FullDescription, 68))
        {
            DrawText(spriteBatch, line, 48, y, Cream);
            y += 16;
        }

        if (_issue.ConceptsIntroduced.Count > 0 && y < 300)
        {
            y += 6;
            DrawText(spriteBatch, "LEARN:", 48, y, Cyan);
            y += 18;
            var learn = string.Join("  •  ", _issue.ConceptsIntroduced);
            foreach (var line in Wrap(learn, 68))
            {
                if (y > 360)
                    break;
                DrawText(spriteBatch, line, 48, y, Cream);
                y += 16;
            }
        }

        if (_issue.GrantedFeatures.Count > 0)
        {
            y += 6;
            DrawText(spriteBatch, "NEW:", 48, y, Cyan);
            y += 18;
            DrawText(spriteBatch, FeatureLabel(_issue.GrantedFeatures[0]), 48, y, Yellow);
            y += 18;
        }

        var next = MagazineCatalog.Next(_issue);
        if (next != null && y < 390)
        {
            y += 8;
            DrawText(spriteBatch, "NEXT ISSUE  " + next.CoverHeadline, 48, y, Cyan);
            y += 16;
            DrawText(spriteBatch, WrapOne(next.Teaser, 68), 48, y, Muted);
        }
    }

    private void DrawNextIssue(SpriteBatch spriteBatch, ref int y)
    {
        DrawText(spriteBatch, "COVER TAPE", 48, y, Cyan);
        y += 18;
        DrawText(spriteBatch, _issue!.CoverGameTitle, 48, y, Yellow);
        y += 22;
        foreach (var line in Wrap(_issue.Teaser, 68))
        {
            DrawText(spriteBatch, line, 48, y, Cream);
            y += 16;
        }

        y += 12;
        DrawText(spriteBatch, "KEEP PROGRAMMING!", 48, y, Yellow);
        y += 18;
        DrawText(spriteBatch, "NEW ISSUES ARRIVE AS YOUR CAREER DEVELOPS.", 48, y, Muted);
    }

    private void DrawLaterIssue(SpriteBatch spriteBatch, ref int y)
    {
        foreach (var line in Wrap(DistantTeaser(_issue!), 68))
        {
            DrawText(spriteBatch, line, 48, y, Cream);
            y += 18;
        }

        y += 16;
        DrawText(spriteBatch, "KEEP PROGRAMMING!", 48, y, Muted);
    }

    private static string DistantTeaser(MagazineIssue issue)
    {
        if (issue.IssueNumber >= 10)
            return issue.Teaser;

        if (issue.IssueNumber >= 8)
            return issue.Teaser;

        return issue.CoverHeadline + "  —  " + issue.Teaser;
    }

    private static string FeatureLabel(FeatureId feature) =>
        feature switch
        {
            FeatureId.Graphics => "BASIC GRAPHICS",
            FeatureId.Sprites => "SPRITE DESIGNER",
            FeatureId.Maps => "MAP EDITOR",
            FeatureId.Images => "IMAGE EDITOR",
            FeatureId.Networking => "NETWORKING",
            FeatureId.Arrays => "ARRAYS / BIGGER GAMES",
            FeatureId.DataStatements => "DATA STATEMENTS",
            FeatureId.CustomAssets => "CUSTOM ASSETS",
            FeatureId.LowLevelMachine => "LOW-LEVEL VIDEO TRICKS",
            _ => feature.ToString().ToUpperInvariant()
        };

    private static string WrapOne(string text, int width)
    {
        var lines = Wrap(text, width);
        return lines.Length == 0 ? text : lines[0];
    }

    private static string[] Wrap(string text, int width)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<string>();

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new System.Collections.Generic.List<string>();
        var current = "";
        foreach (var word in words)
        {
            var next = current.Length == 0 ? word : current + " " + word;
            if (next.Length > width && current.Length > 0)
            {
                lines.Add(current);
                current = word;
            }
            else
            {
                current = next;
            }
        }

        if (current.Length > 0)
            lines.Add(current);

        return lines.ToArray();
    }
}
