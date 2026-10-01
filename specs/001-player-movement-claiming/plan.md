# Implementation Plan: Player Movement and Territory Claiming

**Branch**: `001-player-movement-claiming` | **Date**: 2026-10-01 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-player-movement-claiming/spec.md`

## Summary

Build the first playable version of the Qix remake as a .NET 10 console game. The player moves a
marker along the playfield frame, holds Space to draw lines into the unclaimed area, and claims
territory whenever a line closes: every region except the largest becomes claimed. A status line
shows the claimed percentage; at 75% a completion screen offers restart or quit. There are no
enemies.

The technical approach: pure, step-driven game logic (`Core`) that is unit-tested headlessly,
plus a thin Windows terminal layer (`Terminal`). The terminal layer reads raw Win32 console input
to detect key-up events (needed for "hold to draw") and renders frame differences with VT escape
sequences to avoid flicker. See [research.md](research.md).

## Technical Context

**Language/Version**: C# 14 on .NET 10 (LTS), nullable reference types enabled

**Primary Dependencies**: None at runtime beyond the .NET base class library; Win32 APIs
(`kernel32`, `winmm`) are called through P/Invoke. Tests: xUnit plus the default test SDK
packages from `dotnet new xunit`.

**Storage**: N/A

**Testing**: xUnit (`tests/Qix.Tests`), core logic only (R8); manual playtest per
[quickstart.md](quickstart.md)

**Target Platform**: Windows 10/11, Windows Terminal (conhost should work too)

**Project Type**: Console application (single executable)

**Performance Goals**: Movement steps every 50 ms with no visible stutter (SC-005); fill and
percentage update in under 100 ms (SC-003). Expected: under 1 ms for a 1,520-cell grid.

**Constraints**: Terminal at least 80×25; one write per frame; the terminal is restored on every
exit path

**Scale/Scope**: One playfield of 78×22 cells; about 10 source files

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Rule (constitution v1.0.0) | Check | Status |
|----------------------------|-------|--------|
| I. Simplicity: smallest implementation, no speculative abstractions | Two projects only (game + tests). No interfaces, DI, or libraries. Logic and terminal layers are separated by folders, not by projects. | ✅ Pass |
| I. Deviations justified in Complexity Tracking | Win32 P/Invoke and the timer-resolution call are non-obvious, so both are listed below | ✅ Pass |
| II. Tests for core/risky logic | Movement, drawing, claiming (including pockets and the tie rule), percentage, and completion are unit-tested | ✅ Pass |
| II. Glue may be verified manually | Rendering, input, and the loop are covered by quickstart V1–V11 | ✅ Pass |
| II. Fast, deterministic tests | The logic is step-driven with no clock, randomness, or console access | ✅ Pass |
| III. Always playable after each user story | US1 makes a launchable game with movement; US2 adds claiming; US3 adds progress and completion | ✅ Pass |
| Stack: C#, current .NET LTS, nullable | .NET 10, `<Nullable>enable</Nullable>` | ✅ Pass |
| Console app, runs in Windows Terminal | Yes; Windows-only input is allowed because cross-platform is "desirable but not required" | ✅ Pass |
| NuGet packages justified | Only the test packages from the default xUnit template | ✅ Pass |
| `dotnet build` / `dotnet test` from root | `Qix.slnx` at the root | ✅ Pass |

**Gate result (pre-research)**: PASS
**Gate result (post-design)**: PASS. The design in [data-model.md](data-model.md) adds no layers,
interfaces, or projects beyond those listed here.

## Project Structure

### Documentation (this feature)

```text
specs/001-player-movement-claiming/
├── plan.md              # This file
├── research.md          # Phase 0: decisions R1–R9
├── data-model.md        # Phase 1: cells, marker rules, game phases
├── quickstart.md        # Phase 1: build/run + manual validation V1–V11
├── contracts/
│   └── cli-and-controls.md   # Launch, exit codes, controls, screen layout
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 (/speckit-tasks, not created here)
```

### Source Code (repository root)

```text
Qix.slnx
src/
└── Qix/
    ├── Qix.csproj              # net10.0 console app, nullable on
    ├── Program.cs              # Entry point: environment checks, terminal session, game loop
    ├── Core/                   # Pure game logic: no console, no clock
    │   ├── Cell.cs             # Cell enum
    │   ├── Geometry.cs         # Point, Direction (+ offsets)
    │   ├── InputState.cs       # Held direction + draw flag for one step
    │   ├── Playfield.cs        # Grid, trail, CloseTrail (flood fill + demotion), percentage
    │   └── Game.cs             # Marker, movement rules, phase, Step / Restart
    └── Terminal/               # Windows console I/O (glue, tested manually)
        ├── NativeMethods.cs    # P/Invoke: console modes, ReadConsoleInputW, timeBeginPeriod
        ├── KeyboardState.cs    # Tracks pressed keys from key-down/up and focus events
        ├── TerminalSession.cs  # Alternate screen, cursor, VT mode, guaranteed restore
        └── Renderer.cs         # Frame buffer, diffing, single VT write per frame
tests/
└── Qix.Tests/
    ├── Qix.Tests.csproj        # xUnit, references src/Qix
    ├── MovementTests.cs        # Border travel, blocked moves, corners
    ├── DrawingTests.cs         # Trail creation, blocks, releasing Space
    ├── ClaimingTests.cs        # Straight/L/pocket fills, tie rule, demotion
    └── ProgressTests.cs        # Percentage rounding, 75% completion, restart
```

**Structure Decision**: A single console project with `Core` (logic) and `Terminal` (I/O)
folders, plus one test project. Tests reference the executable project directly, so no separate
class library is needed (Principle I). `Core` must not use `System.Console` or the clock; this
keeps the logic deterministic and testable (Principle II).

## Complexity Tracking

These are not violations; they are the non-obvious design choices that Principle I requires to
be justified.

| Choice | Why Needed | Simpler Alternative Rejected Because |
|--------|------------|-------------------------------------|
| Win32 `ReadConsoleInputW` via P/Invoke | "Hold Space to draw" (FR-006/FR-008) needs key-up events and Space + arrow held together | `Console.ReadKey` reports only key-down and auto-repeats only the last key, so held Space is invisible (research R2) |
| `timeBeginPeriod(1)` via P/Invoke | Even 50 ms movement steps (SC-005) | The default ~15.6 ms timer makes steps alternate between 47 and 62 ms, which shows as stutter (research R4) |
| Diff-based VT renderer | Flicker-free drawing (SC-005) | `Console.Clear` + full redraw flickers; per-cell `Console.Write` is slow (research R3) |
