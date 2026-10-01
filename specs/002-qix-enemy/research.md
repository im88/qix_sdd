# Research: Qix Enemy and Lives

**Feature**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md) | **Date**: 2026-10-01

Decisions R1–R9 of [001 research](../001-player-movement-claiming/research.md) still apply unless
replaced here. Numbering continues from there.

## R10. Time in the core logic

- **Decision**: `Game.Advance(TimeSpan elapsed)` is the only way time enters `Core`. It adds the
  elapsed time to an accumulator, takes one Qix step per full `QixStepInterval` (125 ms, about 8
  steps per second, FR-002), and counts down the life-lost pause (R14). It returns `true` if
  anything visible changed, so the loop knows to redraw. `Program` measures real time with a
  `Stopwatch` and calls `Advance` once per loop pass, after handling keys. Elapsed time per call
  is capped at 250 ms, so a stalled loop (window drag, debugger) causes at most two catch-up
  steps rather than a jump.
- **Rationale**: `Core` stays free of the clock (Principle II): tests call
  `Advance(Game.QixStepInterval)` to take exactly one step. The 10 ms loop from R4 is already
  fast enough; at 125 ms per step the Qix moves on every 12th–13th pass.
- **While the window is too small**, `Program` doesn't call `Advance` (the game is paused) and
  restarts the stopwatch, so the Qix doesn't jump when the window is enlarged again.
- **Alternatives considered**: A separate timer thread brings locking for no benefit. Stepping
  the Qix once per loop pass ties its speed to the sleep accuracy (10 ms is really 10–16 ms on
  Windows). Injecting an `IClock` interface is an abstraction with one use (Principle I);
  passing elapsed time in is enough.

## R11. Randomness

- **Decision**: `Game` takes an optional `Random` in its constructor (default: `new Random()`).
  It is used only to pick the Qix's starting diagonal direction, at start and on restart. Tests
  pass a seeded `Random` or set `Qix.Velocity` directly.
- **Rationale**: This is the constitution's "inject randomness where tests need to control it",
  with the smallest possible surface.

## R12. Qix movement and bouncing

The Qix is one cell with a position and a velocity `(Dx, Dy)`, each ±1. It starts at the center
of the playfield, `(Width / 2, Height / 2)` = (39, 11), which is always unclaimed on a fresh
playfield.

- **Open cells** for the Qix are `Empty` and `Trail`. **Walls** are `Border`, `Claimed`, and
  anything outside the playfield. Entering a `Trail` cell is a *contact* (R13), not a move.
- **One step** tries candidate moves in order and takes the first one that isn't blocked:
  1. The current diagonal `(Dx, Dy)`.
  2. The reflected diagonal: flip `Dx` if the horizontal neighbour `(x+Dx, y)` is a wall, flip
     `Dy` if the vertical neighbour `(x, y+Dy)` is a wall, and flip both if neither is (the Qix
     hit the tip of a corner).
  3. The two remaining diagonals.
  4. Straight moves: `(Dx, 0)`, `(−Dx, 0)`, `(0, Dy)`, `(0, −Dy)`.
  5. Nothing: the Qix stays put (a single-cell region).

  After a diagonal move, the velocity becomes that diagonal. After a straight move, only the
  moved component changes, so a Qix in a one-cell-wide corridor runs straight along it and
  bounces at its ends (spec edge case "Narrow areas").
- **No corner cutting**: a diagonal move is blocked when *neither* orthogonal neighbour is open.
  Without this the Qix could slip diagonally through a gap where two walls meet at a corner.
- **Rationale**: Ball-like bouncing as the spec asks, deterministic for a given start, and always
  confined to the region it is in. The candidate order means the common case (open space, flat
  wall) takes one or two checks.
- **Alternatives considered**: Classic Qix's erratic random walk is out of scope (spec
  Assumptions). Allowing corner cutting is simpler but lets the Qix leave its region through
  diagonal gaps, which would break FR-003.

