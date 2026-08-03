# RugbyManager

A *Championship Manager*–style **Rugby Union** management simulator, with an isometric
club/facilities view and a low-poly match presentation in the spirit of 1990s *Jonah Lomu
Rugby*.

> **Status: Phases 0–4 implemented in the simulation core; Phase 3 (Godot visuals) scaffolded.**
> The game is a deep, deterministic, text-driven Rugby Union management sim: a full **interactive
> career** with a squad roster, tactics, a weekly training focus, injuries, a transfer market with
> **scouting fog-of-war**, **coaches** with specialties, a **set-play** playbook, **club finances**,
> and save/load — played across **multiple seasons** with **promotion/relegation** up a league
> pyramid, squad aging, youth intake and a news feed. The visual layer (`godot/`) is a scaffolded
> Godot 4 project that already plays back the sim's match feed; the low-poly *Jonah Lomu*-style
> match view and isometric club builder replace that rendering layer next.

---

## Design principle: separate the sim from the rendering

The one rule that keeps this project alive:

```
┌────────────────────────────────────────────┐
│  Simulation Core  (headless, deterministic) │  ← plain C#, unit-testable, no engine
│  players · match engine · (later) training,  │
│  economy, competitions                       │
└───────────────┬────────────────────────────┘
                │  emits events + state
    ┌───────────┴───────────┐
    ▼                       ▼
 Isometric UI          Match Renderer
 (facilities/menus)    (low-poly playback of the sim's events)
```

