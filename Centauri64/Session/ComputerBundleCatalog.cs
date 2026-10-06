using System;
using System.Collections.Generic;
using System.Linq;

namespace Centauri64.Session;

public sealed class ComputerBundle
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Price { get; init; } = string.Empty;
    public string Tagline { get; init; } = string.Empty;
    public IReadOnlyList<string> Included { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> SoftwareTapes { get; init; } = Array.Empty<string>();
}

public static class ComputerBundleCatalog
{
    public const string LegacyId = "legacy";

    public static readonly ComputerBundle[] Bundles =
    {
        new()
        {
            Id = "starter",
            Name = "STARTER PACK",
            Price = "£199",
            Tagline = "YOUR FIRST CENTAURI64.",
            Included = new[]
            {
                "CENTAURI64 COMPUTER",
                "1 JOYSTICK",
                "WELCOME TAPE",
                "1 GAME"
            },
            SoftwareTapes = new[] { "HELLO" }
        },
        new()
        {
            Id = "family",
            Name = "FAMILY PACK",
            Price = "£249",
            Tagline = "GAMES AND LEARNING FOR EVERYONE.",
            Included = new[]
            {
                "CENTAURI64 COMPUTER",
                "2 JOYSTICKS",
                "3 GAMES",
                "EDUCATIONAL SOFTWARE"
            },
            SoftwareTapes = new[] { "HELLO", "PONG", "ADVENTURE" }
        },
        new()
        {
            Id = "programmer",
            Name = "PROGRAMMER PACK",
            Price = "£279",
            Tagline = "MADE FOR PEOPLE WHO WRITE SOFTWARE.",
            Included = new[]
            {
                "CENTAURI64 COMPUTER",
                "PROGRAMMING MANUAL",
                "BLANK TAPES",
                "2 GAMES",
                "PRODUCTIVITY SOFTWARE"
            },
            SoftwareTapes = new[] { "HELLO", "PONG" }
        }
    };

    public static ComputerBundle? Find(string id) =>
        Bundles.FirstOrDefault(bundle =>
            bundle.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}
