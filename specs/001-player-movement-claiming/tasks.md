---

description: "Task list for 001 Player Movement and Territory Claiming"
---

# Tasks: Player Movement and Territory Claiming

**Input**: Design documents from `/specs/001-player-movement-claiming/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/cli-and-controls.md, quickstart.md

**Tests**: Included. Constitution Principle II requires automated tests for claiming/fill, the
claimed percentage, and win conditions. The plan (R8) also covers movement and drawing rules.
Only `src/Qix/Core` is unit-tested. The `Terminal` layer and `Program.cs` are checked by the
manual playtest in quickstart.md (V1–V11).

**Organization**: Tasks are grouped by user story, so each story can be implemented and tested on its own.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)

## Conventions used by every task

- Namespaces: `Qix.Core` for `src/Qix/Core/*`, `Qix.Terminal` for `src/Qix/Terminal/*`, and
  `Qix.Tests` for the tests. Use file-scoped namespaces.
- `Core` MUST NOT reference `System.Console`, `Stopwatch`, `DateTime`, or randomness (plan
  Structure Decision).
- Coordinates: `Point.X` is the column (0 = left) and `Point.Y` is the row (0 = top), in
  playfield coordinates. Playfield (0,0) is drawn at screen column 1, row 1 (R7).
- No interfaces, DI, or extra projects (Principle I).
- Tests use small playfields (for example 7×5 or 10×6) built with `new Playfield(width, height)`,
  so expected cell states can be worked out by hand.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Solution and project skeleton, so that `dotnet build` and `dotnet test` work from the repository root.

- [X] T001 Create `.gitignore` at the repository root ignoring `bin/`, `obj/`, `.vs/`, `*.user`, and `TestResults/`
- [X] T002 Create the console project `src/Qix/Qix.csproj` (`dotnet new console -n Qix -o src/Qix`), then set `<TargetFramework>net10.0</TargetFramework>`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, and `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` (needed for `[LibraryImport]` P/Invoke). Leave `TreatWarningsAsErrors` off (R1). Replace the template `Program.cs` with an empty `return 0;` placeholder, and create the empty folders `src/Qix/Core/` and `src/Qix/Terminal/`
- [X] T003 Create the xUnit test project `tests/Qix.Tests/Qix.Tests.csproj` (`dotnet new xunit -n Qix.Tests -o tests/Qix.Tests`), set `net10.0` and `<Nullable>enable</Nullable>`, add a `<ProjectReference>` to `../../src/Qix/Qix.csproj`, and delete the template `UnitTest1.cs`
- [X] T004 Create the solution `Qix.slnx` at the repository root (`dotnet new sln --format slnx -n Qix`, then `dotnet sln add src/Qix/Qix.csproj tests/Qix.Tests/Qix.Tests.csproj`). Verify that `dotnet build` and `dotnet test` run from the root with no extra arguments

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core value types, the playfield grid, and the terminal I/O layer that every user story needs. This phase includes the **R2 check** for keyboard input in Git Bash and Windows Terminal.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete, and T012 (V1) must pass.

### Core types

- [X] T005 [P] Create `enum Cell { Empty, Border, Claimed, Trail }` in `src/Qix/Core/Cell.cs`. Add an XML doc comment on each value with its meaning from data-model.md (Empty = unclaimed area; Border = frame or edge between claimed and unclaimed; Claimed = claimed territory including demoted border; Trail = line currently being drawn)
- [X] T006 [P] Create `readonly record struct Point(int X, int Y)` with `Point operator +(Point p, Direction d)`, `enum Direction { Up, Down, Left, Right }`, and a `static class DirectionExtensions` with `(int Dx, int Dy) Offset(this Direction d)` (Up = (0,−1), Down = (0,+1), Left = (−1,0), Right = (+1,0)) in `src/Qix/Core/Geometry.cs`
- [X] T007 [P] (Revised 2026-10-01, research R2) Delete `src/Qix/Core/InputState.cs`: input now arrives one key press at a time, so the core takes `Game.Move(Direction)` and `Game.ToggleDraw()` calls instead of an `InputState`
- [X] T008 Create `sealed class Playfield` in `src/Qix/Core/Playfield.cs` with: the constants `DefaultWidth = 78` and `DefaultHeight = 22`; a constructor `Playfield(int width = DefaultWidth, int height = DefaultHeight)` that throws `ArgumentOutOfRangeException` unless the size is "any size ≥ 3×3"; a `Cell[,]` grid where the outer ring is `Border` and everything else is `Empty`; `Width`, `Height`; `bool Contains(Point p)` (inside the grid bounds); an indexer `Cell this[Point p]` that returns the cell (callers check `Contains` first); and `int InteriorCount => (Width − 2) × (Height − 2)`. Do not add `SetTrail`, `CloseTrail`, or `ClaimedPercent` yet (added in US2/US3) (depends on T005, T006)

### Terminal layer (glue, verified manually)

- [X] T009 [P] (Revised 2026-10-01) Reduce `src/Qix/Terminal/NativeMethods.cs` to what VT output needs: `[LibraryImport]` `kernel32` `GetStdHandle` (`STD_OUTPUT_HANDLE = −11`), `GetConsoleMode`, `SetConsoleMode`, and the constant `ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004`. Remove the input records, input-mode flags, virtual-key codes and `winmm` timer calls (research R2, R3, R4)
- [X] T010 [P] (Revised 2026-10-01) Delete `src/Qix/Terminal/KeyboardState.cs`. Keys are read with `Console.KeyAvailable` + `Console.ReadKey(intercept: true)` in `Program.cs` (T017), as in the developer's RectangleDotGame (research R2)
- [X] T011 (Revised 2026-10-01) Rework `sealed class TerminalSession : IDisposable` in `src/Qix/Terminal/TerminalSession.cs` (research R2, R3, R9). The constructor saves the original output console mode and sets `original | ENABLE_VIRTUAL_TERMINAL_PROCESSING` (ignore failure, e.g. when the host already handles VT). It sets `Console.OutputEncoding = Encoding.UTF8` and `Console.TreatControlCAsInput = true`, and writes `ESC[?1049h` (alternate screen) and `ESC[?25l` (hide cursor). No input-mode changes and no timer calls. `Dispose()` is idempotent: it writes `ESC[0m` `ESC[?25h` `ESC[?1049l`, sets `Console.TreatControlCAsInput = false`, and restores the output mode. Every exit path (Esc, Ctrl+C, completion screen, unhandled exception) MUST end in `Dispose()` (contract "Terminal guarantees", FR-016) (depends on T009)
- [X] T012 **R2 check (quickstart V1)**: temporarily make `src/Qix/Program.cs` open a `TerminalSession` and echo key presses: show the last `Key` read and a counter of arrow presses; Space flips a `Draw: ON/OFF` line; Esc or Ctrl+C quits. Run it in **Git Bash and in Windows Terminal** and confirm: (a) each arrow press is reported, and holding an arrow repeats; (b) Space flips `Draw`; (c) Esc and Ctrl+C quit and leave a usable prompt (cursor visible, normal colours); (d) the text is drawn in place (VT sequences interpreted, no `[` garbage). If any check fails in either terminal, STOP and revisit research R2. Once it passes, put `Program.cs` back to the `return 0;` placeholder (depends on T011, T013)
- [X] T013 Create `sealed class Renderer` in `src/Qix/Terminal/Renderer.cs` (research R3). It has a frame buffer of `(char Glyph, ConsoleColor? Color)` sized to the current window (`Console.WindowWidth` × `Console.WindowHeight`) and keeps the previously written frame. API: `Clear()` (fills the frame with spaces, default color), `Put(int col, int row, char glyph, ConsoleColor? color)`, `Text(int col, int row, string text, ConsoleColor? color = null)`, `Invalidate()` (forces a full redraw next time; called on resize), and `Present()`. `Present()` compares the frame with the previous one and, for **changed cells only**, appends `ESC[{row+1};{col+1}H`, an SGR color code, and the glyph. Map colours with an explicit table, not arithmetic (`ConsoleColor`'s numbering doesn't follow ANSI order): `null` → `39` (default), `White` → `97`, `Yellow` → `93`, `Red` → `91`, `DarkCyan` → `36`; any other colour throws `ArgumentOutOfRangeException`. Collect everything in one `StringBuilder`. It then writes everything with a **single** `Console.Out.Write` + `Flush`. It never writes the last screen row (R7) and writes nothing if no cell changed. If the window size changed, it reallocates the buffers and redraws in full (depends on T011)

**Checkpoint**: Foundation ready. `dotnet build` passes, and key input is confirmed in Git Bash and Windows Terminal (V1).

---

## Phase 3: User Story 1 - Move Along the Border (Priority: P1) 🎯 MVP

**Goal**: A launchable game showing the 78×22 framed playfield, a marker at the bottom center that moves along the frame with the arrow keys, and Esc to quit.

**Independent Test**: `dotnet run --project src/Qix`, then press each arrow. The marker follows the frame, turns corners, does nothing on ↑/↓ along a horizontal edge, and moves one cell per press. Esc returns to a usable prompt (quickstart V2, V9).

### Tests for User Story 1 ⚠️

> Write these tests FIRST and make sure they FAIL before implementing T015.

- [X] T014 [P] [US1] Write `MovementTests` in `tests/Qix.Tests/MovementTests.cs` using `new Game(new Playfield(10, 6))`: (1) the marker starts at `(Width / 2, Height − 1)` = (5, 5), `Mode == MarkerMode.OnBorder`, and `DrawOn == false`; for the default playfield it is (39, 21); (2) `Move(Direction.Left)` moves it to (4, 5), and `Right` moves it to (6, 5); (3) on the bottom edge away from a corner, `Up` with draw off leaves it in place (target is `Empty`); (4) `Down` on the bottom edge is blocked (outside the grid); (5) walking Left to (0, 5) and then `Up` moves it to (0, 4) (turning the corner); (6) at (0, 5), `Left` is blocked; (7) each `Move` call moves at most one cell; (8) a full lap of the frame (Left to the corner, Up, Right, Down, Left) returns it to the start, and every visited cell is `Border`

### Implementation for User Story 1

- [X] T015 [US1] Create `enum MarkerMode { OnBorder, Drawing }`, `sealed class Marker` (`Point Position`, `MarkerMode Mode`, `List<Point> Trail`, empty while `OnBorder`), and `sealed class Game` in `src/Qix/Core/Game.cs`. `Game(Playfield? playfield = null)` uses `new Playfield()` by default and puts the marker at `(Width / 2, Height − 1)` in mode `OnBorder` (data-model Marker). It exposes `Playfield`, `Marker`, and `bool DrawOn` (starts false). `void Move(Direction direction)` implements the `OnBorder` rows of the data-model transition table: target = Position + direction; if the target is outside the grid or `Claimed`, block; if the target is `Border`, move; if the target is `Empty` and draw is off, block. Leave the Empty + draw-on case (start drawing) as a blocked no-op for now; US2 adds it and `ToggleDraw()`. Make the T014 tests pass (depends on T008, T014)
- [X] T016 [US1] Create a static `PlayfieldView` in `src/Qix/Terminal/PlayfieldView.cs` with `Draw(Renderer r, Game game)`. It draws every playfield cell at screen `(x + 1, y + 1)` with the contract glyphs and colors: Empty `' '` default, Claimed `'░'` `DarkCyan`, Border `'█'` `White`, Trail `'█'` `Yellow`. It then draws the marker `'◆'` `Red` (bright red, SGR 91) at its position. On row 23 it writes the controls hint `←↑→↓ move   Space draw on/off   Esc quit` (FR-018). Leave row 0 empty for now; US2/US3 add the status line (depends on T013, T015)
- [X] T017 [US1] Implement the game loop in `src/Qix/Program.cs` (research R2, R4, R9). (1) If `Console.IsInputRedirected || Console.IsOutputRedirected`, write `Qix must be run in an interactive console.` to stderr and return exit code 1 (contract, quickstart V11). (2) Inside `using var session = new TerminalSession()` (its implicit `finally` restores the terminal on unhandled exceptions too), create `Renderer` and `Game`. (3) Loop: while `Console.KeyAvailable`, `ReadKey(intercept: true)`: Esc, or C with `ConsoleModifiers.Control`, quits; arrow keys call `game.Move(direction)`; other keys are ignored. If the window size differs from the last pass, call `renderer.Invalidate()`. If any key was handled or the view was invalidated, call `Clear`, `PlayfieldView.Draw`, and `Present`. Then `Thread.Sleep(10)`. (4) Return exit code 0 (depends on T011, T016)
- [X] T018 [US1] Run `dotnet build` and `dotnet test`, then play quickstart V2 (border movement) and V9 (Esc and Ctrl+C quit with terminal restored) in Git Bash and Windows Terminal. Confirm US1 acceptance scenarios 1–5 and that there is no flicker (SC-005)

**Checkpoint**: The game launches and the marker travels around the frame. This is the MVP (Principle III).

---

## Phase 4: User Story 2 - Draw a Line and Claim Territory (Priority: P2)

**Goal**: With drawing switched on (Space), moving off the border draws a trail. Reaching a border closes the line and claims every region except the largest (research R5). The new edges become travelable border.

**Independent Test**: Press Space, then hold ↑ from the bottom edge up to the top frame. The smaller side fills with `░` and the marker can walk along the new line (quickstart V3–V7).

### Tests for User Story 2 ⚠️

> Write these tests FIRST and make sure they FAIL before implementing T021–T022.

- [X] T019 [P] [US2] Write `DrawingTests` in `tests/Qix.Tests/DrawingTests.cs` using `Game(new Playfield(10, 6))`: (1) `ToggleDraw()` flips `DrawOn` on and off; (2) from (5, 5) with draw on, `Move(Up)` moves to (5, 4), the cell becomes `Trail`, `Mode == Drawing`, and `Marker.Trail` = [(5, 4)]; (3) still drawing, after `ToggleDraw()` (draw off) `Move(Up)` leaves the marker in place (FR-008); after `ToggleDraw()` again, `Move(Up)` moves; (4) drawing Up, then Left, then Down, then trying Right back onto the trail is blocked (FR-007); (5) moving into a `Claimed` cell while drawing is blocked. *Not testable through the game: after every close, each claimed area is walled off by border, so an Empty cell is never next to a Claimed one. The rule stays in `Move` as a guard;* (6) with draw on but on the border, `Move(Left)` along the frame still moves along the border; (7) when the drawing marker steps onto a `Border` cell, `Mode == OnBorder`, `Marker.Trail` is empty, no `Trail` cells remain, and `DrawOn == false` (US2 scenario 8)
- [X] T020 [P] [US2] Write `ClaimingTests` in `tests/Qix.Tests/ClaimingTests.cs`, driving `Playfield` directly with `SetTrail` + `CloseTrail()` (and through `Game` where noted): (1) **straight cut**: in a 10×6 field (interior 8×4), a vertical trail at x = 3 (y = 1..4) claims the 2×4 left side, and the 5×4 right side stays `Empty`; (2) **L-shape**: a trail cutting off a corner claims only the enclosed corner rectangle; (3) **one-cell step**: in a field with a claimed corner, a single trail cell that closes a 1-cell region claims it (spec Edge Case "One-cell steps"); (4) **ends on a claimed edge**: a second line from the outer frame to the edge of the first claimed region fills correctly; (5) **pocket**: a trail that runs alongside itself and encloses a separate 1-cell gap claims both the gap and the smaller main side, so more than two regions are handled; (6) **tie rule**: in an 11×6 field (interior 9×4), a vertical trail at x = 5 splits the interior into two equal 4×4 halves; the region that contains the topmost (then leftmost) `Empty` cell stays unclaimed, and the other is claimed (FR-010); (7) **demotion**: after a close, every `Border` cell has at least one `Empty` cell among its 8 neighbours (or no `Empty` cells remain), and former border cells surrounded only by claimed cells are `Claimed` (FR-012); (8) after `CloseTrail()`, no `Trail` cells remain and there is exactly one connected `Empty` region; (9) via `Game`: after a close, the marker can walk along the new edge without draw, but cannot step into the claimed interior (US2 scenario 4)

### Implementation for User Story 2

- [X] T021 [US2] In `src/Qix/Core/Playfield.cs`, add `void SetTrail(Point p)` (only allowed when the cell is `Empty`; otherwise throw `InvalidOperationException`) and `void CloseTrail()`, implementing research R5 in order: (1) every `Trail` cell becomes `Border`; (2) find all 4-connected components of `Empty` cells with an iterative BFS (`Queue<Point>`, no recursion), scanning rows top to bottom and columns left to right, so the first component found contains the topmost-then-leftmost `Empty` cell; (3) keep the **largest** component `Empty`. On a size tie, keep the one found first (it contains the topmost/leftmost cell, which is the spec tie rule). Set every other component to `Claimed`; (4) **border demotion**: every `Border` cell with no `Empty` cell among its **8** neighbours becomes `Claimed`. Compute the demotion set first, then apply it, so the result doesn't depend on scan order. Uphold the data-model invariants: "The outer ring is never `Empty` or `Trail`", and "every `Border` cell has at least one `Empty` cell among its 8 neighbours (or there are no `Empty` cells left at all)". Make the T020 Playfield-level tests pass (depends on T008, T020)
- [X] T022 [US2] In `src/Qix/Core/Game.cs`, add `void ToggleDraw()` (flips `DrawOn`) and complete `Move` with the rest of the data-model transition table. `OnBorder` + draw on + `Empty` target: move, call `SetTrail(target)`, add the target to `Marker.Trail`, and set mode to `Drawing`. `Drawing` + draw off: blocked. `Drawing` + draw on + `Empty`: move and `SetTrail`. `Drawing` + draw on + `Border`: move, `Playfield.CloseTrail()`, clear `Marker.Trail`, set mode to `OnBorder`, and set `DrawOn = false`. `Drawing` + draw on + `Trail`/`Claimed`/outside: blocked. In `src/Qix/Program.cs`, Space calls `game.ToggleDraw()`. In `src/Qix/Terminal/PlayfieldView.cs`, write `Draw: ON` / `Draw: OFF` on row 0 at column 22 (FR-006). Make all T019 and T020 tests pass (depends on T015, T017, T019, T021)
- [X] T023 [US2] Run `dotnet test`, then play quickstart V3 (straight cut), V4 (L-shaped/zig-zag lines ending on the frame and on previous lines), V5 (blocks while drawing), V6 (draw off mid-line), and V7 (pocket). Confirm US2 acceptance scenarios 1–9, that the trail shows in yellow and claimed cells as dark-cyan `░` (FR-011), and that the fill appears with no noticeable delay (SC-003)

**Checkpoint**: Drawing and claiming work, and the game is still launchable and playable (US1 behaviour unchanged).

---

## Phase 5: User Story 3 - Track Progress and Complete the Playfield (Priority: P3)

**Goal**: The status line shows `Claimed: NN% / 75%` and updates after every fill. At ≥ 75%, a completion box appears, input pauses, and R restarts. A too-small terminal shows a resize message.

**Independent Test**: Claim regions until the status shows 75%. The completion box appears, arrows do nothing, and R gives a fresh 0% playfield (quickstart V8, V10).

### Tests for User Story 3 ⚠️

> Write these tests FIRST and make sure they FAIL before implementing T025–T026.

- [X] T024 [P] [US3] Write `ProgressTests` in `tests/Qix.Tests/ProgressTests.cs`: (1) a new `Playfield` has `ClaimedPercent == 0`; a new `Game` has `Phase == GamePhase.Playing` and `TargetPercent == 75`; (2) **rounding down**: in a 12×6 field (interior 10×4 = 40 cells), claiming 11 cells (27.5%) gives `ClaimedPercent == 27`, and trail cells turned border count as claimed (R6: "interior cells that are not Empty"); (3) a close that brings the field to exactly 75% (for example, 30 of 40 interior cells non-`Empty`) sets `Phase == Complete`; (4) a close that reaches 74.x% stays `Playing`; (5) while `Complete`, `Move` and `ToggleDraw` change nothing; (6) `Restart()` gives a fresh playfield of the same size with `ClaimedPercent == 0`, the marker back at `(Width / 2, Height − 1)` in `OnBorder`, `DrawOn == false`, and `Phase == Playing` (FR-015); (7) `ClaimedPercent` never decreases across a sequence of closes (data-model invariant); (8) while drawing (trail cells placed, line not closed yet), `ClaimedPercent` is unchanged (FR-013)

### Implementation for User Story 3

- [X] T025 [US3] In `src/Qix/Core/Playfield.cs`, add `int ClaimedPercent`: `100 × (count of interior cells that are not Empty) / InteriorCount` using **integer division** (floor), where the interior is every cell with `1 ≤ x ≤ Width − 2` and `1 ≤ y ≤ Height − 2` (research R6). Compute it once at the end of `CloseTrail()` and store it in a field that starts at 0, so the value only changes when a fill happens (FR-013 "update right after every fill"); trail cells drawn mid-line MUST NOT change it (depends on T021, T024)
- [X] T026 [US3] In `src/Qix/Core/Game.cs`, add `enum GamePhase { Playing, Complete }`, `GamePhase Phase` (starts `Playing`), `const int TargetPercent = 75`, and `void Restart()` (new `Playfield` with the same width/height, new marker at the start position, `DrawOn = false`, phase → `Playing`). Make `Move` and `ToggleDraw` return immediately while `Complete`. After `CloseTrail()`, set phase → `Complete` when `Playfield.ClaimedPercent >= TargetPercent` (data-model Phase transitions). Make all T024 tests pass (depends on T022, T025)
- [X] T027 [US3] In `src/Qix/Terminal/PlayfieldView.cs`, draw `Claimed: {ClaimedPercent}% / {TargetPercent}%` on row 0 at column 1, keeping `Draw: ON/OFF` after it (FR-013). When `Phase == Complete`, draw a centered box over the playfield (border characters `┌─┐│└┘`) containing `Playfield complete! {NN}% claimed   R = restart   Esc = quit` (contract "Completion screen", FR-014), keeping the final playfield visible around it (depends on T022, T026)
- [X] T028 [US3] In `src/Qix/Program.cs`, handle the R key: if `game.Phase == Complete`, call `game.Restart()` and redraw; while `Playing`, R is ignored (a key press is consumed when read, so it can't trigger a restart later). Esc keeps quitting from every screen (depends on T017, T026)
- [X] T029 [US3] In `src/Qix/Program.cs`, add the too-small-terminal screen (FR-017, research R9). On every loop pass (which also covers shrinking mid-game at no extra cost), while `Console.WindowWidth < 80 || Console.WindowHeight < 25`, ignore game keys and show `Please resize the window to at least 80×25 (current: W×H). Esc to quit.` via the renderer (update W×H whenever the size changes), read keys, and return exit code 0 on Esc or Ctrl+C. Start the game as soon as the size is big enough. Shrinking below the minimum mid-game is out of scope (R9) (depends on T017)
- [X] T030 [US3] Run `dotnet test`, then play quickstart V8 (completion and restart), V9 (Esc on the completion screen), and V10 (small window). Confirm US3 acceptance scenarios 1–5 and that the displayed percentage matches the true claimed share after every fill (SC-004)

**Checkpoint**: All three user stories work. The game can be played from 0% to completion and restarted.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Full validation against the spec's success criteria and the constitution's definition of done.

- [X] T031 [P] Update the Source Code tree in `specs/001-player-movement-claiming/plan.md` so it matches the files that were actually created (including `src/Qix/Terminal/PlayfieldView.cs` and `.gitignore`)
- [X] T032 Review `src/Qix/Core/*.cs` to confirm there are no references to `System.Console`, `Stopwatch`, `DateTime`, `Thread`, or `Random`, and that there are no unused members or speculative abstractions (Principles I and II)
- [X] T033 Run the full quickstart.md checklist V1–V11 in Windows Terminal, including V11 (`echo x | dotnet run --project src/Qix` writes the error to stderr and exits with code 1)
- [ ] T034 Playtest SC-002: draw 20 different line shapes (straight, L-shaped, zig-zag, ending on the frame, ending on claimed edges, pockets) and confirm all 20 fill the correct region. Then do a 10-minute session from 0% to completion (SC-005, SC-006) with no stutter, flicker, crash, or freeze. Fix any defect with a new unit test in the matching `tests/Qix.Tests/*Tests.cs` file
- [X] T035 Definition of done: from the repository root, `dotnet build` passes with no errors and no new warnings, `dotnet test` passes, and the game launches (constitution Development Workflow)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies. T001–T004 run in order (T004 needs both projects).
- **Foundational (Phase 2)**: Depends on Setup. Blocks all user stories. **The T012 V1 spike is a go/no-go gate.**
- **US1 (Phase 3)**: Depends on Foundational.
- **US2 (Phase 4)**: Depends on US1 (`Game.Step` and `Program.cs` are extended, not duplicated).
- **US3 (Phase 5)**: Depends on US2 (the phase change happens after `CloseTrail()`, and the percentage counts claimed cells).
- **Polish (Phase 6)**: Depends on all user stories.

The stories are sequential because each one extends the same small set of files (`Game.cs`,
`Playfield.cs`, `Program.cs`). Each one still ends in a launchable, demoable game (Principle III).

### Task-level dependency graph

```text
T001 → T002 → T003 → T004
                       │
       ┌───────┬───────┼────────┬────────┐
      T005    T006    T007     T009
       └───┬───┘       │      ┌─┴──────┐
          T008         └─────T010     T011
                              └──┬─────┤
                               T012   T013
                                 ▼ (gate)
US1:  T014 → T015 → T016 → T017 → T018
US2:  T019, T020 → T021 → T022 → T023
US3:  T024 → T025 → T026 → T027, T028, T029 → T030
Polish: T031, T032 → T033 → T034 → T035
```

### Within Each User Story

- Tests are written first and must fail before the matching implementation.
- `Playfield` before `Game` before `Terminal` view before `Program.cs`.
- Finish each story with its manual playtest task before moving on.

### Parallel Opportunities

- **Phase 2**: T005, T006, T007, T009, and T010 touch different files and can run together. T011 follows T009.
- **US2**: T019 and T020 (two test files) can be written together.
- **US3**: T024 can be written while US2's playtest (T023) is running. T027, T028, and T029 can be split, but T028 and T029 both edit `Program.cs`, so do them one after the other.
- **Polish**: T031 and T032 can run in parallel.

---

## Parallel Example: Foundational core types

```text
Task: "Create Cell enum in src/Qix/Core/Cell.cs"                         (T005)
Task: "Create Point/Direction in src/Qix/Core/Geometry.cs"               (T006)
Task: "Delete src/Qix/Core/InputState.cs"                                (T007)
Task: "Reduce P/Invoke declarations in src/Qix/Terminal/NativeMethods.cs" (T009)
```

## Parallel Example: User Story 2 tests

```text
Task: "Write DrawingTests in tests/Qix.Tests/DrawingTests.cs"    (T019)
Task: "Write ClaimingTests in tests/Qix.Tests/ClaimingTests.cs"  (T020)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 (Setup) and Phase 2 (Foundational). **Pass the V1 gate (T012).**
2. Complete Phase 3 (US1).
3. **STOP and VALIDATE**: `dotnet test`, then quickstart V2 + V9. The marker moves around the frame.

### Incremental Delivery

1. Setup + Foundational: the input approach is proven.
2. + US1: a launchable game with border movement (MVP).
3. + US2: the core Qix mechanic, drawing and claiming.
4. + US3: a goal and an end state (percentage, 75% completion, restart, resize screen).
5. Polish: full quickstart pass and success-criteria playtests.

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks.
- Commit after each task or logical group. Each story checkpoint should be a commit on which the game builds and launches.
- 2026-10-01: the first V1 check (raw Win32 input, hold Space to draw) failed in Git Bash. The controls were changed by clarification (spec Clarifications; research R2), and T007, T009–T012 and the dependent story tasks were revised.
