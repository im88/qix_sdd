# Data Model: Qix Enemy and Lives

**Feature**: [spec.md](spec.md) | **Research**: [research.md](research.md)

This extends the [001 data model](../001-player-movement-claiming/data-model.md). Only new and
changed members are listed. Everything lives in `src/Qix/Core` and stays free of the console and
the clock.

## Cell (unchanged)

How the Qix sees each cell (R12):

| Value     | Qix may enter?                           |
|-----------|------------------------------------------|
| `Empty`   | Yes                                      |
| `Trail`   | Entering it is a contact (R13)           |
| `Border`  | No, it is a wall                         |
| `Claimed` | No, it is a wall                         |

## Velocity (new, readonly record struct)

`Dx`, `Dy`, each −1 or +1 while moving diagonally. Kept separate from `Direction`, which stays the
marker's four orthogonal directions.

## Qix (new)

| Field      | Type       | Notes                                                    |
|------------|------------|----------------------------------------------------------|
| `Position` | `Point`    | Starts at `(Width / 2, Height / 2)`; always an `Empty` cell |
| `Velocity` | `Velocity` | Random diagonal at start and restart (R11); settable for tests |

**Step** (one per `QixStepInterval`, only in `Playing`): try candidates in the R12 order; the
first candidate that is not blocked is taken. Its outcome is either *Move* (position and velocity
update) or *Contact* (R13; the Qix stays put). If every candidate is blocked, the Qix stays put.

**Invariants**

- The Qix is never on a `Border`, `Claimed`, or `Trail` cell, or outside the playfield.
- After any `CloseTrail`, the Qix's cell is still `Empty`.

## Marker (changed)

| Field       | Type     | Notes                                                             |
|-------------|----------|-------------------------------------------------------------------|
| `LineStart` | `Point`  | **New.** The border cell the marker left from when the current line began; the respawn point after a lost life |

## Playfield (changed)

| Member                | Notes                                                            |
|-----------------------|------------------------------------------------------------------|
| `CloseTrail(Point keep)` | **Changed.** Trail → border; keep the `Empty` region containing `keep`, claim all others; demote border; update percentage (R15) |
| `ClearTrail()`        | **New.** Every `Trail` cell becomes `Empty` again (after a lost life) |

The "keep the largest region" rule and its tie rule are removed.

## Game (changed)

| Member              | Notes                                                              |
|---------------------|--------------------------------------------------------------------|
| `Game(Playfield? playfield = null, Random? random = null)` | **Changed.** Random picks the Qix's start direction (R11) |
| `Qix`               | **New.** The single enemy                                          |
| `Lives`             | **New.** Starts at `StartingLives` = 3                             |
| `QixStepInterval`   | **New.** 125 ms                                                    |
| `LifeLostPause`     | **New.** 1 s                                                       |
| `Phase`             | **Changed.** `Playing`, `LifeLost`, `GameOver`, or `Complete`      |
| `Advance(TimeSpan)` | **New.** Steps the Qix and counts down the pause (R10); returns `true` if something visible changed |
| `Move(Direction)`   | **Changed.** Only in `Playing`. Records `LineStart` when leaving the border; a drawing move onto the Qix's cell is a contact; closing passes the Qix's position to `CloseTrail` |
| `ToggleDraw()`      | **Changed.** Only in `Playing`                                     |
| `Restart()`         | **Changed.** Also resets lives to 3 and the Qix to the center with a new random direction; allowed from `Complete` and `GameOver` |

### Phase transitions

```text
Playing  --(contact, lives left after decrement)--> LifeLost
Playing  --(contact, no lives left)---------------> GameOver
LifeLost --(LifeLostPause elapsed)----------------> Playing   (trail cleared, marker at LineStart,
                                                               mode OnBorder, draw off)
Playing  --(CloseTrail makes ClaimedPercent >= 75)-> Complete
Complete / GameOver --(R pressed)-----------------> Playing   (via Restart)
any      --(Esc / Ctrl+C)-------------------------> exit      (terminal layer)
```

### What happens in each phase

| Phase      | Arrows / Space | Qix moves | Pause counts down | R restarts |
|------------|----------------|-----------|-------------------|------------|
| `Playing`  | Yes            | Yes       | —                 | No         |
| `LifeLost` | Ignored        | No        | Yes               | No         |
| `GameOver` | Ignored        | No        | —                 | Yes        |
| `Complete` | Ignored        | No        | —                 | Yes        |
