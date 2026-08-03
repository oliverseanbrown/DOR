# RugbyManager — Godot visual layer (Phase 3 scaffold)

This folder is the **Phase 3 starting point**: a Godot 4 (C#/.NET) project that references the
engine-agnostic `RugbyManager.Core` and renders what it produces.

> **Not built or run in this repo's CI.** Godot was not installed in the environment where this
> was scaffolded, so these files are a hand-written starting point — open them in the Godot
> editor to build and run. Treat `Main.cs`'s Godot API calls as unverified until then.

## What it demonstrates

`scripts/Main.cs` simulates a match with `RugbyManager.Core.MatchEngine` and plays the
`MatchEvent` feed back on a timer with a live scoreboard. That's the whole architectural point:

- **The simulation decides everything.** The visual layer never contains game logic.
- The low-poly, *Jonah Lomu Rugby*–style match view and the isometric club/facilities builder
  are future **replacements for this rendering layer** — they read the same `MatchResult` /
  `MatchEvent` data this scaffold already consumes.

## How to open

1. Install **Godot 4.3+ (.NET/Mono build)** and the **.NET 8 SDK** (already present here).
2. Open the Godot editor and import this `godot/` folder as a project.
3. Let Godot build the C# solution (it restores `Godot.NET.Sdk` and compiles against
   `../src/RugbyManager.Core`). Press **Play** — `scenes/Main.tscn` runs the match playback.

It is kept **out of `RugbyManager.slnx`** on purpose, so `dotnet build` / `dotnet test` on the
core solution never depend on Godot being installed.

## Suggested next steps (Phase 3 → 4 visuals)

1. **Scoreboard + pitch** — replace the label scoreboard with a proper HUD; draw a simple
   top-down pitch and animate the ball's `_ballX` position from the event stream.
2. **Low-poly players** — swap in chunky low-poly 3D (or pre-rendered sprites) driven by event
   types (carry, line break, try, kick).
3. **Isometric club builder** — a `TileMap`-based ground/facilities view, reading a future
   `Facilities` model from Core (pitch, gym, medical bay, stands) with upgrade interactions.
4. **Career front-end** — port the console career screens (squad, table, transfers, training,
   coaches, playbook, finances) to Godot Control scenes over the same `Career` aggregate.
5. **Visual set-play designer** — a drag-a-runner editor that emits `SetPlay` data back into Core.
