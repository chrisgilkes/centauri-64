# Career progression (as implemented)

How Bedroom Coder careers, magazines, authoring features, publishers, and classifieds currently work. Source of truth for **machine capabilities** remains [`CENTAURI64_SPEC.md`](CENTAURI64_SPEC.md). Submission/mail timing is also described in [`CAREER-LOOP.md`](CAREER-LOOP.md).

This file describes **curriculum and market availability**, not hardware.

---

## 1. Two play experiences

Source: `Session/PlayExperience.cs`, `Session/GameSession.cs`

| Experience | Career | Authoring gates |
|------------|--------|-----------------|
| **Bedroom Coder** | One of three save slots | `FeatureAvailability.FromUnlocks` — locked statements/tools throw until the matching magazine is owned |
| **Hardcore Coder** | No career slot | `FeatureAvailability.AllReleased()` — full machine from the start |

Hardcore magazines open as a **reference library** (`MagazinesScreen.Open(referenceLibrary: true)`). That path does **not** call `MagazineProgression.EnsureStartingIssue` and does **not** write career state. Classifieds (`C`) are Bedroom-only.

Running a tape that already uses sprites (etc.) is **not** blocked by the player’s authoring unlocks when that tape is executed as existing software. Gates run in `Interpreter` for the **current session** while the player authors/runs code (`FeatureGate` / `EnsureAuthoringFeature`).

---

## 2. Career slots and save data

Sources: `Session/CareerRepository.cs`, `Session/CareerState.cs`, `Progression/PlayerProgress.cs`

- Three slots: `%LocalAppData%\Centauri64\Careers\slot1.json` … `slot3.json`
- Legacy `%LocalAppData%\Centauri64\player.json` migrates into **slot 1** with all Year One magazines owned (`CareerState.FromLegacy` → `OwnThrough(10)`)
- Identity: name, bundle id, rank (`HOBBYIST`)
- Progression: owned magazines, unlocked features, unlocked publishers, unlocked contract ids, `PlayerProgress` (cash, submissions, mail)

**Reset Career** (settings) clears progression and re-grants Issue #1; it does **not** delete tapes. **DELETE ALL USER DATA** remains disabled.

On load, `MigrateMagazineFields` re-runs `OwnThrough(highest owned issue)` so publisher/feature grants from `MagazineCatalog` stay in sync if catalogue mappings change.

---

## 3. How magazines are owned

Sources: `Session/MagazineCatalog.cs`, `Session/MagazineProgression.cs`, `Publishing/CareerService.TryPurchaseIssue`

Year One is ten data-driven issues (`mag_issue_01` … `mag_issue_10`). Display titles, teasers, cover-tape **names**, prices, and fictional months live on the catalogue — do not hardcode them in UI.

Issue #1 is included with a new Bedroom career (`EnsureStartingIssue` / `OwnThrough(1)`, `PricePennies = 0`).

Issues #2–#10 are **bought** with career cash when they are **on sale**. There is **no** real-world calendar, `DateTime.Now`, or idle wait. JAN 1986 → FEB 1986 is career state (`CalendarIssueNumber`).

Owning an issue (`OwnIssue` / `OwnThrough` / purchase) is **idempotent** and may grant:

- `GrantedFeatures` → `CareerState.UnlockedFeatures` → `GameSession.RefreshFeatures`
- `ManualSectionsUnlocked` → Programming Manual sections
- `PublisherUnlockIds` → classifieds market
- `ContractUnlockIds` → stored on the career (**not currently used** to show/hide classifieds; see §6)
- Cover tape into My Software **only if** `CoverTapeReady` is true **and** `CoverGameId` is set

**All Year One cover tapes currently have `CoverTapeReady = false`.** Previewing a cover game on the shelf does not grant software.

Purchase (`MagazineProgression.Purchase`): issue must be `OnSale`, not already owned, cash ≥ `PricePennies`. Cash is deducted once; cash cannot go negative. Hardcore never buys magazines.

### How later issues become ON SALE

Issue N+1 is on sale when issue N is **owned** and, if `OnSaleAfterContractId` is set, that contract is **completed** (mail opened). If the contract id is empty, owning the previous issue is enough.

| Path | What happens |
|------|----------------|
| New Bedroom career | Issue #1 owned; #2 is NEXT MONTH until the January milestone |
| Complete the milestone contract | Fictional month advances; next issue NOW ON SALE |
| Pay `PricePennies` (Y on the issue page) | `OwnIssue` + grants; persist slot |
| Settings debug | ADVANCE MAGAZINE / OWN THROUGH ISSUE 3 / OWN YEAR ONE — grants **without** charging |
| Legacy migration | Owns issues 1–10 without charging |

Shelf states (`MagazineIssueState`): **Owned**, **OnSale**, **ComingNext**, **ComingLater**. Hardcore reference library treats every issue as Owned.

---

## 4. Authoring features vs the machine

Sources: `Session/FeatureId.cs`, `Session/FeatureGate.cs`, `Basic/Interpreter.cs`, `Game1.cs`

The **machine** may already implement a command. Bedroom **authoring** may still reject it until the magazine grant.

