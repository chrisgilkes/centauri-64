# Career Loop V1

Magazine ownership, authoring features, and when publishers/contracts appear: [`CAREER.md`](CAREER.md).

The bedroom programmer **submission** loop sits on top of the Software Analyser.

```text
Magazines / Notice Board
        ↓
SubmissionContract (stable Id)
        ↓
SoftwareAnalyser → SoftwareAnalysis
        ↓
SubmissionEvaluator (+ TapeLabel metadata)
        ↓
CareerService.TrySubmit → Pending
        ↓
Return to Bedroom → DeliverPendingResponses → Mail
        ↓
Open mail → claim payment once → CompletedContractIds
```

## Stable IDs

Organisation display names are placeholders. Progression uses Ids only:

| Id | Placeholder name |
|----|------------------|
| `magazine_main` | Centauri User |
| `publisher_arcade_01` | NovaByte Software |
| `publisher_adventure_01` | Quill & Lantern |
| `publisher_puzzle_01` | MicroMoth Software |
| `publisher_technical_01` | Vector Crown |

Contracts use Ids such as `career_first_program`, `technical_network_game`.

## Player flow

1. Bedroom `[4] MAGAZINES` — browse available opportunities
2. Open contract → `S` submit → select tape → checklist
3. `Y` send tape (no instant cash)
4. Return to bedroom — response is prepared
5. Bedroom `[5] NOTICE BOARD` — `*** NEW MAIL ***`
6. Open letter — payment claimed once

Cash is stored as integer pennies on the career slot (`%LocalAppData%\Centauri64\Careers\slotN.json`, field `Progress.CashPennies`). Legacy `%LocalAppData%\Centauri64\player.json` is migrated into slot 1.

## Presentation rules (V1.1)

| Stage | Player sees |
|-------|-------------|
| Available contract | Advert + requirements + payment only |
| Submission check | Factual requirement ticks |
| Sent confirmation | SubmissionReceivedText only |
| Pending | Tape name + AWAITING RESPONSE |
| Inbox | From + neutral subject (no ACCEPTED/£) |
| Opened letter | AcceptanceBody + payment claim |

Bedroom `[5] NOTICE BOARD   NEW!` gently flashes only while unread mail exists.

