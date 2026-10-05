using System.Collections.Generic;
using System.Linq;

using Centauri64.Analysis;
using Centauri64.Basic;

namespace Centauri64.Publishing;

/// <summary>
/// Magazines, software houses and contracts.
/// Display names are placeholder presentation data — use Ids everywhere else.
/// </summary>
public static class PublisherCatalog
{
    public static IReadOnlyList<OrganisationDefinition> Organisations { get; } =
        new[]
        {
            new OrganisationDefinition
            {
                Id = "magazine_main",
                Name = "CENTAURI USER",
                ShortName = "CENTAURI USER",
                Type = OrganisationType.Magazine,
                Tagline = "BEGINNER MAGAZINE FOR BEDROOM PROGRAMMERS",
                Description = "READER CHALLENGES AND FIRST PROGRAMS.",
                SubmissionReceivedText =
                    "YOUR TAPE HAS BEEN SENT TO\nCENTAURI USER.\n\nGOOD LUCK!",
                AcceptanceText =
                    "THANK YOU FOR SENDING US\nYOUR PROGRAM!\n\nWE'VE INCLUDED IT IN OUR\nREADER PROGRAMS SECTION.\n\nEVERY PROGRAMMER HAS TO\nSTART SOMEWHERE.\n\nKEEP PROGRAMMING!\n\nPAYMENT ENCLOSED:\n\n          {REWARD}",
                RejectionText =
                    "KEEP PRACTISING AND SEND\nIT IN AGAIN SOON."
            },
            new OrganisationDefinition
            {
                Id = "publisher_arcade_01",
                Name = "NOVABYTE SOFTWARE",
                ShortName = "NOVABYTE",
                Type = OrganisationType.SoftwareHouse,
                Tagline = "COLOURFUL ARCADE GAMES",
                Description = "ARCADE AND ACTION SOFTWARE.",
                SubmissionReceivedText =
                    "YOUR TAPE HAS BEEN SENT TO\nNOVABYTE SOFTWARE.\n\nGOOD LUCK!",
                AcceptanceText =
                    "ARCADE ACTION - JUST OUR STYLE!\nWE'D LIKE TO RELEASE YOUR GAME.\n\nPAYMENT ENCLOSED:\n\n          {REWARD}",
                RejectionText =
                    "THANKS FOR YOUR SUBMISSION.\nWE'RE LOOKING FOR SOMETHING WITH\nA LITTLE MORE ARCADE ACTION."
            },
            new OrganisationDefinition
            {
                Id = "publisher_adventure_01",
                Name = "QUILL & LANTERN",
                ShortName = "QUILL & LANTERN",
                Type = OrganisationType.SoftwareHouse,
                Tagline = "ADVENTURES AND STORIES",
                Description = "TEXT ADVENTURES AND NARRATIVE GAMES.",
                SubmissionReceivedText =
                    "YOUR TAPE HAS BEEN SENT TO\nQUILL & LANTERN.\n\nGOOD LUCK!",
                AcceptanceText =
                    "JUST OUR SORT OF THING!\nWE'D LIKE TO FEATURE YOUR\nADVENTURE IN OUR NEXT RELEASE.\n\nPAYMENT ENCLOSED:\n\n          {REWARD}",
                RejectionText =
                    "WE SPECIALISE IN TEXT ADVENTURES\nAND STORY GAMES."
            },
            new OrganisationDefinition
            {
                Id = "publisher_puzzle_01",
                Name = "MICROMOTH SOFTWARE",
                ShortName = "MICROMOTH",
                Type = OrganisationType.SoftwareHouse,
                Tagline = "PUZZLES AND ODD EXPERIMENTS",
                Description = "PUZZLE, STRATEGY AND EXPERIMENTAL SOFTWARE.",
                SubmissionReceivedText =
                    "YOUR TAPE HAS BEEN SENT TO\nMICROMOTH SOFTWARE.\n\nGOOD LUCK!",
                AcceptanceText =
                    "WHAT A CURIOUS LITTLE PROGRAM!\nWE'D LOVE TO PUBLISH IT.\n\nPAYMENT ENCLOSED:\n\n          {REWARD}",
                RejectionText =
                    "INTERESTING, BUT NOT QUITE OUR\nUSUAL PUZZLE OR STRATEGY FARE."
            },
            new OrganisationDefinition
            {
                Id = "publisher_technical_01",
                Name = "VECTOR CROWN",
                ShortName = "VECTOR CROWN",
                Type = OrganisationType.SoftwareHouse,
                Tagline = "TECHNICAL AND NETWORK GAMES",
                Description = "AMBITIOUS CENTAURI64 SOFTWARE.",
                SubmissionReceivedText =
                    "YOUR TAPE HAS BEEN SENT TO\nVECTOR CROWN SOFTWARE.\n\nGOOD LUCK!",
                AcceptanceText =
                    "THANK YOU FOR SENDING US\n\"{TITLE}\".\n\nWE'RE PLEASED TO SAY WE'D\nLIKE TO PUBLISH IT FOR THE\nCENTAURI64.\n\nPAYMENT ENCLOSED:\n\n          {REWARD}\n\nWE LOOK FORWARD TO SEEING\nWHAT YOU WRITE NEXT.",
                RejectionText =
                    "WE'RE AFTER MORE AMBITIOUS\nTECHNICAL SOFTWARE. KEEP GOING!"
            }
        };

