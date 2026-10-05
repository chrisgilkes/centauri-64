# Software Analyser

Centauri64 can inspect a saved tape and report **factual** information about what it contains and which BASIC techniques it uses.

It does **not** score quality, fun, or artwork taste.

## Architecture

```text
BASIC PROGRAM + TAPE ASSETS
             |
             v
      SoftwareAnalyser
             |
             v
      SoftwareAnalysis
             |
     +-------+--------+-----------+
     |                |           |
 Magazines       Publishers   CentauriNet (later)
```

Key types:

| Type | Role |
|------|------|
| `Analysis/CapabilityCatalog` | Maps statements/functions → capabilities |
| `Analysis/SoftwareAnalyser` | Parses listing + assets + cover |
| `Analysis/SoftwareAnalysis` | Immutable factual result |
| `Publishing/SubmissionRequirements` | Contract rules |
| `Publishing/SubmissionEvaluator` | Requirements vs analysis + metadata |
| `Publishing/PublisherCatalog` | Software houses and contracts |
| `Progression/PlayerProgress` | Cash (pennies) + completed contracts |

## Capabilities

When you add a new BASIC command or function, register it in `CapabilityCatalog`.

Current capabilities:

```text
Text, Input, Graphics, Sprites, Animation,
Sound, Maps, Strings, Random, Networking, Camera
```

Future values can include `Backgrounds`, `Parallax`, `Music` without redesigning `SoftwareAnalysis`.

## Cover analysis

Covers are stored as an optional `{name}.cover` sidecar. The game only writes that
file when the player saves artwork (`SaveTapeCover`); clearing art deletes the file.

| State | `HasCover` | `HasCustomCover` |
|-------|------------|------------------|
| No `.cover` file | NO | NO |
| Saved blank/default (all colour 0) | YES | NO |
| Saved art below threshold (&lt; 12 pixels) | YES | NO |
| Saved art ≥ 12 non-zero pixels | YES | YES |

- Blank cover colour is `0`.
- Pixel metrics (`CoverChangedPixelCount`, coverage, colour count) describe only the
  saved cover asset — never cassette UI chrome or generated placeholder art.
- Coverage is fractional (e.g. `0.45%`) so small legitimate covers are not shown as `0%`.
- Threshold / colour count are factual only — never an art-quality score.

## Metadata vs analysis

- **Metadata** (`TapeLabel`): what the author claims (Kind, Genre, Players, …).
- **Analysis**: what Centauri64 can see in the listing and assets.

Submission checks may compare them (e.g. Players = Network but no NET commands) without rewriting the label.

## Testing

1. At `READY.` type `ANALYSE` (or `ANALYZE`) for the loaded program.
2. In My Software press `A` for a SOFTWARE REPORT on the selected tape.
3. Headless smoke checks: `dotnet run --project Centauri64 -- --verify-analyser`

Both UI paths call the same `SoftwareAnalyser` — do not duplicate analysis logic.

## Adding a publisher/contract

Edit `Publishing/PublisherCatalog.cs`:

1. Add a `SoftwarePublisher` entry.
2. Add one or more `SubmissionContract` entries with `SubmissionRequirements`.
3. Keep UI free of `if (publisher == "NovaByte")` checks.

## Progression

`%LocalAppData%\Centauri64\player.json` stores:

- `CashPennies`
- `CompletedContractIds` (non-repeatable rewards)
- `PendingRewards` (accepted work awaiting mail/payment UI)

Rewards are **not** paid instantly on submit — pending rewards are for the future mail loop.
