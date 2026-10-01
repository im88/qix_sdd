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

## R2. Detecting held keys (hold Space to draw, hold arrows to move)

The spec requires knowing whether keys are *held*: the marker moves while an arrow is held
(FR-004), and drawing only happens while Space is held (FR-006, FR-008). Playing also requires
**Space and an arrow held at the same time**.

- **Decision**: Read raw console input records with the Win32 functions
  `GetNumberOfConsoleInputEvents` and `ReadConsoleInputW` (P/Invoke into `kernel32`). Each
  `KEY_EVENT_RECORD` has a `bKeyDown` flag, so both key-down and key-up are reported. The game
  keeps a set of currently pressed keys, plus the order in which the arrow keys were pressed so
  that the most recently pressed one wins. Auto-repeat key-down events are ignored. A
  `FOCUS_EVENT` that reports focus loss clears the pressed set, so keys can't get "stuck" down.
- **Rationale**: This is the only option that gives real key-up information, respects window
  focus, and needs no third-party package. Windows Terminal passes full key-down and key-up
  records to console apps through ConPTY's win32-input-mode; classic conhost does so natively.
  The constitution requires Windows Terminal only, so being Windows-only is acceptable.
- **Risk**: Whether Windows Terminal delivers the key-up records must be checked on the target
  machine before the rest of the input work is built on it. The quickstart includes a check
  (V1), and the first input task in `tasks.md` should be a short spike that confirms it.
- **Alternatives considered**:
  - `Console.ReadKey` / `Console.KeyAvailable`: these report key-down only. "Held" would have to
    be guessed from auto-repeat, which starts after a delay of about 250–500 ms (the marker
    would stutter at the start of every move) and only repeats the *last* key pressed. Holding
    Space and then pressing an arrow would look like Space had been released. **Rejected**: it
    cannot satisfy FR-006/FR-008.
  - `GetAsyncKeyState`: this gives true held state but ignores focus, so the game would react to
    typing in other windows. **Rejected**.
  - Making Space a toggle for draw mode: this would work with `Console.ReadKey`, but it changes
    the spec's controls. **Kept as a fallback** if R2's risk check fails; that change would
    have to go back through `/speckit-clarify`.

## R3. Rendering without flicker

- **Decision**: Render into an in-memory frame buffer of cells (character + color). Each frame
  is compared with the previous one, and **only changed cells** are emitted as ANSI/VT escape
  sequences (cursor position + color + character), collected in one `StringBuilder` and written
  with a single `Console.Out.Write`. On start, the game switches to the alternate screen buffer
  (`ESC[?1049h`), hides the cursor (`ESC[?25l`), and turns on
  `ENABLE_VIRTUAL_TERMINAL_PROCESSING`. On exit, all of this is reversed. Output encoding is
  UTF-8.
- **Rationale**: Sending only the differences, in one write, avoids flicker and keeps output
  tiny: moving the marker changes two cells per step. The alternate screen leaves the user's
  scrollback untouched and makes restoring the terminal trivial (FR-016, SC-007).
- **Alternatives considered**: Calling `Console.Clear()` and redrawing everything causes visible
  flicker. Using `Console.SetCursorPosition` + `Console.Write` per cell is slow, because each
  call is a separate write. A TUI library (Terminal.Gui, Spectre.Console) is a dependency
  Principle I does not justify for a single grid view.

## R4. Game loop and timing

- **Decision**: Use a fixed-timestep loop driven by `Stopwatch`. The game logic advances in
  discrete **movement steps of 50 ms** (20 cells per second); a full horizontal crossing of the
  playfield takes about 4 s. Each loop iteration reads input, runs as many steps as the elapsed
  time allows (at most 3 per iteration, so it can't spiral after a pause), renders if anything
  changed, and then sleeps 1 ms. During play, `timeBeginPeriod(1)` / `timeEndPeriod(1)`
  (`winmm`) raise the Windows timer resolution so that sleeps are actually about 1 ms.
- **Rationale**: Windows' default timer granularity of about 15.6 ms would make 50 ms steps land
  unevenly (47 / 62 ms), which shows as uneven movement (SC-005). Keeping the logic step-based
  means the logic never reads the clock, so tests can drive it step by step deterministically
  (Principle II).
- **Alternatives considered**: `PeriodicTimer` / `System.Threading.Timer` have the same timer
  granularity and add threading. A busy-wait loop uses 100% of a CPU core.

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
  line (`Claimed: NN% / 75%`) and row 23 is the controls hint. **One character per cell.**
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
  blocked moves), drawing rules (trail, blocks on trail and claimed area, releasing Space),
  claiming (straight, L-shaped, and pocket-forming lines, the tie rule, border demotion), the
  percentage, and the completion threshold. Rendering, Win32 input, and the loop are verified by
  the manual playtest in [quickstart.md](quickstart.md).
- **Rationale**: This is what Principle II asks for: tests where bugs are costly and hard to spot
  by playing, and manual checks for glue code.

## R9. Terminal edge cases

- **Decision**: At launch, if input or output is redirected (not a real console), the game
  writes an error to stderr and exits with code 1. If the window is smaller than 80×25, it
  shows a "please resize to at least 80×25" message and waits until the window is resized or
  Esc is pressed (FR-017). Ctrl+C is handled with `Console.CancelKeyPress` (cancel the default
  kill, request a clean quit) so the terminal is restored in every case. A resize *during* play
  triggers a full redraw on the next frame. Shrinking the window mid-game below the minimum
  size is out of scope for this feature.