| `FeatureId` | Issue | Gate actually enforced? | What it gates |
|-------------|------:|-------------------------|---------------|
| `CoreBasic` | 1 (always) | Always on | PRINT, INPUT, control flow, INK/PAPER, MODE, BEEP, KEY, tapes, … |
| `Graphics` | 2 | **Yes** | PLOT, LINE, RECT, CIRCLE |
| `Sprites` | 3 | **Yes** | Sprite statements, `COLLIDE`, `ANIMPLAYING`; F5 Sprite Designer |
| `Maps` | 4 | **Yes** | TDEF/MAP/LOADMAP/CAMERA/CAMOFF, `TILEAT`; F6 Map Editor |
| `Images` | 5 | **Yes** | IMAGE, BG, FG; F7 Image Editor |
| `Arrays` | 6 | **No extra interpreter gate** | `DIM` already exists; flag is stored for curriculum |
| `DataStatements` | 6 | **Planned** | DATA/READ/RESTORE are **not** BASIC yet (`CENTAURI64_SPEC.md`) |
| `Networking` | 8 | **Yes** | NET statements and NET* functions |
| `CustomAssets` | 10 | **Planned** | No machine API; Image Editor import is a tool |
| `LowLevelMachine` | 10 | **Planned** | PEEK/POKE/SYS are **not** BASIC yet |

`BORDER` is named in Issue #1 concept copy only. It is **not** a statement and is **not** a `FeatureId`.

---

## 5. Year One magazine grants

Exact copy: `MagazineCatalog.BuildYearOne`. Grants below are the **ids**, not marketing text.

| # | Fictional month | Price | On sale after | Features granted | Publishers granted | Notes |
|---|-----------------|------:|---------------|------------------|--------------------|--------|
| 1 | JAN 1986 | included | (new career) | (Core Basic already on) | `magazine_main` (Centauri User) | Stores `career_first_program` |
| 2 | FEB 1986 | £1.25 | `career_interactive_program` | `Graphics` | `publisher_arcade_01` (NovaByte) | |
| 3 | MAR 1986 | £1.25 | `career_graphical_program` | `Sprites` | — | |
| 4 | APR 1986 | £1.50 | `career_first_game` | `Maps` | `publisher_puzzle_01` (MicroMoth) | |
| 5 | MAY 1986 | £1.50 | previous issue owned | `Images` | `publisher_adventure_01` (Quill & Lantern) | No extra reader-challenge gate yet |
| 6 | JUN 1986 | £1.75 | previous issue owned | `Arrays`, `DataStatements` | — | DATA/READ/RESTORE not implemented |
| 7 | JUL 1986 | £1.75 | previous issue owned | — | — | Unlocks NovaByte “Sprite Spectacular” floor |
| 8 | AUG 1986 | £2.00 | previous issue owned | `Networking` | `publisher_technical_01` (Vector Crown) | |
| 9 | SEP 1986 | £2.00 | previous issue owned | — | — | |
| 10 | OCT 1986 | £2.25 | previous issue owned | `CustomAssets`, `LowLevelMachine` | — | PEEK/POKE/SYS not implemented |

Cover-tape **titles** (still not granted as software): Dungeon of Ghoule, Lunar Rescue, Ghost Catcher, Castle Creator, Hell House, Toad Trouble, Star Lance, Netpong 86, Caverns of Centauri, Neon Runner — always read from catalogue fields.

---

## 6. When a classified advert is visible

Sources: `Publishing/CareerService.cs`, `Publishing/PublisherCatalog.cs`

A contract appears in Classifieds only if **all** of the following hold:

1. **Prerequisites** — every `PrerequisiteContractIds` entry is **completed** (mail opened / payment claimed). Until then `GetAvailability` is `Locked` and the advert is hidden.
2. **Publisher on the scene** — `UnlockedPublisherIds` contains the organisation, **or** highest owned issue number ≥ `OrganisationDefinition.AvailableFromIssue`.
3. **Issue floor** — if `SubmissionContract.AvailableFromIssue` &gt; 0, highest owned issue must be ≥ that number.

`CareerState.UnlockedContractIds` is **written** when magazines are owned and **not read** by `GetVisibleContracts`. Visibility is publisher + issue floor + completed prerequisites.

Completed and in-flight adverts still list if they remain on the market (stamps: FILLED / UNDER REVIEW / SUBMITTED). Repeatable is currently `false` on all catalogue contracts.

Presentation split (not a different backend):

- Organisation `Type.Magazine` (`magazine_main`) → **Reader Challenges**
- `Type.SoftwareHouse` → **Software Wanted**

---

## 7. Publishers and contracts (Year One mapping)

Organisation `AvailableFromIssue` matches the magazine grant in §5.

| Organisation id | Display name (placeholder) | From issue | Advert style (print only) |
|-----------------|----------------------------|-----------:|---------------------------|
| `magazine_main` | Centauri User | 1 | Plain |
| `publisher_arcade_01` | NovaByte Software | 2 | Bold |
| `publisher_puzzle_01` | MicroMoth Software | 4 | Plain |
| `publisher_adventure_01` | Quill & Lantern | 5 | Ornate |
| `publisher_technical_01` | Vector Crown | 8 | Technical |