Everything in `RugbyManager.Core` is engine-agnostic C#. When we add the Godot 4 project
(also C#), it simply references `Core` and plays back the `MatchEvent` feed. Graphics never
drive logic — they render decisions the sim already made.

## Solution layout

| Project | What it is |
|---|---|
| `src/RugbyManager.Core` | The whole simulation. Model, match engine, competition/season/career, transfers & scouting, training, injuries, finances, persistence, generation. **No Godot dependency.** |
| `src/RugbyManager.Console` | The interactive career REPL (default), plus `auto`, `match` and `load` subcommands. |
| `src/RugbyManager.Web` | **Blazor WebAssembly** front-end — reuses `Core` unchanged, runs the sim client-side in the browser. A working proof-of-concept: live match playback + season table. |
| `tests/RugbyManager.Tests` | 39 xUnit tests (match determinism & balance; fixtures, table bonus points, season determinism, golden-master; save/load round-trips; transfers; training; injuries; coaches; set-plays; scouting; season transitions). |
| `godot/` | Godot 4 (C#) visual layer — **scaffold, not in the solution.** See `godot/README.md`. |

### Inside `Core`
- **`Model/`** — `Position` (the 15 shirts), `PlayerAttributes` (1–100 stats), `Player`
  (+ condition, injury, scouting), `Team` (squad + selected XV, money, coaches, playbook),
  `Tactics`, `PlayerRating`, `Coach`, `SetPlay` (+ `SetPlayLibrary`).
- **`Match/`** — `MatchEngine` (phase-based simulator), `MatchTeam` (live fatigue + derived,
  position-weighted, coach- and set-play-aware ratings), `MatchEvent`, `MatchStats`,
  `MatchResult`, `Commentary`.
- **`Competition/`** — `League`, `FixtureGenerator` (balanced double round-robin via the
  circle method), `LeagueTable` (+ `TableRow`, Rugby Union bonus points), `Season`,
  `Career` (the save-able aggregate), `Pyramid` + `SeasonTransition` (promotion/relegation,
  aging, youth intake, news across seasons).
- **`Transfers/`** — `TransferMarket`, `TransferValue`, `TransferService` (buy/sell against a
  budget), `Scouting` (fog-of-war over ability).
- **`Training/`** — `TrainingFocus`, `TrainingService` (youth-weighted, potential-capped
  development + week-to-week `Condition`; coaches accelerate matching focuses).
- **`Injuries/`** — `InjuryService` (weekly healing + match injury rolls; injured players drop
  out of the XV automatically).
- **`Finance/`** — `FinanceService` (wages, sponsorship, gate receipts that rise with position).
- **`Persistence/`** — `CareerStore` + `SaveData` (compact JSON saves; the schedule is
  regenerated deterministically, so only results & progress are stored).
- **`Generation/`** — `SquadGenerator`, `LeagueGenerator`, `MarketGenerator`, `CoachGenerator`.
- **`Util/`** — `Dice` (seeded RNG; every match is replayable from its seed).

## How the match engine works

The pitch is one axis (`_ballX`, 0–100). Play advances as a sequence of **situations** —
open play, scrum, lineout — whose outcomes are **weighted probability contests** between the
two sides' current ratings (fatigue- and tactics-adjusted) plus a seeded dice roll:

- **Set piece** — contested scrums & lineouts (dominant scrum → penalty; steals; mauls with
  pushover-try potential near the line).
- **Breakdown** — turnovers (the jackal), penalties, knock-ons; aggression wins more ball but
  concedes more penalties.
- **Open play** — carries gain/lose metres, occasional line breaks, tries.
- **Kicking** — territorial kicks, kicks to touch, penalty goals, conversions, drop goals.

Tactics visibly change outcomes: a *forwards/kicking* side trades possession for territory and
takes its penalty goals; an *expansive/rush* side makes more line breaks and goes to the corner.

## Running it

Start an interactive career (random seed, 10 clubs — you manage the first):

```bash
dotnet run --project src/RugbyManager.Console
```

Start a specific career (fixed seed, choose the number of clubs), or continue a save:

```bash
dotnet run --project src/RugbyManager.Console 7 12
```

```bash
dotnet run --project src/RugbyManager.Console load career
```

Auto-play a whole season, or watch a single match's commentary:

```bash
dotnet run --project src/RugbyManager.Console auto 7
```

```bash
dotnet run --project src/RugbyManager.Console match 42
```

Run the tests:

```bash
dotnet test
```

Run the **web** front-end (Blazor WebAssembly — the whole sim in the browser, no server):

```bash
dotnet run --project src/RugbyManager.Web
```

Then open http://localhost:5144 and go to **Career** — the full interactive career (squad,
tactics, transfers/scouting, coaches, set plays, training, finances, live match playback,
save/load to browser localStorage, and multi-season promotion/relegation) is ported to the web
and playable there, alongside the original quick-match/quick-season demo pages.

### Career commands

- **Squad & tactics:** `squad` · `tactics` · `style|breakdown|defence|penalty|kick <value>`
- **Transfers & scouting:** `market [pos]` · `scout <#>` · `sign <#>` · `sell <#>` · `budget`
- **Staff & plays:** `coaches` · `coachmarket` · `hire <#>` · `fire <#>` · `plays` · `learn <#>` · `unlearn <#>`
- **Prep:** `training <focus>` · `finances`
- **Play:** `next` · `sim <n>` · `season` · `commentary` · `newseason` · `news`
- **League:** `table` · `fixtures`
- **Career:** `save [name]` · `load [name]` · `quit`

## Roadmap

- **Phase 0 — text match engine** ✅
- **Phase 1 — the core loop** ✅ — interactive career, tactics, transfers, training, save/load.
- **Phase 2 — depth** ✅ — squad roster, injuries, finances, coaches & specialties, set-play
  library, scouting fog-of-war.
- **Phase 3 — visuals** 🚧 — two front-end tracks: a **Blazor WebAssembly web app**
  (`src/RugbyManager.Web`) that already runs the sim in the browser (live match playback +
  season table), and a **Godot 4** scaffold (`godot/`). The web route reuses `Core` verbatim
  and needs no server. Next: port the full career screens; a 2-D/low-poly match view over the
  same event feed.
- **Phase 4 — the world** ✅ (core) — multi-season careers, promotion/relegation pyramid,
  aging, youth academy, news. Remaining: internationals, sponsorship deals, the visual
  set-play designer (a Godot task, downstream of Phase 3).

## Tech

- .NET 8 (targeted for Godot 4 C# compatibility), built with the .NET 10 SDK.
- `godot/` holds the Godot 4 (C#) visual scaffold — open it in the Godot editor to build/run.
