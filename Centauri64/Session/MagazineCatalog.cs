using System;
using System.Collections.Generic;
using System.Linq;

namespace Centauri64.Session;

/// <summary>
/// Data-driven magazine issue. Display names, cover games and copy live here
/// so working titles can change without touching progression code.
/// </summary>
public sealed class MagazineIssue
{
    public string Id { get; init; } = string.Empty;
    public int IssueNumber { get; init; }
    public string Title { get; init; } = string.Empty;
    public string CoverHeadline { get; init; } = string.Empty;
    public string Teaser { get; init; } = string.Empty;
    public string FullDescription { get; init; } = string.Empty;
    public string CoverGameId { get; init; } = string.Empty;
    public string CoverGameTitle { get; init; } = string.Empty;
    public string CoverGameDescription { get; init; } = string.Empty;
    public bool CoverGameTitleIsWorkingTitle { get; init; }
    public bool CoverTapeReady { get; init; }
    public IReadOnlyList<string> ConceptsIntroduced { get; init; } = Array.Empty<string>();
    public IReadOnlyList<FeatureId> GrantedFeatures { get; init; } = Array.Empty<FeatureId>();
    public IReadOnlyList<string> ManualSectionsUnlocked { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> PublisherUnlockIds { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ContractUnlockIds { get; init; } = Array.Empty<string>();
    public string PreviousIssueId { get; init; } = string.Empty;
    public string NextIssueId { get; init; } = string.Empty;
    public string AvailabilityRequirement { get; init; } = string.Empty;
    public bool IsMajorMilestone { get; init; }
    public string FictionalMonth { get; init; } = string.Empty;
    public string CoverAssetId { get; init; } = string.Empty;
}

/// <summary>
/// CENTAURI64 MAGAZINE — YEAR ONE / FIRST RUN.
/// Issue numbers and copy can be rearranged here without rewriting screens.
/// </summary>
public static class MagazineCatalog
{
    public static readonly MagazineIssue[] Issues = BuildYearOne();

    public static IEnumerable<string> AllIds => Issues.Select(issue => issue.Id);

    public static MagazineIssue? Find(string id) =>
        Issues.FirstOrDefault(issue =>
            issue.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public static MagazineIssue? FindByNumber(int number) =>
        Issues.FirstOrDefault(issue => issue.IssueNumber == number);

    public static MagazineIssue? Next(MagazineIssue issue) =>
        string.IsNullOrEmpty(issue.NextIssueId) ? null : Find(issue.NextIssueId);

    private static MagazineIssue[] BuildYearOne()
    {
        const string i1 = "mag_issue_01";
        const string i2 = "mag_issue_02";
        const string i3 = "mag_issue_03";
        const string i4 = "mag_issue_04";
        const string i5 = "mag_issue_05";
        const string i6 = "mag_issue_06";
        const string i7 = "mag_issue_07";
        const string i8 = "mag_issue_08";
        const string i9 = "mag_issue_09";
        const string i10 = "mag_issue_10";

        return new[]
        {
            new MagazineIssue
            {
                Id = i1,
                IssueNumber = 1,
                Title = "WELCOME TO YOUR CENTAURI64",
                CoverHeadline = "WELCOME TO YOUR CENTAURI64!",
                Teaser = "YOUR FIRST PROGRAMS START HERE.",
                FullDescription =
                    "YOU HAVE A CENTAURI64. THIS ISSUE SHOWS THAT YOU CAN MAKE IT DO THINGS TODAY — PRINT WORDS, ASK QUESTIONS, MAKE DECISIONS AND LOOP FOREVER.",
                CoverGameId = "dungeon_of_ghoule",
                CoverGameTitle = "DUNGEON OF GHOULE",
                CoverGameDescription = "A SHORT TEXT ADVENTURE BUILT FROM SIMPLE BASIC.",
                CoverGameTitleIsWorkingTitle = true,
                ConceptsIntroduced = new[]
                {
                    "PRINT", "VARIABLES", "STRINGS", "INPUT", "IF",
                    "GOTO", "GOSUB / RETURN", "FOR / NEXT", "RND",
                    "INK", "PAPER", "BORDER", "CLS"
                },
                GrantedFeatures = Array.Empty<FeatureId>(),
                ManualSectionsUnlocked = new[] { "GettingStarted", "Basic", "TextColour" },
                PublisherUnlockIds = new[] { "magazine_main" },
                ContractUnlockIds = new[] { "career_first_program" },
                NextIssueId = i2,
                IsMajorMilestone = false,
                FictionalMonth = "JAN 1986"
            },
            new MagazineIssue
            {
                Id = i2,
                IssueNumber = 2,
                Title = "GRAPHICS!",
                CoverHeadline = "GRAPHICS!",
                Teaser = "DRAW ON YOUR CENTAURI64! MAKE YOUR FIRST ARCADE GAME!",
                FullDescription =
                    "PUT PIXELS ON THE SCREEN. PLOT, LINE, RECT AND CIRCLE ARE ENOUGH TO BUILD A REAL ARCADE GAME — NO SPRITES REQUIRED.",
                CoverGameId = "lunar_rescue",
                CoverGameTitle = "LUNAR RESCUE",
                CoverGameDescription = "A LUNAR-LANDER-INSPIRED GAME USING BASIC DRAWING COMMANDS.",
                CoverGameTitleIsWorkingTitle = true,
                ConceptsIntroduced = new[]
                {
                    "PLOT", "LINE", "RECT", "CIRCLE", "BEEP", "KEY", "WAIT",
                    "SIMPLE MOVEMENT", "GAME LOOPS", "SCREEN COORDINATES"
                },
                GrantedFeatures = new[] { FeatureId.Graphics },
                ManualSectionsUnlocked = new[] { "Graphics" },
                PreviousIssueId = i1,
                NextIssueId = i3,
                AvailabilityRequirement = i1,
                IsMajorMilestone = true,
                FictionalMonth = "FEB 1986"
            },
            new MagazineIssue
            {
                Id = i3,
                IssueNumber = 3,
                Title = "SPRITES!",
                CoverHeadline = "SPRITES!",
                Teaser = "MAKE YOUR GAMES MOVE!",
                FullDescription =
                    "DRAW CHARACTERS THAT WALK, CHASE AND COLLIDE. THIS ISSUE OPENS THE SPRITE DESIGNER AND SHOWS HOW TO CONTROL THEM FROM BASIC.",
                CoverGameId = "ghost_catcher",
                CoverGameTitle = "GHOST CATCHER",
                CoverGameDescription =
                    "CATCH ANIMATED GHOSTS, AVOID HAZARDS, AND KEEP YOUR SCORE ALIVE ON A SINGLE SCREEN.",
                CoverGameTitleIsWorkingTitle = false,
                ConceptsIntroduced = new[]
                {
                    "SPRITE ARTWORK", "SPRITE POSITIONING", "MOVEMENT",
                    "ANIMATION FRAMES", "KEYBOARD-CONTROLLED SPRITES",
                    "MULTIPLE SPRITES", "COLLISION", "SIMPLE ENEMY BEHAVIOUR"
                },
                GrantedFeatures = new[] { FeatureId.Sprites },
                ManualSectionsUnlocked = new[] { "Sprites" },
                PreviousIssueId = i2,
                NextIssueId = i4,
                AvailabilityRequirement = i2,
                IsMajorMilestone = true,
                FictionalMonth = "MAR 1986"
            },
            new MagazineIssue
            {
                Id = i4,
                IssueNumber = 4,
                Title = "BUILD YOUR OWN WORLDS!",
                CoverHeadline = "BUILD YOUR OWN WORLDS!",
                Teaser = "MAPS. TILES. MAZES. YOUR BIGGEST GAMES YET.",
                FullDescription =
                    "THE MAP EDITOR BUILDS WORLD GEOMETRY. BASIC STILL CREATES THE GAMEPLAY. TILE ROOMS, COLLECT KEYS AND EXPLORE BEYOND A SINGLE SCREEN.",
                CoverGameId = "crypt_raider",
                CoverGameTitle = "CRYPT RAIDER",
                CoverGameDescription = "A TOP-DOWN MAZE ADVENTURE THROUGH LINKED ROOMS.",
                CoverGameTitleIsWorkingTitle = true,
                ConceptsIntroduced = new[]
                {
                    "TILES", "TILESETS", "MAP CONSTRUCTION", "MAP COLLISION",
                    "MULTIPLE ROOMS", "SPRITE + WORLD INTERACTION"
                },
                GrantedFeatures = new[] { FeatureId.Maps },
                ManualSectionsUnlocked = Array.Empty<string>(),
                PreviousIssueId = i3,
                NextIssueId = i5,
                AvailabilityRequirement = i3,
                IsMajorMilestone = true,
                FictionalMonth = "APR 1986"
            },
            new MagazineIssue
            {
                Id = i5,
                IssueNumber = 5,
                Title = "PICTURE THIS!",
                CoverHeadline = "PICTURE THIS!",
                Teaser = "ADD ILLUSTRATIONS TO YOUR GAMES!",
                FullDescription =
                    "PAINT IMAGES, PLACE THEM WITH IMAGE, BG AND FG, AND BUILD ATMOSPHERIC STORY GAMES AS WELL AS ARCADE ACTION.",
                CoverGameId = "haunted_house_adventure",
                CoverGameTitle = "HOUSE ON BLACKWELL HILL",
                CoverGameDescription =
                    "AN ILLUSTRATED MYSTERY. ABOUT 15-20 LOCATIONS. DISPLAY TITLE IS A WORKING PLACEHOLDER.",
                CoverGameTitleIsWorkingTitle = true,
                ConceptsIntroduced = new[]
                {
                    "IMAGE ASSETS", "IMAGE SIZES", "CATEGORIES", "FRAMES",
                    "TRANSPARENCY", "IMAGE", "BG", "FG", "ILLUSTRATED SCENES"
                },
                GrantedFeatures = new[] { FeatureId.Images },
                ManualSectionsUnlocked = new[] { "Images" },
                PreviousIssueId = i4,
                NextIssueId = i6,
                AvailabilityRequirement = i4,
                IsMajorMilestone = true,
                FictionalMonth = "MAY 1986"
            },
            new MagazineIssue
            {
                Id = i6,
                IssueNumber = 6,
                Title = "BIGGER GAMES!",
                CoverHeadline = "BIGGER GAMES!",
                Teaser = "BUILD LEVELS! CONTROL LOTS OF ENEMIES! ORGANISE YOUR CODE!",
                FullDescription =
                    "ARRAYS AND STAGE DATA LET YOU TRACK MANY ENEMIES AND BUILD REUSABLE GAME SYSTEMS. DATA / READ / RESTORE ARE PLANNED FOR A LATER BASIC PHASE.",
                CoverGameId = "toad_trouble",
                CoverGameTitle = "TOAD TROUBLE",
                CoverGameDescription =
                    "A SINGLE-SCREEN ARENA. CAST A SPELL, CLEAR THE ROOM, THEN FACE A TOUGHER STAGE.",
                CoverGameTitleIsWorkingTitle = true,
                ConceptsIntroduced = new[]
                {
                    "ARRAYS", "DATA / READ / RESTORE", "MULTIPLE ENEMIES",
                    "STAGE DATA", "REUSABLE LOGIC", "ENEMY TYPES"
                },
                GrantedFeatures = new[] { FeatureId.Arrays, FeatureId.DataStatements },
                ManualSectionsUnlocked = Array.Empty<string>(),
                PreviousIssueId = i5,
                NextIssueId = i7,
                AvailabilityRequirement = i5,
                IsMajorMilestone = true,
                FictionalMonth = "JUN 1986"
            },
            new MagazineIssue
            {
                Id = i7,
                IssueNumber = 7,
                Title = "ATTACK WAVES!",
                CoverHeadline = "ATTACK WAVES!",
                Teaser = "MAKE YOUR ALIENS SWOOP! BUILD ARCADE ATTACK PATTERNS!",
                FullDescription =
                    "FORMATIONS, WAVES AND REUSABLE MOVEMENT ROUTINES. BUILD A WHOLE ATTACKING FLEET INSTEAD OF SCRIPTING EVERY ENEMY BY HAND.",
                CoverGameId = "formation_shooter",
                CoverGameTitle = "STAR LANCE",
                CoverGameDescription =
                    "A FORMATION SHOOTER. DISPLAY TITLE IS A WORKING PLACEHOLDER.",
                CoverGameTitleIsWorkingTitle = true,
                ConceptsIntroduced = new[]
                {
                    "ENEMY FORMATIONS", "WAVES", "ATTACK PATHS",
                    "ENEMY STATE", "INCREASING DIFFICULTY", "SCORE SYSTEMS"
                },
                GrantedFeatures = Array.Empty<FeatureId>(),
                ManualSectionsUnlocked = Array.Empty<string>(),
                PreviousIssueId = i6,
                NextIssueId = i8,
                AvailabilityRequirement = i6,
                IsMajorMilestone = true,
                FictionalMonth = "JUL 1986"
            },
            new MagazineIssue
            {
                Id = i8,
                IssueNumber = 8,
                Title = "TWO CAN PLAY!",
                CoverHeadline = "TWO CAN PLAY!",
                Teaser = "WHAT IF ANOTHER CENTAURI64 COULD JOIN YOUR GAME?",
                FullDescription =
                    "HOST, JOIN AND SHARE GAME STATE BETWEEN TWO MACHINES. START WITH A SIMPLE NETWORK MATCH SO THE LINK IS THE LESSON.",
                CoverGameId = "netpong_86",
                CoverGameTitle = "NETPONG 86",
                CoverGameDescription = "A NETWORK PONG-STYLE MATCH FOR TWO CENTAURI64S.",
                CoverGameTitleIsWorkingTitle = true,
                ConceptsIntroduced = new[]
                {
                    "HOST", "JOIN", "PLAYERS", "SYNCHRONISED STATE",
                    "SENDING AND RECEIVING"
                },
                GrantedFeatures = new[] { FeatureId.Networking },
                ManualSectionsUnlocked = new[] { "Network1" },
                PreviousIssueId = i7,
                NextIssueId = i9,
                AvailabilityRequirement = i7,
                IsMajorMilestone = true,
                FictionalMonth = "AUG 1986"
            },
            new MagazineIssue
            {
                Id = i9,
                IssueNumber = 9,
                Title = "MAKE A COMMERCIAL GAME!",
                CoverHeadline = "MAKE A COMMERCIAL GAME!",
                Teaser = "TITLE SCREENS. DIFFICULTY. PACKAGING. FINISH SOMETHING REAL.",
                FullDescription =
                    "NO NEW EDITOR. COMBINE EVERYTHING YOU KNOW INTO A COMPLETE, POLISHED RELEASE — THE SORT OF TAPE A SERIOUS HOUSE MIGHT TAKE SERIOUSLY.",
                CoverGameId = "caverns_of_centauri",
                CoverGameTitle = "CAVERNS OF CENTAURI",
                CoverGameDescription = "A SUBSTANTIAL MULTI-SCREEN ARCADE ADVENTURE. DESIGN COMES LATER.",
                CoverGameTitleIsWorkingTitle = true,
                ConceptsIntroduced = new[]
                {
                    "TITLE SCREENS", "INSTRUCTIONS", "DIFFICULTY CURVES",
                    "GAME-OVER / WIN STATES", "POLISH", "METADATA", "COVER ARTWORK"
                },
                GrantedFeatures = Array.Empty<FeatureId>(),
                PublisherUnlockIds = new[] { "publisher_arcade_01" },
                PreviousIssueId = i8,
                NextIssueId = i10,
                AvailabilityRequirement = i8,
                IsMajorMilestone = true,
                FictionalMonth = "SEP 1986"
            },
            new MagazineIssue
            {
                Id = i10,
                IssueNumber = 10,
                Title = "INSIDE YOUR CENTAURI64!",
                CoverHeadline = "INSIDE YOUR CENTAURI64!",
                Teaser = "CUSTOM ASSETS. SECRET VIDEO TRICKS. PUSH YOUR MACHINE BEYOND ITS LIMITS...",
                FullDescription =
                    "YOU KNOW HOW TO USE THE MACHINE. THIS ISSUE BEGINS TO ASK HOW TO PUSH IT. CUSTOM FONTS, IMPORTED ART AND SAFE LOW-LEVEL VIDEO TRICKS — NOT ARBITRARY MACHINE CODE.",
                CoverGameId = "neon_runner",
                CoverGameTitle = "NEON RUNNER",
                CoverGameDescription = "A FAST ARCADE SHOWCASE FOR EFFECTS THAT LOOK IMPOSSIBLE ON PAPER.",
                CoverGameTitleIsWorkingTitle = true,
                ConceptsIntroduced = new[]
                {
                    "CUSTOM BITMAP FONTS", "IMPORTED IMAGES",
                    "PEEK", "POKE", "SAFE VIDEO EFFECTS"
                },
                GrantedFeatures = new[] { FeatureId.CustomAssets, FeatureId.LowLevelMachine },
                ManualSectionsUnlocked = new[] { "Hardware" },
                PreviousIssueId = i9,
                AvailabilityRequirement = i9,
                IsMajorMilestone = true,
                FictionalMonth = "OCT 1986"
            }
        };
    }
}