Houses **accumulate**. Owning Issue #8 does not remove NovaByte.

### Reader Challenges (`magazine_main`)

| Contract id | Title | Reward | Prerequisites | `AvailableFromIssue` | Taught by |
|-------------|-------|-------:|---------------|---------------------:|-----------|
| `career_first_program` | YOUR FIRST PROGRAM | £2.00 | — | 1 | Issue #1 Core Basic |
| `career_interactive_program` | MAKE IT INTERACTIVE | £3.50 | first program | 1 | Issue #1 KEY/INPUT |
| `career_graphical_program` | GET GRAPHICAL! | £5.00 | interactive | **2** | Issue #2 Graphics (analyser: Graphics **or** Sprites) |
| `career_first_game` | YOUR FIRST GAME | £7.50 | graphical | **3** | Issue #3; still accepts Graphics or Sprites + cover |

GET GRAPHICAL! must not appear before Issue #2 is **owned** (Graphics grant). YOUR FIRST GAME waits until Issue #3 is owned.

### Software Wanted

| Contract id | House | Title | Reward | Prerequisites | From issue |
|-------------|-------|-------|-------:|---------------|-----------:|
| `arcade_first_game` | NovaByte | ARCADE GAMES WANTED | £12.50 | `career_first_game` | 2 |
| `arcade_sprite_game` | NovaByte | SPRITE SPECTACULAR | £17.50 | `arcade_first_game` | **7** |
| `puzzle_first_game` | MicroMoth | PUZZLE GAMES WANTED | £10.00 | `career_first_game` | 4 |
| `adventure_first_adventure` | Quill & Lantern | AUTHORS WANTED! | £12.50 | `career_first_game` | 5 |
| `adventure_1000_lines` | Quill & Lantern | ADVENTURE ON A BUDGET | £25.00 | first adventure | 5 |
| `technical_network_game` | Vector Crown | NETWORK GAMES WANTED | £25.00 | `career_first_game` | 8 |

So a fresh career with only Issue #1 sees **Reader Challenges** YOUR FIRST PROGRAM then MAKE IT INTERACTIVE. GET GRAPHICAL! appears after buying Issue #2. YOUR FIRST GAME appears after buying Issue #3. NovaByte cannot appear until Issue #2 is owned **and** `career_first_game` is completed.

`ArrivalNotice` strings exist on software houses for future copy. They are **not** mailed in this phase.

---

## 8. Guaranteed magazine economy

Reader-challenge pay (claimed mail): £2.00 + £3.50 + £5.00 + £7.50 = **£18.00**.

Issues #2–#10 prices: £1.25 × 2 + £1.50 × 2 + £1.75 × 2 + £2.00 × 2 + £2.25 = **£15.25**.

Required path (no optional Software Wanted):

| After | Cash | Next magazine |
|-------|-----:|---------------|
| Issue #1 + both January challenges | £5.50 | #2 £1.25 |
| Buy #2 + GET GRAPHICAL! | £9.25 | #3 £1.25 |
| Buy #3 + YOUR FIRST GAME | £15.50 | #4 £1.50 |
| Buy #4–#10 in sequence | £2.75 left after #10 | — |

Nothing else currently spends career cash, so this path cannot go negative. **If a shop or other sink is added later without a repeatable small job, a soft lock becomes possible.** Optional publisher contracts are extra income, not required.

Issues #5–#10 go on sale as soon as the previous issue is owned (no maps/images/network reader challenge yet). That is a curriculum-gap, not an economy gap.

---

## 9. Submission and pay (unchanged loop)

See [`CAREER-LOOP.md`](CAREER-LOOP.md). Short version:

Bedroom Magazines → Classifieds → advert → `S` → analyse tape → `Y` send → **no cash yet** → return to bedroom (`DeliverPendingResponses`) → Notice Board mail → open letter → cash once.

Analyser/evaluator/requirements were not redesigned for the magazine print UI.

---

## 10. Debug (Bedroom settings)

| Action | Effect |
|--------|--------|
| ADVANCE MAGAZINE (DEV) | Own the next issue (1–10) |
| OWN THROUGH ISSUE 3 (DEV) | Issues 1–3 (Graphics + Sprites) |
| OWN YEAR ONE (DEV) | Issues 1–10 |
| GRANT SPRITES / MAPS / IMAGES (DEV) | Feature flag only (does not own magazines or publishers) |

Use magazine debug when testing classifieds, because publisher presence follows **owned issue number**, not a lone feature grant.

---

## 11. Known gaps (do not treat as hardware)

- Issues #5–#10 have no dedicated reader-challenge milestone (on sale when previous issue is owned).
- `UnlockedContractIds` is unused for visibility.
- Issue #1 concept list includes `BORDER` (not implemented).
- Issue #6 names DATA/READ/RESTORE as planned BASIC.
- Issue #10 names PEEK/POKE and custom fonts as planned.
- Cover-tape games are titles in data, not granted tapes.
- Rank does not currently change with progression (`CareerRanks.Hobbyist` only).
- No cash sink besides magazines yet; a future shop needs a fallback earning path.
