using System;
using System.Collections.Generic;
using System.Linq;

using Centauri64.Basic;
using Centauri64.Basic.Syntax;
using Centauri64.Machine.Images;
using Centauri64.Machine.Maps;
using Centauri64.Machine.Sprites;

namespace Centauri64.Analysis;

public sealed class SoftwareAnalyser
{
    /// <summary>
    /// Cover pixels that must differ from blank (colour 0) before a cover
    /// counts as custom. One accidental pixel is ignored; a small doodle
    /// or monochrome mark qualifies. This is not an art-quality score.
    /// </summary>
    public const int CustomCoverPixelThreshold = 12;

    private readonly Tokenizer _tokenizer = new();
    private readonly Parser _parser = new();
    private readonly ProgramStorage _programs = new();
    private readonly SpriteStorage _sprites = new();
    private readonly MapStorage _maps = new();
    private readonly ImageStorage _images = new();

    public SoftwareAnalysis AnalyseTape(string tapeName)
    {
        try
        {
            var program = LoadProgram(tapeName);
            var coverSaved = _programs.HasSavedCover(tapeName);
            var cover = coverSaved
                ? _programs.LoadCover(tapeName)
                : new TapeCover();

            var spriteStore = new SpriteAssetStore();
            _sprites.Load(tapeName, spriteStore);

            var mapStore = new MapAssetStore();
            _maps.Load(tapeName, mapStore);

            var imageStore = new ImageAssetStore();
            _images.Load(tapeName, imageStore);

            return Analyse(
                tapeName,
                program,
                cover,
                coverSaved,
                spriteStore,
                mapStore,
                imageStore);
        }
        catch (Exception exception)
        {
            return SoftwareAnalysis.Invalid(
                tapeName,
                exception.Message.ToUpperInvariant());
        }
    }

    public SoftwareAnalysis AnalyseProgram(
        BasicProgram program,
        string tapeName = "",
        ImageAssetStore? liveImages = null)
    {
        var cover = new TapeCover();
        var coverSaved = false;
        var spriteStore = new SpriteAssetStore();
        var mapStore = new MapAssetStore();
        var imageStore = liveImages ?? new ImageAssetStore();

        if (!string.IsNullOrWhiteSpace(tapeName))
        {
            try
            {
                coverSaved = _programs.HasSavedCover(tapeName);
                if (coverSaved)
                    cover = _programs.LoadCover(tapeName);

                _sprites.Load(tapeName, spriteStore);
                _maps.Load(tapeName, mapStore);
                if (liveImages == null)
                    _images.Load(tapeName, imageStore);
            }
            catch
            {
                // Keep empty assets if sidecar files are missing.
            }
        }

        return Analyse(
            tapeName,
            program,
            cover,
            coverSaved,
            spriteStore,
            mapStore,
            imageStore);
    }

    private BasicProgram LoadProgram(string tapeName)
    {
        var program = new BasicProgram();

        foreach (var sourceLine in _programs.Load(tapeName))
        {
            if (string.IsNullOrWhiteSpace(sourceLine))
                continue;

            var tokens = _tokenizer.Tokenize(sourceLine);
            program.StoreLine(_parser.ParseLine(tokens, sourceLine));
        }

        return program;
    }

    private static SoftwareAnalysis Analyse(
        string tapeName,
        BasicProgram program,
        TapeCover cover,
        bool coverSaved,
        SpriteAssetStore sprites,
        MapAssetStore maps,
        ImageAssetStore images)
    {
        var lines = program.GetLines();
        var commands = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var capabilities = SoftwareCapability.None;
        var statementCount = 0;

        if (lines.Count == 0)
        {
            return SoftwareAnalysis.Invalid(
                tapeName,
                "PROGRAM CONTAINS NO LINES");
        }

        foreach (var line in lines)
        {
            InspectStatement(
                line.Statement,
                commands,
                ref capabilities,
                ref statementCount);
        }

        var coverStats = AnalyseCover(cover, coverSaved);
        var animationCount = sprites.Assets.Sum(asset => asset.Animations.Count);

        var sortedCommands = commands
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new SoftwareAnalysis
        {
            TapeName = tapeName,
            ProgramValid = true,
            LineCount = lines.Count,
            StatementCount = statementCount,
            UniqueCommandCount = sortedCommands.Count,
            CommandsUsed = sortedCommands,
            Capabilities = capabilities,
            SpriteCount = sprites.Assets.Count,
            AnimationCount = animationCount,
            MapCount = maps.Maps.Count,
            ImageCount = images.Count,
            ImageFrameCount = images.Images.Sum(image => image.FrameCount),
            GeneralImageCount = images.Images.Count(image => image.Category == ImageCategory.General),
            SpriteImageCount = images.Images.Count(image => image.Category == ImageCategory.Sprite),
            TilesetImageCount = images.Images.Count(image => image.Category == ImageCategory.Tileset),
            BackgroundImageCount = images.Images.Count(image => image.Category == ImageCategory.Background),
            HasCover = coverStats.HasCover,
            HasCustomCover = coverStats.HasCustomCover,
            CoverChangedPixelCount = coverStats.ChangedPixels,
            CoverCoveragePercent = coverStats.CoveragePercent,
            CoverColourCount = coverStats.ColourCount
        };
    }

