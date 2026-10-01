---

description: "Task list for 002 Qix Enemy and Lives"
---

# Tasks: Qix Enemy and Lives

**Input**: Design documents from `/specs/002-qix-enemy/`

**Prerequisites**: plan.md, spec.md, research.md (R10–R16), data-model.md, contracts/cli-and-controls.md, quickstart.md

**Tests**: Included. Constitution Principle II requires automated tests for collision detection
and win/lose conditions; the plan also covers Qix bouncing and the new claiming rule. Only
`src/Qix/Core` is unit-tested. `Program.cs` and `src/Qix/Terminal` are checked by the manual
playtest in quickstart.md (Q1–Q12).

**Organization**: Tasks are grouped by user story, so each story can be implemented and tested on its own.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)

## Conventions used by every task

- All 001 conventions still apply (see `specs/001-player-movement-claiming/tasks.md`):
  file-scoped namespaces `Qix.Core` / `Qix.Terminal` / `Qix.Tests`, `Point.X` = column and
  `Point.Y` = row, no interfaces or extra projects, small hand-checkable test playfields.
- `Core` MUST NOT reference `System.Console`, `Stopwatch`, or `DateTime`. Time enters only as the
  `TimeSpan` argument of `Game.Advance`; randomness only through the `Random` passed to `Game`
  (R10, R11).
- Tests MUST NOT sleep. They take one Qix step with `game.Advance(Game.QixStepInterval)` and set
  `game.Qix.Position` / `game.Qix.Velocity` directly, or pass `new Random(seed)`.
- Qix cell rules (R12): `Empty` and `Trail` are open, `Border`, `Claimed`, and outside the grid are
  walls; entering `Trail` is a contact, not a move.

---

## Phase 1: Setup

**Purpose**: Confirm the 001 baseline before changing it.

- [X] T001 Run `dotnet build` and `dotnet test` from the repository root on branch `002-qix-enemy` and confirm both pass with the 001 code unchanged. If anything fails, fix that first; it is not part of this feature

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Small shared additions that every story builds on.

- [X] T002 [P] Add `readonly record struct Velocity(int Dx, int Dy)` to `src/Qix/Core/Geometry.cs` with an XML doc comment: "Diagonal travel direction of the Qix; each component is −1 or +1 while moving diagonally" (data-model "Velocity"). Keep `Direction` unchanged; it stays the marker's four directions. Add `static Velocity[] Diagonals` holding the four diagonals in the order (1,1), (1,−1), (−1,1), (−1,−1), for random selection
- [X] T003 [P] Add `ConsoleColor.Magenta => 95` to the explicit color table in `SgrCode` in `src/Qix/Terminal/Renderer.cs` (R16). `Red` (91) is already there for the red trail

**Checkpoint**: `dotnet build` and `dotnet test` still pass.

---

## Phase 3: User Story 1 - A Qix Bounces Around the Open Area (Priority: P1) 🎯 MVP

**Goal**: A magenta `●` starts near the center, moves diagonally at about 8 cells per second on its own, and bounces off the frame and claimed territory. No collisions yet: if it would touch the line it just stays put for that step.

**Independent Test**: Launch, press nothing for 30 s: the Qix moves steadily and never enters the frame. Claim a region and watch it bounce off the new edge (quickstart Q1, Q2, Q11).

### Tests for User Story 1 ⚠️