## R13. Contact between the Qix and the line

- **Decision**: A contact (a lost life) happens when, while a line exists:
  1. A Qix step's chosen candidate enters a `Trail` cell (the marker's own cell is the last
     `Trail` cell, so this covers the marker too), or
  2. A Qix diagonal step is pinched between two non-`Empty` orthogonal neighbours and at least
     one of them is `Trail`. The Qix is squeezing past the line, so it touches it. (If both are
     walls, the move is just blocked, R12.)
  3. The player moves the drawing marker onto the Qix's cell.

  On contact the Qix stays where it is.
- **Order within one loop pass**: keys are handled before `Advance`. A move that closes the line
  turns the trail into border before the Qix steps, so the close wins (FR-012). The Qix never
  enters `Border`, so a marker on the border is always safe (spec US2 scenario 5).
- **Rationale**: Rule 2 closes the one geometric loophole: a 4-connected line could otherwise be
  crossed diagonally without the Qix ever entering one of its cells. Rule 3 makes "running into
  the Qix" count the same as the Qix running into you.
- **Alternatives considered**: Counting any Qix adjacent to the line as a contact makes the Qix
  effectively three cells wide and harsher than the spec's "moves onto". Checking contact every
  loop pass instead of every Qix step changes nothing, because the trail only changes on key
  presses (handled by rule 3).

## R14. Losing a life, pause, and game over

- **Decision**: On contact, `Lives` drops by one. If lives remain, the phase becomes `LifeLost`
  for `LifeLostPause` (1 s); the line stays on screen, drawn in red, so the player sees where
  they were hit. When the pause ends, the trail cells become `Empty` again, the marker returns to
  `Marker.LineStart` (the border cell it left from), the marker mode becomes `OnBorder`, drawing
  switches off, and the phase returns to `Playing`. If no lives remain, the phase becomes
  `GameOver` immediately and the red line stays visible under the game-over box.
- **Input**: `Move` and `ToggleDraw` act only in `Playing` (001 checked only for `Complete`).
  The Qix doesn't move outside `Playing`. R restarts from `Complete` or `GameOver`; Esc and
  Ctrl+C always quit.
- **Rationale**: Showing the line during the pause answers "what just happened?" (SC-006)
  without extra animation code. Resetting at the end of the pause, not at the moment of contact,
  keeps the visible state and the game state in step.
- **Alternatives considered**: A blinking marker or full-screen flash needs a frame timer in the
  view; the red line gives the same information more simply.

## R15. Which region is claimed

- **Decision**: `Playfield.CloseTrail(Point keep)` keeps the `Empty` region that contains `keep`
  (the Qix's position) and claims every other region, regardless of size. This replaces "keep
  the largest" and its tie rule (R5 step 3). Border demotion and the percentage work as before.
- **Rationale**: FR-011. The Qix is always on an `Empty` cell (it never enters `Trail`, R13), so
  after the trail turns to border the Qix is in exactly one region. Pockets are claimed
  automatically because they never contain the Qix.
- **Existing tests**: 001's `ClaimingTests` and `ProgressTests` assume the largest region is kept.
  They must be updated to place the Qix explicitly in the region that should stay open; the
  tie-rule tests are replaced by "the Qix's region is kept even when it is smaller".

## R16. Screen additions

- **Decision**: The Qix is drawn as `●` in bright magenta, a color not used by anything else.
  The status line gains `Lives: N` at column 36. During `LifeLost` the trail is drawn red instead
  of yellow and the status line shows `Hit!` after the lives. Game over uses the same centered box
  as completion: `Game over! NN% claimed   R = restart   Esc = quit`. The renderer's color
  table gains `Magenta → 95`.
- **Rationale**: One cell, one character, as in R7. Magenta stands out against white border,
  yellow line, red marker, and dark-cyan claimed area (FR-004).