    public static IReadOnlyList<SubmissionContract> Contracts { get; } =
        new[]
        {
            new SubmissionContract
            {
                Id = "career_first_program",
                OrganisationId = "magazine_main",
                Title = "YOUR FIRST PROGRAM",
                Subtitle = "READER CHALLENGE",
                Description =
                    "HAVE YOU WRITTEN YOUR FIRST\nCENTAURI64 PROGRAM?\n\nWE WANT TO SEE IT!\n\nIT DOESN'T NEED GRAPHICS,\nSOUND OR FANCY SPRITES.\n\nIF YOU'VE MADE SOMETHING RUN,\nSEND US YOUR TAPE.",
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
                Id = "career_interactive_program",
                OrganisationId = "magazine_main",
                Title = "MAKE IT INTERACTIVE",
                Subtitle = "READER CHALLENGE",
                Description =
                    "CAN YOU WRITE A PROGRAM\nTHAT RESPONDS TO THE PLAYER?",
                RewardPennies = 350,
                Repeatable = false,
                PrerequisiteContractIds = new[] { "career_first_program" },
                Requirements = new SubmissionRequirements
                {
                    ProgramValid = true,
                    RequiredCapabilities = SoftwareCapability.Input,
                    RequiresCustomCover = false
                }
            },
            new SubmissionContract
            {
                Id = "career_graphical_program",
                OrganisationId = "magazine_main",
                Title = "GET GRAPHICAL!",
                Subtitle = "READER CHALLENGE",
                Description =
                    "USE THE SCREEN!\n\nDRAW WITH GRAPHICS COMMANDS\nOR PLACE A SPRITE.",
                RewardPennies = 500,
                Repeatable = false,
                PrerequisiteContractIds = new[] { "career_interactive_program" },
                Requirements = new SubmissionRequirements
                {
                    ProgramValid = true,
                    AnyOfCapabilities = new[]
                    {
                        SoftwareCapability.Graphics,
                        SoftwareCapability.Sprites
                    },
                    RequiresCustomCover = false
                }
            },
            new SubmissionContract
            {
                Id = "career_first_game",
                OrganisationId = "magazine_main",
                Title = "YOUR FIRST GAME",
                Subtitle = "READER CHALLENGE",
                Description =
                    "A REAL GAME WITH INPUT,\nGRAPHICS OR SPRITES,\nAND A CASSETTE COVER.",
                RewardPennies = 750,
                Repeatable = false,
                PrerequisiteContractIds = new[] { "career_graphical_program" },
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
                Id = "arcade_first_game",
                OrganisationId = "publisher_arcade_01",
                Title = "ARCADE GAMES WANTED",
                Subtitle = "GAMES WANTED",
                Description =
                    "WE WANT FUN ARCADE ACTION\nFOR THE CENTAURI64.\n\nGRAPHICS OR SPRITES WELCOME.",
                RewardPennies = 1250,
                Repeatable = false,
                PrerequisiteContractIds = new[] { "career_first_game" },
                Requirements = new SubmissionRequirements
                {
                    ProgramValid = true,
                    RequiredKind = TapeKind.Game,
                    AllowedGenres = new[]
                    {
                        GameGenre.Arcade,
                        GameGenre.Action,
                        GameGenre.Shooter,
                        GameGenre.Platform,
                        GameGenre.Sports
                    },
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
                Id = "arcade_sprite_game",
                OrganisationId = "publisher_arcade_01",
                Title = "SPRITE SPECTACULAR",
                Subtitle = "GAMES WANTED",
                Description =
                    "SHOW US WHAT YOU CAN DO\nWITH HARDWARE SPRITES.",
                RewardPennies = 1750,
                Repeatable = false,
                PrerequisiteContractIds = new[] { "arcade_first_game" },
                Requirements = new SubmissionRequirements
                {
                    ProgramValid = true,
                    RequiredKind = TapeKind.Game,
                    RequiredCapabilities =
                        SoftwareCapability.Input | SoftwareCapability.Sprites,
                    RequiresCustomCover = true
                }
            },
            new SubmissionContract
            {
                Id = "adventure_first_adventure",
                OrganisationId = "publisher_adventure_01",
                Title = "AUTHORS WANTED!",
                Subtitle = "AUTHORS WANTED",
                Description =
                    "TEXT ADVENTURES AND STORIES\nWANTED.\n\nGRAPHICS ARE NOT REQUIRED.",
                RewardPennies = 1250,
                Repeatable = false,
                PrerequisiteContractIds = new[] { "career_first_game" },
                Requirements = new SubmissionRequirements
                {
                    ProgramValid = true,
                    RequiredKind = TapeKind.Game,
                    RequiredGenre = GameGenre.Adventure,
                    RequiredCapabilities =
                        SoftwareCapability.Text |
                        SoftwareCapability.Input |
                        SoftwareCapability.Strings,
                    RequiresCustomCover = true
                }
            },
            new SubmissionContract
            {
                Id = "adventure_1000_lines",
                OrganisationId = "publisher_adventure_01",
                Title = "ADVENTURE ON A BUDGET",
                Subtitle = "AUTHORS WANTED",
                Description =
                    "CAN YOU CREATE A COMPLETE\nTEXT ADVENTURE IN FEWER\nTHAN 1000 LINES OF BASIC?",
                RewardPennies = 2500,
                Repeatable = false,
                PrerequisiteContractIds = new[] { "adventure_first_adventure" },
                Requirements = new SubmissionRequirements
                {
                    ProgramValid = true,
                    RequiredKind = TapeKind.Game,
                    RequiredGenre = GameGenre.Adventure,
                    RequiredCapabilities =
                        SoftwareCapability.Text |
                        SoftwareCapability.Input |
                        SoftwareCapability.Strings,
                    RequiresCustomCover = true,
                    MaximumLineCount = 1000
                }
            },
            new SubmissionContract
            {
                Id = "puzzle_first_game",
                OrganisationId = "publisher_puzzle_01",
                Title = "PUZZLE GAMES WANTED",
                Subtitle = "PROGRAMMERS WANTED",
                Description =
                    "PUZZLES, STRATEGY AND\nCURIOUS LITTLE EXPERIMENTS.",
                RewardPennies = 1000,
                Repeatable = false,
                PrerequisiteContractIds = new[] { "career_first_game" },
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
                Id = "technical_network_game",
                OrganisationId = "publisher_technical_01",
                Title = "NETWORK GAMES WANTED",
                Subtitle = "SOFTWARE COMMISSION",
                Description =
                    "WE'RE LOOKING FOR THE NEXT\nGENERATION OF CENTAURI64\nSOFTWARE.\n\nTWO PLAYER NETWORK TITLES\nWANTED.",
                RewardPennies = 2500,
                Repeatable = false,
                PrerequisiteContractIds = new[] { "career_first_game" },
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

    public static OrganisationDefinition? GetOrganisation(string id) =>
        Organisations.FirstOrDefault(org => org.Id == id);

    public static OrganisationDefinition? GetPublisher(string id) =>
        GetOrganisation(id);

    public static SubmissionContract? GetContract(string id) =>
        Contracts.FirstOrDefault(contract => contract.Id == id);

    public static IEnumerable<SubmissionContract> ContractsFor(string organisationId) =>
        Contracts.Where(contract => contract.OrganisationId == organisationId);

    public static IEnumerable<SoftwarePublisher> Publishers =>
        Organisations.Select(org => new SoftwarePublisher
        {
            Id = org.Id,
            Name = org.Name,
            ShortDescription = org.Tagline,
            AcceptanceMessage = org.AcceptanceBody,
            RejectionMessage = org.RejectionBody,
            IsMagazine = org.Type == OrganisationType.Magazine
        });
}