    private static void InspectStatement(
        Statement statement,
        HashSet<string> commands,
        ref SoftwareCapability capabilities,
        ref int statementCount)
    {
        statementCount++;

        if (CapabilityCatalog.TryGetStatement(
                statement.GetType(),
                out var name,
                out var caps))
        {
            if (statement is not RemStatement)
                commands.Add(name);

            capabilities |= caps;
        }

        switch (statement)
        {
            case IfStatement ifStatement:
                InspectExpression(ifStatement.Condition, commands, ref capabilities);
                InspectStatement(
                    ifStatement.ThenStatement,
                    commands,
                    ref capabilities,
                    ref statementCount);
                break;

            case ForStatement forStatement:
                InspectExpression(forStatement.Start, commands, ref capabilities);
                InspectExpression(forStatement.End, commands, ref capabilities);
                if (forStatement.Step != null)
                    InspectExpression(forStatement.Step, commands, ref capabilities);
                break;

            case AssignmentStatement assignment:
                InspectExpression(assignment.Value, commands, ref capabilities);
                if (assignment.VariableName.EndsWith("$", StringComparison.Ordinal))
                    capabilities |= SoftwareCapability.Strings;
                break;

            case ArrayAssignmentStatement arrayAssignment:
                InspectExpression(arrayAssignment.Index, commands, ref capabilities);
                InspectExpression(arrayAssignment.Value, commands, ref capabilities);
                break;

            case PrintStatement print:
                InspectExpression(print.Expression, commands, ref capabilities);
                break;

            case PrintAtStatement printAt:
                InspectExpression(printAt.X, commands, ref capabilities);
                InspectExpression(printAt.Y, commands, ref capabilities);
                InspectExpression(printAt.Text, commands, ref capabilities);
                break;

            case InputStatement input:
                if (input.Prompt != null)
                    InspectExpression(input.Prompt, commands, ref capabilities);
                if (input.VariableName.EndsWith("$", StringComparison.Ordinal))
                    capabilities |= SoftwareCapability.Strings;
                break;

            case NetSendStatement netSend:
                InspectExpression(netSend.Name, commands, ref capabilities);
                InspectExpression(netSend.Value, commands, ref capabilities);
                break;

            case SpriteStatement sprite:
                InspectExpression(sprite.SpriteIndex, commands, ref capabilities);
                InspectExpression(sprite.AssetName, commands, ref capabilities);
                break;

            case SpritePositionStatement spritePos:
                InspectExpression(spritePos.SpriteIndex, commands, ref capabilities);
                InspectExpression(spritePos.X, commands, ref capabilities);
                InspectExpression(spritePos.Y, commands, ref capabilities);
                break;

            case SpriteShowStatement spriteShow:
                InspectExpression(spriteShow.SpriteIndex, commands, ref capabilities);
                break;

            case SpriteHideStatement spriteHide:
                InspectExpression(spriteHide.SpriteIndex, commands, ref capabilities);
                break;

            case SpriteFlipStatement spriteFlip:
                InspectExpression(spriteFlip.SpriteIndex, commands, ref capabilities);
                InspectExpression(spriteFlip.Facing, commands, ref capabilities);
                break;

            case SpriteAnimationStatement spriteAnim:
                InspectExpression(spriteAnim.SpriteIndex, commands, ref capabilities);
                InspectExpression(spriteAnim.AnimationName, commands, ref capabilities);
                InspectExpression(spriteAnim.Loop, commands, ref capabilities);
                break;

            case PlotStatement plot:
                InspectExpression(plot.X, commands, ref capabilities);
                InspectExpression(plot.Y, commands, ref capabilities);
                InspectExpression(plot.Colour, commands, ref capabilities);
                break;

            case LineStatement lineStmt:
                InspectExpression(lineStmt.X1, commands, ref capabilities);
                InspectExpression(lineStmt.Y1, commands, ref capabilities);
                InspectExpression(lineStmt.X2, commands, ref capabilities);
                InspectExpression(lineStmt.Y2, commands, ref capabilities);
                InspectExpression(lineStmt.Colour, commands, ref capabilities);
                break;

            case RectStatement rect:
                InspectExpression(rect.X, commands, ref capabilities);
                InspectExpression(rect.Y, commands, ref capabilities);
                InspectExpression(rect.Width, commands, ref capabilities);
                InspectExpression(rect.Height, commands, ref capabilities);
                InspectExpression(rect.Colour, commands, ref capabilities);
                break;

            case CircleStatement circle:
                InspectExpression(circle.X, commands, ref capabilities);
                InspectExpression(circle.Y, commands, ref capabilities);
                InspectExpression(circle.Radius, commands, ref capabilities);
                InspectExpression(circle.Colour, commands, ref capabilities);
                break;

            case BeepStatement beep:
                InspectExpression(beep.Frequency, commands, ref capabilities);
                InspectExpression(beep.Duration, commands, ref capabilities);
                break;

            case WaitStatement wait:
                InspectExpression(wait.Duration, commands, ref capabilities);
                break;

            case ModeStatement mode:
                InspectExpression(mode.Mode, commands, ref capabilities);
                break;

            case InkStatement ink:
                InspectExpression(ink.Colour, commands, ref capabilities);
                break;

            case PaperStatement paper:
                InspectExpression(paper.Colour, commands, ref capabilities);
                break;

            case DimStatement dim:
                InspectExpression(dim.Size, commands, ref capabilities);
                break;

            case TdefStatement tdef:
                InspectExpression(tdef.TileId, commands, ref capabilities);
                InspectExpression(tdef.AssetName, commands, ref capabilities);
                break;

            case MapStatement map:
                InspectExpression(map.Columns, commands, ref capabilities);
                InspectExpression(map.Rows, commands, ref capabilities);
                break;

            case LoadMapStatement loadMap:
                InspectExpression(loadMap.Name, commands, ref capabilities);
                break;

            case CameraStatement camera:
                if (camera.SpriteIndex != null)
                    InspectExpression(camera.SpriteIndex, commands, ref capabilities);
                if (camera.X != null)
                    InspectExpression(camera.X, commands, ref capabilities);
                if (camera.Y != null)
                    InspectExpression(camera.Y, commands, ref capabilities);
                break;
        }
    }