> Write these first and make sure they fail (or don't compile) before T007–T010.

- [X] T004 [P] [US1] Create `tests/Qix.Tests/QixMovementTests.cs` testing `Qix.Step(Playfield)` directly on small playfields (e.g. `new Playfield(10, 6)`, interior x 1..8, y 1..4), each test constructing `new Qix(position, velocity)`:
  - open space: from (4, 2) with (1, 1) → moves to (5, 3), velocity unchanged, outcome `Moved`;
  - flat vertical wall: from (8, 2) with (1, 1) → velocity becomes (−1, 1), position (7, 3);
  - flat horizontal wall: from (4, 4) with (1, 1) → velocity becomes (1, −1), position (5, 3);
  - corner: from (8, 4) with (1, 1) → velocity becomes (−1, −1), position (7, 3);
  - corner tip (only the diagonal target is a wall): build a `Claimed`/`Border` cell diagonally ahead by closing a one-cell corner line with `SetTrail`+`CloseTrail` (or the 001 helper), and assert both components flip;
  - no corner cutting: a diagonal move whose two orthogonal neighbours are both walls and whose target is `Empty` is blocked; the Qix bounces instead of passing through;
  - one-cell-wide corridor (e.g. `new Playfield(10, 3)`, interior y = 1 only): from (4, 1) with (1, 1), repeated steps move it straight right one cell per step, then it reverses at x = 8 and runs left;
  - single-cell region (`new Playfield(3, 3)`): the Qix at (1, 1) stays put, outcome `Stayed`;
  - property test: with `new Random(1)` choose a start velocity, run 2,000 steps on a 20×12 playfield that has one claimed region (close a line first), and assert after every step that the Qix's cell is `Empty`
- [X] T005 [P] [US1] Add `tests/Qix.Tests/QixTimingTests.cs` testing `Game.Advance` on `new Game(new Playfield(20, 12), new Random(1))`:
  - the Qix starts at `(Width / 2, Height / 2)` = (10, 6) with a velocity from `Velocity.Diagonals`;
  - `Advance(QixStepInterval − 1 ms)` doesn't move it and returns `false`; one more 1 ms moves it exactly one cell and returns `true`;
  - three calls of `QixStepInterval / 2` give exactly one step (the remainder carries over);
  - `Advance(TimeSpan.FromSeconds(10))` moves it at most 2 steps (250 ms cap, R10);
  - `Restart()` puts the Qix back at the center;
  - `Game.QixStepInterval == TimeSpan.FromMilliseconds(125)`

### Implementation for User Story 1

- [X] T006 [US1] Create `sealed class Qix(Point position, Velocity velocity)` in `src/Qix/Core/Qix.cs` with settable `Position` and `Velocity`, plus `enum QixStepOutcome { Moved, Contact, Stayed }` (depends on T002)
- [X] T007 [US1] Implement `QixStepOutcome Step(Playfield playfield)` in `src/Qix/Core/Qix.cs` exactly as research R12/R13 (also data-model "Qix → Step"). Try candidates in this order and take the first that isn't *blocked*:
  1. the current diagonal `(Dx, Dy)`;
  2. the reflected diagonal: flip `Dx` if `(x+Dx, y)` is a wall, flip `Dy` if `(x, y+Dy)` is a wall, flip both if neither is;
  3. the two remaining diagonals;
  4. straight moves `(Dx, 0)`, `(−Dx, 0)`, `(0, Dy)`, `(0, −Dy)`.

  Evaluating a candidate with target `t`: for a diagonal, if neither orthogonal neighbour is `Empty`, it is a *Contact* when either of them is `Trail`, otherwise *blocked* (no corner cutting). Then if `t` is `Trail` → *Contact*; if `t` is `Empty` → *Moved*; otherwise *blocked*. On *Moved*, set `Position = t`; for a diagonal set `Velocity` to that diagonal, for a straight move change only the moved component. On *Contact*, position and velocity stay unchanged. If every candidate is blocked, return `Stayed`. Add a private helper `bool IsWall(Playfield, Point)` (outside the grid, `Border`, or `Claimed`) (depends on T006)
- [X] T008 [US1] Extend `Game` in `src/Qix/Core/Game.cs` (data-model "Game"): constructor `Game(Playfield? playfield = null, Random? random = null)` storing the `Random` (default `new Random()`); `public Qix Qix { get; private set; }` created at `(Playfield.Width / 2, Playfield.Height / 2)` with `Velocity.Diagonals[random.Next(4)]`; `public static readonly TimeSpan QixStepInterval = TimeSpan.FromMilliseconds(125)`; and `public bool Advance(TimeSpan elapsed)`: cap `elapsed` at 250 ms, add it to a private accumulator, and while the accumulator ≥ `QixStepInterval` subtract the interval and, if `Phase == GamePhase.Playing`, call `Qix.Step(Playfield)`. Return `true` if any step returned `Moved`. A `Contact` outcome is ignored in this story (US2 wires it up). `Restart()` also recreates the Qix at the center with a new random diagonal and resets the accumulator (depends on T007)
- [X] T009 [US1] Update the loop in `src/Qix/Program.cs` (R10): start a `Stopwatch` before the loop; each pass, after handling keys, if the window is not too small call `dirty |= game.Advance(stopwatch.Elapsed)` and `stopwatch.Restart()`; while the window is too small, just `stopwatch.Restart()` so the game is paused and the Qix doesn't jump afterwards (contract "Too-small screen") (depends on T008)
- [X] T010 [P] [US1] In `src/Qix/Terminal/PlayfieldView.cs`, draw the Qix after the cells and before the marker: `r.Put(OriginCol + qix.X, OriginRow + qix.Y, '●', ConsoleColor.Magenta)` (contract glyph table, FR-004) (depends on T003, T008)
- [ ] T011 [US1] Run `dotnet test` (all 001 tests plus T004–T005 pass), then playtest quickstart Q1, Q2, and Q11 in Windows Terminal and Git Bash. Known and accepted until US3 (plan "US1 caveat"): cutting off the Qix's region with the old "keep the largest" rule can leave the Qix inside claimed area

**Checkpoint**: The game runs with a bouncing Qix; nothing else has changed.

---

## Phase 4: User Story 2 - Touching the Unfinished Line Costs a Life (Priority: P2)

**Goal**: Qix contact with the line (or the drawing marker running into the Qix) costs one of 3 lives, shows the line in red for 1 s, then resets the line and returns the marker to where the line started. The last life lost shows a game-over box with R to restart.

**Independent Test**: Draw into the Qix's path: lives 3 → 2, red line with `Hit!`, then line gone and marker at the line start with `Draw: OFF`. Lose all lives: game-over box, R restarts (quickstart Q3–Q7, Q10).

### Tests for User Story 2 ⚠️

- [X] T012 [US2] Keep the existing 001 tests valid once the Qix can hit the line: their lines run through the playfield center, where the Qix now starts. Move the Qix out of the way right after creating each `Game`:
  - `tests/Qix.Tests/DrawingTests.cs`: in `NewGame()`, set `game.Qix.Position = new Point(1, 1)`;
  - `tests/Qix.Tests/ClaimingTests.cs` test `After_a_close_the_marker_walks_the_new_edge_but_not_into_claimed_area`: set `game.Qix.Position = new Point(2, 2)` (the left side, which stays open);
  - `tests/Qix.Tests/ProgressTests.cs`: in every test that creates a `Game` (including `CompletedGame()`), set `game.Qix.Position = new Point(2, 3)` (inside the area that stays open, x 1..5, y 3..4).

  Keep `MovementTests.cs` unchanged (the marker never leaves the frame there). Run `dotnet test` and confirm everything still passes
- [X] T013 [P] [US2] Create `tests/Qix.Tests/CollisionTests.cs` on `new Game(new Playfield(10, 6), new Random(1))` (marker starts at (5, 5)). Each test sets `game.Qix.Position`/`Velocity` and takes single steps with `game.Advance(Game.QixStepInterval)`:
  - Qix steps onto a trail cell → `Lives == 2`, `Phase == LifeLost`, Qix position unchanged;
  - Qix steps onto the marker's own cell while drawing → life lost;
  - pinch: the Qix's diagonal step passes between two trail cells (draw an L so the trail occupies both orthogonal neighbours) → life lost;
  - the drawing marker moves onto the Qix's cell → life lost (contact from `Move`);
  - marker on the frame with no line, Qix moving right next to it for 20 steps → no life lost (US2 scenario 5);
  - drawing switched off mid-line, Qix steps onto the trail → life lost (US2 scenario 6);
  - close wins: the Qix's next step would enter the trail, but the player's `Move` closes the line before `Advance` → fill happens, `Lives == 3`, `Phase == Playing` (FR-012)
- [X] T014 [P] [US2] Create `tests/Qix.Tests/LivesTests.cs`:
  - a new game has `Lives == Game.StartingLives == 3` and `Phase == Playing`;
  - after a contact: during `LifeLost` the trail is still present, `Move`/`ToggleDraw` change nothing, and `Advance` does not move the Qix;
  - `Advance(Game.LifeLostPause − 1 ms)` keeps `LifeLost`; `Advance(1 ms)` more ends it: no `Trail` cells left, marker at its `LineStart` (the frame cell it left from), `Mode == OnBorder`, `DrawOn == false`, `Phase == Playing`, and `ClaimedPercent` unchanged;
  - `Marker.LineStart` is recorded when the marker leaves the border (e.g. after `ToggleDraw`, `Move(Left)`, `Move(Up)` from (5, 5) it is (4, 5));
  - third contact → `Lives == 0`, `Phase == GameOver`; `Advance` and `Move` then change nothing;
  - `Restart()` from `GameOver` gives `Lives == 3`, 0%, `Phase == Playing`, marker at its start and Qix at the center;
  - `Game.LifeLostPause == TimeSpan.FromSeconds(1)`

### Implementation for User Story 2

- [X] T015 [P] [US2] Add `public void ClearTrail()` to `src/Qix/Core/Playfield.cs`: every `Trail` cell becomes `Empty` again (reuse the existing private `Replace`). `ClaimedPercent` is not touched (data-model "Playfield")
- [X] T016 [US2] In `src/Qix/Core/Game.cs`, add `public Point LineStart { get; set; }` to `Marker` ("the border cell the marker left from when the current line began") and set it in `Move` in the `Cell.Empty when DrawOn` case when `Marker.Mode` is still `OnBorder`, before moving (depends on T008)
- [X] T017 [US2] In `src/Qix/Core/Game.cs`, add the lives and phases (data-model "Game", "Phase transitions", R14): extend `GamePhase` to `Playing, LifeLost, GameOver, Complete`; add `public const int StartingLives = 3`, `public static readonly TimeSpan LifeLostPause = TimeSpan.FromSeconds(1)`, `public int Lives { get; private set; } = StartingLives`. Add private `LoseLife()`: `Lives--`; if `Lives == 0` → `Phase = GameOver`, else `Phase = LifeLost` and start a pause countdown of `LifeLostPause`. Add private `EndLifeLostPause()`: `Playfield.ClearTrail()`, `Marker.Trail.Clear()`, `Marker.Position = Marker.LineStart`, `Marker.Mode = OnBorder`, `DrawOn = false`, `Phase = Playing` (depends on T015, T016)
- [X] T018 [US2] Wire contacts and the pause into `src/Qix/Core/Game.cs`:
  - `Advance`: in `LifeLost`, count the (capped) elapsed time down and call `EndLifeLostPause()` when it reaches zero and reset the Qix step accumulator, returning `true`; in `Playing`, a `Contact` from `Qix.Step` calls `LoseLife()` and stops stepping for this call (return `true`); in `GameOver` and `Complete` nothing happens;
  - `Move` and `ToggleDraw` return immediately unless `Phase == Playing` (replaces the 001 `Complete` check);
  - `Move`: in the two `Cell.Empty when DrawOn` paths, after moving, if `Marker.Position == Qix.Position` call `LoseLife()` (R13 rule 3);
  - `Restart()` also resets `Lives` to `StartingLives` and the pause countdown (depends on T017)
- [X] T019 [US2] In `src/Qix/Program.cs`, make R restart in `GamePhase.GameOver` as well as `GamePhase.Complete` (contract "Controls") (depends on T018)
- [X] T020 [P] [US2] In `src/Qix/Terminal/PlayfieldView.cs` (contract "Screen layout", R16): draw `Trail` cells in `ConsoleColor.Red` instead of `Yellow` when `game.Phase` is `LifeLost` or `GameOver`; add `private const int LivesStatusCol = 36` and draw `$"Lives: {game.Lives}"` there, followed by `"  Hit!"` in red during `LifeLost`; when `Phase == GameOver`, draw a centered box with `Game over! NN% claimed   R = restart   Esc = quit`, generalising `DrawCompletionBox` to take the message (depends on T018)
- [ ] T021 [US2] Run `dotnet test` (all tests pass), then playtest quickstart Q3–Q7 and Q10

**Checkpoint**: The Qix is dangerous, lives work, and the game can be lost and restarted.

---

## Phase 5: User Story 3 - Claim the Side Without the Qix (Priority: P3)

**Goal**: Closing a line keeps the Qix's region open and claims every other region, even the larger one. Trapping the Qix in a corner can claim most of the field in one move.

**Independent Test**: Wait until the Qix is in a small corner, cut that corner off: the large side is claimed and the Qix keeps bouncing in the small side (quickstart Q8, Q9, Q12).

### Tests for User Story 3 ⚠️

- [X] T022 [US3] Update `tests/Qix.Tests/ClaimingTests.cs` for `CloseTrail(Point keep)` (R15): change the `Close` helper to `Close(Playfield playfield, Point keep, params (int X, int Y)[] trail)` and pass, in each existing test, a cell inside the region that test expects to stay `Empty` (e.g. (6, 2) for the straight cut, (6, 3) for the L-shape, the corner-cut and claimed-edge tests, and (8, 4) for the pocket test), so the expected results stay the same. Replace `Equal_regions_keep_the_one_with_the_topmost_leftmost_cell` with `The_qix_region_is_kept_even_when_it_is_smaller`: on 10×6, cut at x = 3 with `keep` = (1, 1) → the left 2×4 stays `Empty` and the right 5×4 is `Claimed`. Add `Every_region_without_the_qix_is_claimed`: the pocket line with `keep` in the bottom-left region (1, 4) → the pocket and the big region are `Claimed`, only x = 1, y = 3..6 stays `Empty`
- [X] T023 [P] [US3] Update `tests/Qix.Tests/ProgressTests.cs` for `CloseTrail(Point keep)`: change the `Close` helper the same way and pass, per call, a keep point inside the region that call expects to stay open, so all expected percentages stay the same: (2, 3) for the x = 8 cut, the y = 2 cut, the `(6, 3), (6, 4)` cut, and the one-cell corner cut; (6, 3) for the x = 3 cut in `Lines_count_as_claimed` (the larger right side stays open there)
- [X] T024 [P] [US3] Add Game-level tests to `tests/Qix.Tests/CollisionTests.cs` (or a new `tests/Qix.Tests/QixClaimingTests.cs`): on `new Game(new Playfield(10, 6), new Random(1))` with `game.Qix.Position = new Point(1, 1)`, draw a straight cut up x = 2 from the bottom frame (move the marker left to (2, 5) first): the large right side x 3..8 is `Claimed`, the Qix's column x = 1 stays `Empty`, the Qix's cell is still `Empty`, and since 28 of 32 interior cells are now claimed (87%), `Phase == Complete` (US3 scenarios 1 and 4)

### Implementation for User Story 3

- [X] T025 [US3] In `src/Qix/Core/Playfield.cs`, change `CloseTrail()` to `CloseTrail(Point keep)` and replace `ClaimAllButLargestRegion()` with `ClaimAllExceptRegionOf(Point keep)`: flood-fill the `Empty` region containing `keep` (reuse `FloodFill`) and turn every other `Empty` cell into `Claimed`. If `keep` is not an `Empty` cell, throw `InvalidOperationException` (the Qix is always on `Empty`, data-model invariant). Remove the tie-rule comment and code; update the XML doc comments (R15). Border demotion and `ClaimedPercent` are unchanged (depends on T022, T023)
- [X] T026 [US3] In `src/Qix/Core/Game.cs`, make `CloseLine()` call `Playfield.CloseTrail(Qix.Position)` (FR-011) (depends on T025)
- [ ] T027 [US3] Run `dotnet test` (all tests pass), then playtest quickstart Q8, Q9, and Q12

**Checkpoint**: All three stories work together; the Qix can no longer end up inside claimed territory.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T028 [P] Update the doc comments that still describe 001 behaviour: the `Game` summary in `src/Qix/Core/Game.cs` ("Driven one move at a time" → moves plus `Advance`), and the `GamePhase`/`Marker` comments, so they match data-model.md
- [X] T029 [P] Leave 001's documents as they are (they describe 001). Check that `specs/002-qix-enemy/data-model.md` and `contracts/cli-and-controls.md` match what was built (member names, glyphs, status column) and fix the documents if they drifted
- [ ] T030 Definition of done (constitution): `dotnet build` with no errors, `dotnet test` all green, and a full manual pass of quickstart Q1–Q12 plus 001's V2–V9 in Windows Terminal and Git Bash

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (Phase 1)** → **Foundational (Phase 2)** → **US1** → **US2** → **US3** → **Polish**
- US2 needs the Qix from US1. US3 needs the Qix position from US1; it doesn't need US2, but
  T022–T024 are written assuming T012 has already moved the Qix out of the way in the 001 tests.
  If US3 is done before US2, do T012 first.

### Within each story

- Tests first (they fail or don't compile), then implementation, then the story's run-and-playtest task.
- `src/Qix/Core/Game.cs` is touched by T008, T016, T017, T018, and T026: these run in sequence.

### Parallel opportunities

- T002 ‖ T003
- US1: T004 ‖ T005 (separate test files); T010 can run alongside T009 once T008 is done
- US2: T013 ‖ T014 ‖ T015 (separate files); T020 alongside T019
- US3: T023 ‖ T024 alongside T022
- Polish: T028 ‖ T029

### Parallel example: User Story 2

```text
T013  CollisionTests.cs
T014  LivesTests.cs
T015  Playfield.ClearTrail()
```

---

## Implementation Strategy

### MVP first (User Story 1 only)

1. Phase 1 + Phase 2
2. Phase 3 (US1): a bouncing Qix in a playable game
3. **Stop and validate**: Q1, Q2, Q11

### Incremental delivery

1. US1 → the field comes alive (demo-able, harmless Qix)
2. US2 → the Qix is a threat; lives and game over
3. US3 → the claiming rule that makes trapping the Qix pay off, and closes the US1 caveat

Each story ends with `dotnet test` green and a runnable game (constitution Principle III).
