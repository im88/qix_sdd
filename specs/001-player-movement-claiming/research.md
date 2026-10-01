# Research: Player Movement and Territory Claiming

**Feature**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md) | **Date**: 2026-10-01

## R1. Runtime and project setup

- **Decision**: C# on .NET 10 (the current LTS; SDK 10.0.102 is installed). There is one console
  project, `src/Qix`, and one xUnit test project, `tests/Qix.Tests`, tied together by `Qix.slnx`
  at the repository root. Nullable reference types are enabled, and `TreatWarningsAsErrors` is
  off.
- **Rationale**: This is what the constitution's Technology & Platform Constraints require.
  `dotnet build` and `dotnet test` at the root find the solution file without extra arguments.
- **Alternatives considered**: A single project with no tests contradicts Principle II. A
  separate class library for the game logic adds a third project; folder separation inside one
  project is enough (Principle I).

## R2. Keyboard input (revised 2026-10-01)

The first decision was to read raw Win32 input records (`ReadConsoleInputW`) to detect held
keys, so that Space could be *held* to draw. The T012 check showed that this doesn't work in Git
Bash, which the developer wants to use: Git Bash passes keys on as text, so every key-down is
followed by an immediate key-up and held keys can't be seen. The controls were changed through
clarification (spec, Clarifications 2026-10-01).

- **Decision**: Read keys with `Console.KeyAvailable` + `Console.ReadKey(intercept: true)`, the
  approach the developer's earlier RectangleDotGame uses successfully in Git Bash. Each arrow
  press moves the marker one cell; holding an arrow repeats through the keyboard's auto-repeat.
  Space toggles drawing on and off. `Console.TreatControlCAsInput = true`, so Ctrl+C arrives as
  an ordinary key and quits like Esc.
- **Rationale**: This works in both Windows Terminal and Git Bash, needs no P/Invoke for input,
  and makes the game loop event-driven (no movement timer, see R4). It is simpler than the
  Win32 approach (Principle I).
- **Accepted trade-off**: Auto-repeat starts after the keyboard's repeat delay (about
  250–500 ms), so a held arrow pauses briefly after the first step. Classic Qix's "hold to draw"
  becomes a toggle.
- **Alternatives considered**:
  - Win32 `ReadConsoleInputW` with key-up tracking: **rejected after testing**, no usable
    key-ups in Git Bash (see above).
  - `GetAsyncKeyState`: gives true held state but ignores window focus, so the game would react
    to typing in other windows. **Rejected**.
  - Shift + arrows to draw: closer to "hold to draw", but Shift+arrow handling in mintty is less
    certain. **Rejected** by the developer in favour of the toggle.

## R3. Rendering without flicker

- **Decision**: Render into an in-memory frame buffer of cells (character + color). Each frame
  is compared with the previous one, and **only changed cells** are emitted as ANSI/VT escape
  sequences (cursor position + color + character), collected in one `StringBuilder` and written
  with a single `Console.Out.Write`. On start, the game switches to the alternate screen buffer
  (`ESC[?1049h`), hides the cursor (`ESC[?25l`), and turns on
  `ENABLE_VIRTUAL_TERMINAL_PROCESSING` on the output handle (the one remaining P/Invoke). On exit, all of this is reversed. Output encoding is
  UTF-8.
- **Rationale**: Sending only the differences, in one write, avoids flicker and keeps output
  tiny: moving the marker changes two cells per step. The alternate screen leaves the user's
  scrollback untouched and makes restoring the terminal trivial (FR-016, SC-007).
- **Alternatives considered**: Calling `Console.Clear()` and redrawing everything causes visible
  flicker. Using `Console.SetCursorPosition` + `Console.Write` per cell is slow, because each
  call is a separate write. A TUI library (Terminal.Gui, Spectre.Console) is a dependency
  Principle I does not justify for a single grid view.

## R4. Game loop (revised 2026-10-01)

- **Decision**: An event-driven loop. Each iteration handles every key in the buffer
  (`while (Console.KeyAvailable) ReadKey`), checks whether the window size changed (and forces
  a full redraw if so), redraws if anything changed, and then sleeps 10 ms. Each arrow press
  calls `Game.Move(direction)` once; there is no movement timer.
