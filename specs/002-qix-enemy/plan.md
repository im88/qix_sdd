# Implementation Plan: Qix Enemy and Lives

**Branch**: `002-qix-enemy` | **Date**: 2026-10-01 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/002-qix-enemy/spec.md`

## Summary

Add one Qix enemy to the 001 game. The Qix is a single cell that moves diagonally on its own at
about 8 steps per second and bounces off the border and claimed territory. If it touches the
unfinished line (or the drawing marker runs into it), the player loses one of 3 lives: the line
is shown in red for a one-second pause, then erased, and the marker returns to where the line
started. Losing the last life shows a game-over box with restart. Closing a line now keeps the
Qix's region open and claims every other region, whatever its size.

The technical approach: time enters the pure `Core` logic only through
`Game.Advance(TimeSpan elapsed)`, called from the existing 10 ms loop with a `Stopwatch`
measurement, and randomness only through an injected `Random`. Tests drive both directly, so
they stay fast and deterministic. See [research.md](research.md) R10–R16.

## Technical Context

**Language/Version**: C# 14 on .NET 10 (LTS), nullable reference types enabled (unchanged)

**Primary Dependencies**: None beyond the .NET base class library (unchanged); xUnit for tests

**Storage**: N/A

**Testing**: xUnit (`tests/Qix.Tests`) for Qix movement, collisions, lives and phases, and the
new claiming rule; manual playtest per [quickstart.md](quickstart.md). 001's claiming and
progress tests are updated for the new rule (R15).

**Target Platform**: Windows 10/11, Windows Terminal and Git Bash (unchanged)

**Project Type**: Console application (single executable)

**Performance Goals**: Qix step every 125 ms with no visible stall or flicker (SC-003). A step
is a handful of cell checks; the fill on close is unchanged (well under 1 ms).

**Constraints**: `Core` takes no clock and no console; elapsed time per `Advance` call capped at
250 ms (R10); the game is paused while the window is too small

**Scale/Scope**: One Qix, one playfield of 78×22 cells; about 3 new or changed core files and
3 new test files

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Rule (constitution v1.0.0) | Check | Status |
|----------------------------|-------|--------|
| I. Simplicity: smallest implementation, no speculative abstractions | A `Qix` class and a `Velocity` struct; elapsed time passed as a `TimeSpan`, no clock interface; `Random` passed directly, no wrapper | ✅ Pass |
| I. Deviations justified in Complexity Tracking | None new; the candidate-move bounce order and the pinch rule are explained in R12/R13 because they are not obvious | ✅ Pass |
| II. Tests for core/risky logic | Collision detection (Qix–line, marker–Qix, pinch, close-wins), win/lose conditions (lives, game over, completion with the new claim rule), and Qix bouncing are unit-tested | ✅ Pass |
| II. Glue may be verified manually | Stopwatch timing, rendering of the Qix, red line, and game-over box are covered by quickstart Q1–Q12 | ✅ Pass |
| II. Fast, deterministic tests | Tests call `Advance(Game.QixStepInterval)` and set `Qix.Position`/`Velocity` or pass a seeded `Random`; no real-time waits | ✅ Pass |
| III. Always playable after each user story | US1: Qix visible and bouncing (no collisions yet). US2: lives, pause, game over. US3: new claiming rule. Each ends with a runnable game | ✅ Pass |
| Gameplay changes state affected behaviours | Spec Assumptions: the Qix, drawing, claiming, win/lose conditions | ✅ Pass |
| Stack, console app, no new NuGet packages, `dotnet build` / `dotnet test` from root | Unchanged from 001 | ✅ Pass |

**Gate result (pre-research)**: PASS
**Gate result (post-design)**: PASS. [data-model.md](data-model.md) adds one class, one struct,
and new members on existing types; no layers, interfaces, or projects.

**US1 caveat for "Always playable"**: Between US1 and US3 the old "keep the largest region" rule
is still active, so the Qix can end up inside claimed territory if the player cuts off its
region. It then keeps bouncing inside a wall-locked cell area or stands still. The game stays
playable and doesn't crash; US3 fixes it. Implementing US3's claim rule together with US1 is an
acceptable alternative if the tasks prefer it.

## Project Structure

### Documentation (this feature)

```text
specs/002-qix-enemy/
├── plan.md              # This file
├── research.md          # Phase 0: decisions R10–R16 (continues 001's R1–R9)
├── data-model.md        # Phase 1: Qix, Velocity, lives, phases, changed members
├── quickstart.md        # Phase 1: manual validation Q1–Q12
├── contracts/
│   └── cli-and-controls.md   # Changes to controls and screen
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 (/speckit-tasks, not created here)
```

### Source Code (repository root)

```text
src/Qix/
├── Program.cs                  # CHANGED: Stopwatch + game.Advance(elapsed); pause while too small; R in GameOver
├── Core/
│   ├── Geometry.cs             # CHANGED: + Velocity
│   ├── Qix.cs                  # NEW: Qix position/velocity and step logic (R12, R13)
│   ├── Playfield.cs            # CHANGED: CloseTrail(Point keep), ClearTrail()
│   └── Game.cs                 # CHANGED: Qix, Lives, phases, Advance, LineStart, contact handling
└── Terminal/
    ├── Renderer.cs             # CHANGED: + Magenta color code
    └── PlayfieldView.cs        # CHANGED: Qix glyph, Lives status, red trail when hit, game-over box
tests/Qix.Tests/
├── QixMovementTests.cs         # NEW: diagonal moves, flat-wall and corner bounces, corridor, no corner cutting, stuck in 1 cell
├── CollisionTests.cs           # NEW: Qix enters trail, pinch, marker onto Qix, close wins, border safe, draw-off still vulnerable
├── LivesTests.cs               # NEW: 3 lives, pause length, respawn at LineStart, input ignored in pause, game over, restart
├── ClaimingTests.cs            # CHANGED: Qix placed explicitly; Qix region kept even if smaller; tie-rule tests replaced
└── ProgressTests.cs            # CHANGED: Qix placed explicitly where 001 relied on "largest kept"
```

**Structure Decision**: Same single console project with `Core` and `Terminal` folders plus one
test project. The Qix gets its own file in `Core` because its stepping rules are the bulk of the
new logic; everything else extends existing types.

## Complexity Tracking

No constitution violations and no new non-obvious infrastructure. The 001 entries (VT-output
P/Invoke, diff renderer) still apply unchanged.
