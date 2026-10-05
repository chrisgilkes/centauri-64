using System.Collections.Generic;
using System.Linq;

using Centauri64.Analysis;
using Centauri64.Basic;

namespace Centauri64.Publishing;

/// <summary>
/// Fictional magazines and software houses plus their contracts.
/// Add new publishers/contracts here — do not hard-code checks in UI.
/// </summary>
public static class PublisherCatalog
{
    public static IReadOnlyList<SoftwarePublisher> Publishers { get; } =
        new[]
        {
            new SoftwarePublisher
            {
                Id = "centauri-user",
                Name = "CENTAURI USER",
                IsMagazine = true,
                ShortDescription = "BEGINNER MAGAZINE FOR BEDROOM PROGRAMMERS",
                AcceptanceMessage = "CONGRATULATIONS!\nYOUR PROGRAM WILL APPEAR IN\nTHE NEXT ISSUE.",
                RejectionMessage = "KEEP PRACTISING AND SEND\nIT IN AGAIN SOON."
            },
            new SoftwarePublisher
            {
                Id = "novabyte",
                Name = "NOVABYTE SOFTWARE",
                ShortDescription = "COLOURFUL ARCADE GAMES",
                AcceptanceMessage = "ARCADE ACTION - JUST OUR STYLE!\nWE'D LIKE TO RELEASE YOUR GAME.",
                RejectionMessage = "THANKS FOR YOUR SUBMISSION.\nWE'RE LOOKING FOR SOMETHING WITH\nA LITTLE MORE ARCADE ACTION.\nTRY QUILL & LANTERN FOR ADVENTURES."
            },
            new SoftwarePublisher
            {
                Id = "quill-lantern",
                Name = "QUILL & LANTERN",
                ShortDescription = "ADVENTURES AND STORIES",
                AcceptanceMessage = "JUST OUR SORT OF THING!\nWE'D LIKE TO FEATURE YOUR\nADVENTURE IN OUR NEXT RELEASE.",
                RejectionMessage = "WE SPECIALISE IN TEXT ADVENTURES\nAND STORY GAMES. NOVABYTE MAY\nSUIT ARCADE TITLES BETTER."
            },
            new SoftwarePublisher
            {
                Id = "micromoth",
                Name = "MICROMOTH SOFTWARE",
                ShortDescription = "PUZZLES AND ODD EXPERIMENTS",
                AcceptanceMessage = "WHAT A CURIOUS LITTLE PROGRAM!\nWE'D LOVE TO PUBLISH IT.",
                RejectionMessage = "INTERESTING, BUT NOT QUITE OUR\nUSUAL PUZZLE OR STRATEGY FARE."
            },
            new SoftwarePublisher
            {
                Id = "vector-crown",
                Name = "VECTOR CROWN",
                ShortDescription = "TECHNICAL AND NETWORK GAMES",
                AcceptanceMessage = "IMPRESSIVE WORK.\nVECTOR CROWN WOULD LIKE TO\nPUBLISH YOUR TITLE.",
                RejectionMessage = "WE'RE AFTER MORE AMBITIOUS\nTECHNICAL SOFTWARE. KEEP GOING!"
            }
        };