    private static void InspectExpression(
        Expression expression,
        HashSet<string> commands,
        ref SoftwareCapability capabilities)
    {
        switch (expression)
        {
            case StringExpression:
                capabilities |= SoftwareCapability.Strings | SoftwareCapability.Text;
                break;

            case VariableExpression variable:
                if (variable.Name.EndsWith("$", StringComparison.Ordinal))
                    capabilities |= SoftwareCapability.Strings;
                break;

            case FunctionCallExpression function:
                if (CapabilityCatalog.TryGetFunction(
                        function.Name,
                        out var name,
                        out var caps))
                {
                    commands.Add(name);
                    capabilities |= caps;
                }

                foreach (var argument in function.Arguments)
                    InspectExpression(argument, commands, ref capabilities);
                break;

            case BinaryExpression binary:
                InspectExpression(binary.Left, commands, ref capabilities);
                InspectExpression(binary.Right, commands, ref capabilities);
                break;

            case UnaryExpression unary:
                InspectExpression(unary.Operand, commands, ref capabilities);
                break;

            case ArrayAccessExpression arrayAccess:
                InspectExpression(arrayAccess.Index, commands, ref capabilities);
                break;
        }
    }

    private readonly struct CoverStats
    {
        public bool HasCover { get; init; }
        public bool HasCustomCover { get; init; }
        public int ChangedPixels { get; init; }
        public double CoveragePercent { get; init; }
        public int ColourCount { get; init; }
    }

    /// <summary>
    /// Cover rules:
    /// - No <c>.cover</c> file → no cover (HasCover/HasCustomCover false, metrics 0).
    /// - Saved blank/default (all colour 0) → HasCover true, HasCustomCover false.
    /// - Saved art with ≥ <see cref="CustomCoverPixelThreshold"/> non-zero pixels
    ///   → HasCustomCover true. Threshold ignores accidental single-pixel edits;
    ///   it is not an art-quality score.
    /// Pixel metrics describe only the saved cover asset, never cassette UI chrome.
    /// </summary>
    private static CoverStats AnalyseCover(TapeCover cover, bool coverSaved)
    {
        if (!coverSaved)
        {
            return new CoverStats
            {
                HasCover = false,
                HasCustomCover = false,
                ChangedPixels = 0,
                CoveragePercent = 0,
                ColourCount = 0
            };
        }

        var colours = new HashSet<int>();
        var changed = 0;
        var total = TapeCover.Width * TapeCover.Height;

        for (var y = 0; y < TapeCover.Height; y++)
        {
            for (var x = 0; x < TapeCover.Width; x++)
            {
                var colour = cover.Pixels[y, x];

                if (colour == 0)
                    continue;

                changed++;
                colours.Add(colour);
            }
        }

        var coverage = total == 0
            ? 0
            : Math.Round(changed * 100.0 / total, 2);

        return new CoverStats
        {
            HasCover = true,
            HasCustomCover = changed >= CustomCoverPixelThreshold,
            ChangedPixels = changed,
            CoveragePercent = coverage,
            ColourCount = colours.Count
        };
    }
}