- **Rationale**: With one cell per key press (R2), there is nothing to time. Polling with a
  short sleep, rather than a blocking `ReadKey`, lets the loop also notice window resizes and
  serve the too-small-window screen. 10 ms is well below what a player can notice, and the CPU
  stays idle.
- **Alternatives considered**: A blocking `ReadKey` loop (like RectangleDotGame) is slightly
  simpler but can't react to a resize until the next key press. The earlier fixed 50 ms
  timestep with `timeBeginPeriod(1)` is no longer needed.

## R5. Claiming algorithm

- **Decision**: The playfield is a grid. Each cell is `Empty`, `Border`, `Claimed`, or `Trail`.
  When a drawing marker steps onto a `Border` cell:
  1. All `Trail` cells become `Border`.
  2. Find the 4-connected components of `Empty` cells (breadth-first flood fill).
  3. Keep the **largest** component unclaimed; every other component becomes `Claimed`. If the
     largest components tie in size, keep the one that contains the topmost unclaimed cell
     (leftmost if several are equally high). This is the spec's tie rule.
  4. **Border demotion**: any `Border` cell that has no `Empty` cell among its 8 neighbours
     becomes `Claimed`, because it no longer separates claimed from unclaimed area (FR-012).
- **Rationale**: A line can't cross itself, but it can run alongside itself and enclose small
  pockets, so a single close may produce **more than two** regions. "Keep the largest, claim the
  rest" generalises the spec's "claim the smaller side" and handles pockets correctly. Using 8
  neighbours in the demotion step keeps frame corners and line corners traversable while edges
  still border open area. The grid has about 1,500 cells, so a full flood fill is far below one
  millisecond (SC-003).
- **Alternatives considered**: Polygon area calculations on line vertices are more complex and
  fragile for grid paths. Filling only from the two cells on either side of the line misses
  enclosed pockets.

## R6. Claimed percentage

- **Decision**: `percent = floor(100 × (interior cells that are not Empty) / interior cells)`,
  where the interior is everything inside the outer frame. It is computed with integer
  arithmetic after each fill. Completion happens when `percent ≥ 75`.
- **Rationale**: Lines drawn on the interior count as claimed, as in Qix. Rounding down with
  integers means "75%" is only shown once it has actually been reached (spec edge case, SC-004).

## R7. Screen layout and glyphs

- **Decision**: The minimum terminal size is 80×25. The playfield is 78×22 cells including the
  frame (76×20 = 1,520 interior cells), drawn at screen column 1, row 1. Row 0 is the status
  line (`Claimed: NN% / 75%   Draw: ON|OFF`) and row 23 is the controls hint. **One character per cell.**
  Glyphs: unclaimed `' '`, claimed `'░'` (dark cyan), border `'█'` (white), trail `'█'`
  (yellow), marker `'◆'` (bright red). The marker starts at the bottom frame, centered
  (x = 39, y = 21).
- **Rationale**: This fits the size fixed in the spec's assumptions and leaves the last screen
  row free, so writing the bottom-right cell can't scroll the screen.
- **Accepted trade-off**: Terminal cells are about twice as tall as they are wide, so vertical
  movement *looks* faster than horizontal movement at the same step rate. Equalising it (using
  two characters per cell, or a slower vertical step) is deferred until playtesting shows it
  matters (Principle I).

## R8. Testing scope

- **Decision**: xUnit tests cover the core logic only: movement rules (border-only travel,
  blocked moves), drawing rules (trail, blocks on trail and claimed area, draw toggle),
  claiming (straight, L-shaped, and pocket-forming lines, the tie rule, border demotion), the
  percentage, and the completion threshold. Rendering, keyboard input, and the loop are verified by
  the manual playtest in [quickstart.md](quickstart.md).
- **Rationale**: This is what Principle II asks for: tests where bugs are costly and hard to spot
  by playing, and manual checks for glue code.

## R9. Terminal edge cases

- **Decision**: At launch, if input or output is redirected (not a real console), the game
  writes an error to stderr and exits with code 1. If the window is smaller than 80×25, it
  shows a "please resize to at least 80×25" message and waits until the window is resized or
  Esc is pressed (FR-017). Ctrl+C is read as an ordinary key (`Console.TreatControlCAsInput`,
  R2) and quits like Esc, so the terminal is restored in every case. A resize *during* play
  triggers a full redraw on the next frame. Shrinking the window mid-game below the minimum
  size is out of scope for this feature.
