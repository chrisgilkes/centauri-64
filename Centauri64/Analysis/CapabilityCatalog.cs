using System;
using System.Collections.Generic;

using Centauri64.Basic.Syntax;

namespace Centauri64.Analysis;

/// <summary>
/// Maps BASIC statements and functions to analyser capabilities.
/// When you add a new BASIC command or function, add a row here.
/// </summary>
public static class CapabilityCatalog
{
    private static readonly Dictionary<Type, (string Name, SoftwareCapability Caps)> Statements =
        new()
        {
            [typeof(PrintStatement)] = ("PRINT", SoftwareCapability.Text),
            [typeof(PrintAtStatement)] = ("PRINTAT", SoftwareCapability.Text | SoftwareCapability.Graphics),
            [typeof(ClsStatement)] = ("CLS", SoftwareCapability.Text),
            [typeof(InkStatement)] = ("INK", SoftwareCapability.Text | SoftwareCapability.Graphics),
            [typeof(PaperStatement)] = ("PAPER", SoftwareCapability.Text | SoftwareCapability.Graphics),
            [typeof(InputStatement)] = ("INPUT", SoftwareCapability.Input | SoftwareCapability.Text | SoftwareCapability.Strings),
            [typeof(GotoStatement)] = ("GOTO", SoftwareCapability.None),
            [typeof(GosubStatement)] = ("GOSUB", SoftwareCapability.None),
            [typeof(ReturnStatement)] = ("RETURN", SoftwareCapability.None),
            [typeof(IfStatement)] = ("IF", SoftwareCapability.None),
            [typeof(ForStatement)] = ("FOR", SoftwareCapability.None),
            [typeof(NextStatement)] = ("NEXT", SoftwareCapability.None),
            [typeof(YieldStatement)] = ("YIELD", SoftwareCapability.None),
            [typeof(WaitStatement)] = ("WAIT", SoftwareCapability.None),
            [typeof(EndStatement)] = ("END", SoftwareCapability.None),
            [typeof(RemStatement)] = ("REM", SoftwareCapability.None),
            [typeof(DimStatement)] = ("DIM", SoftwareCapability.None),
            [typeof(AssignmentStatement)] = ("LET", SoftwareCapability.None),
            [typeof(ArrayAssignmentStatement)] = ("LET", SoftwareCapability.None),
            [typeof(ResetStatement)] = ("RESET", SoftwareCapability.None),
            [typeof(ModeStatement)] = ("MODE", SoftwareCapability.Graphics),
            [typeof(PlotStatement)] = ("PLOT", SoftwareCapability.Graphics),
            [typeof(LineStatement)] = ("LINE", SoftwareCapability.Graphics),
            [typeof(RectStatement)] = ("RECT", SoftwareCapability.Graphics),
            [typeof(CircleStatement)] = ("CIRCLE", SoftwareCapability.Graphics),
            [typeof(BeepStatement)] = ("BEEP", SoftwareCapability.Sound),
            [typeof(SpriteStatement)] = ("SPRITE", SoftwareCapability.Sprites),
            [typeof(SpritePositionStatement)] = ("SPRITEPOS", SoftwareCapability.Sprites),
            [typeof(SpriteShowStatement)] = ("SPRITESHOW", SoftwareCapability.Sprites),
            [typeof(SpriteHideStatement)] = ("SPRITEHIDE", SoftwareCapability.Sprites),
            [typeof(SpriteFlipStatement)] = ("SPRITEFLIP", SoftwareCapability.Sprites),
            [typeof(SpriteAnimationStatement)] = ("SPRITEANIM", SoftwareCapability.Sprites | SoftwareCapability.Animation),
            [typeof(TdefStatement)] = ("TDEF", SoftwareCapability.Maps | SoftwareCapability.Sprites),
            [typeof(MapStatement)] = ("MAP", SoftwareCapability.Maps),
            [typeof(LoadMapStatement)] = ("LOADMAP", SoftwareCapability.Maps),
            [typeof(CameraStatement)] = ("CAMERA", SoftwareCapability.Camera | SoftwareCapability.Graphics),
            [typeof(CamOffStatement)] = ("CAMOFF", SoftwareCapability.Camera | SoftwareCapability.Graphics),
            [typeof(NetHostStatement)] = ("NET HOST", SoftwareCapability.Networking),
            [typeof(NetJoinStatement)] = ("NET JOIN", SoftwareCapability.Networking),
            [typeof(NetWaitStatement)] = ("NET WAIT", SoftwareCapability.Networking),
            [typeof(NetLeaveStatement)] = ("NET LEAVE", SoftwareCapability.Networking),
            [typeof(NetSendStatement)] = ("NET SEND", SoftwareCapability.Networking)
        };

    private static readonly Dictionary<string, (string Name, SoftwareCapability Caps)> Functions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["KEY"] = ("KEY", SoftwareCapability.Input),
            ["KEYPRESSED"] = ("KEYPRESSED", SoftwareCapability.Input),
            ["RND"] = ("RND", SoftwareCapability.Random),
            ["COLLIDE"] = ("COLLIDE", SoftwareCapability.Sprites),
            ["SWIDTH"] = ("SWIDTH", SoftwareCapability.Graphics),
            ["SHEIGHT"] = ("SHEIGHT", SoftwareCapability.Graphics),
            ["ANIMPLAYING"] = ("ANIMPLAYING", SoftwareCapability.Sprites | SoftwareCapability.Animation),
            ["LEN"] = ("LEN", SoftwareCapability.Strings),
            ["LEFT$"] = ("LEFT$", SoftwareCapability.Strings),
            ["RIGHT$"] = ("RIGHT$", SoftwareCapability.Strings),
            ["MID$"] = ("MID$", SoftwareCapability.Strings),
            ["UPPER$"] = ("UPPER$", SoftwareCapability.Strings),
            ["TILEAT"] = ("TILEAT", SoftwareCapability.Maps),
            ["NET"] = ("NET", SoftwareCapability.Networking),
            ["NETPLAYER"] = ("NETPLAYER", SoftwareCapability.Networking),
            ["NETCONNECTED"] = ("NETCONNECTED", SoftwareCapability.Networking)
        };

    public static bool TryGetStatement(
        Type statementType,
        out string name,
        out SoftwareCapability capabilities)
    {
        if (Statements.TryGetValue(statementType, out var entry))
        {
            name = entry.Name;
            capabilities = entry.Caps;
            return true;
        }

        name = statementType.Name;
        capabilities = SoftwareCapability.None;
        return false;
    }

    public static bool TryGetFunction(
        string functionName,
        out string name,
        out SoftwareCapability capabilities)
    {
        if (Functions.TryGetValue(functionName, out var entry))
        {
            name = entry.Name;
            capabilities = entry.Caps;
            return true;
        }

        name = functionName.ToUpperInvariant();
        capabilities = SoftwareCapability.None;
        return false;
    }
}