    public static IReadOnlyList<SubmissionContract> Contracts { get; } =
        new[]
        {
            new SubmissionContract
            {
                Id = "cu-first-program",
                PublisherId = "centauri-user",
                Title = "YOUR FIRST PROGRAM",
                Summary = "SAVE AND SUBMIT ANY WORKING PROGRAM.",
                RewardPennies = 200,
                Repeatable = false,
                Requirements = new SubmissionRequirements
                {
                    ProgramValid = true,
                    RequiresCustomCover = false
                }
            },
            new SubmissionContract
            {
                Id = "cu-interactive",
                PublisherId = "centauri-user",
                Title = "MAKE IT INTERACTIVE",
                Summary = "USE KEYBOARD INPUT IN YOUR PROGRAM.",
                RewardPennies = 350,
                Repeatable = false,
                Requirements = new SubmissionRequirements
                {
                    ProgramValid = true,
                    RequiredCapabilities = SoftwareCapability.Input,
                    RequiresCustomCover = false
                }
            },
            new SubmissionContract
            {
                Id = "cu-first-game",
                PublisherId = "centauri-user",
                Title = "YOUR FIRST GAME",
                Summary = "A GAME WITH INPUT, GRAPHICS OR SPRITES, AND A COVER.",
                RewardPennies = 750,
                Repeatable = false,
                Requirements = new SubmissionRequirements
                {
                    ProgramValid = true,
                    RequiredKind = TapeKind.Game,
                    RequiredCapabilities = SoftwareCapability.Input,
                    AnyOfCapabilities = new[]
                    {
                        SoftwareCapability.Graphics,
                        SoftwareCapability.Sprites
                    },
                    RequiresCustomCover = true
                }
            },
            new SubmissionContract
            {
                Id = "novabyte-arcade",
                PublisherId = "novabyte",
                Title = "ARCADE GAMES WANTED",
                Summary = "GAMES WITH INPUT AND GRAPHICS OR SPRITES.",
                RewardPennies = 1250,
                Repeatable = false,
                Requirements = new SubmissionRequirements
                {
                    ProgramValid = true,
                    RequiredKind = TapeKind.Game,
                    RequiredCapabilities = SoftwareCapability.Input,
                    AnyOfCapabilities = new[]
                    {
                        SoftwareCapability.Graphics,
                        SoftwareCapability.Sprites
                    },
                    RequiresCustomCover = true
                }
            },
            new SubmissionContract
            {
                Id = "quill-adventures",
                PublisherId = "quill-lantern",
                Title = "ADVENTURES WANTED",
                Summary = "TEXT ADVENTURES WITH STRINGS AND INPUT.",
                RewardPennies = 1250,
                Repeatable = false,
                Requirements = new SubmissionRequirements
                {
                    ProgramValid = true,
                    RequiredKind = TapeKind.Game,
                    RequiredGenre = GameGenre.Adventure,
                    RequiredCapabilities =
                        SoftwareCapability.Strings | SoftwareCapability.Input,
                    RequiresCustomCover = true
                }
            },
            new SubmissionContract
            {
                Id = "micromoth-puzzle",
                PublisherId = "micromoth",
                Title = "PUZZLE GAMES WANTED",
                Summary = "PUZZLE GAMES WITH PLAYER INPUT.",
                RewardPennies = 1000,
                Repeatable = false,
                Requirements = new SubmissionRequirements
                {
                    ProgramValid = true,
                    RequiredKind = TapeKind.Game,
                    RequiredGenre = GameGenre.Puzzle,
                    RequiredCapabilities = SoftwareCapability.Input,
                    RequiresCustomCover = true
                }
            },
            new SubmissionContract
            {
                Id = "vector-network",
                PublisherId = "vector-crown",
                Title = "NETWORK GAMES WANTED",
                Summary = "TWO-PLAYER NETWORK GAMES.",
                RewardPennies = 2500,
                Repeatable = false,
                Requirements = new SubmissionRequirements
                {
                    ProgramValid = true,
                    RequiredKind = TapeKind.Game,
                    RequiredPlayers = TapePlayers.TwoNetwork,
                    RequiredCapabilities =
                        SoftwareCapability.Input | SoftwareCapability.Networking,
                    AnyOfCapabilities = new[]
                    {
                        SoftwareCapability.Graphics,
                        SoftwareCapability.Sprites
                    },
                    RequiresCustomCover = true
                }
            }
        };

    public static SoftwarePublisher? GetPublisher(string id) =>
        Publishers.FirstOrDefault(publisher => publisher.Id == id);

    public static SubmissionContract? GetContract(string id) =>
        Contracts.FirstOrDefault(contract => contract.Id == id);

    public static IEnumerable<SubmissionContract> ContractsFor(string publisherId) =>
        Contracts.Where(contract => contract.PublisherId == publisherId);
}
