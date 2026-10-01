# Data Model: Player Movement and Territory Claiming

**Feature**: [spec.md](spec.md) | **Research**: [research.md](research.md)

All types below live in the core game logic (`src/Qix/Core`). They don't depend on the console
or the clock, so tests can drive them step by step.

## Cell (enum)

| Value     | Meaning                                                   | Marker may enter?                |
|-----------|-----------------------------------------------------------|----------------------------------|
| `Empty`   | Unclaimed area                                            | Only while drawing               |
| `Border`  | Frame or edge between claimed and unclaimed area          | Yes (closing a line if drawing)  |
| `Claimed` | Claimed territory, including demoted border               | Never                            |
| `Trail`   | Part of the line currently being drawn                    | Never                            |

## Point (readonly record struct)

`X` (column, 0 = left) and `Y` (row, 0 = top), in playfield coordinates.

## Direction (enum)

`Up`, `Down`, `Left`, `Right`, each with a unit offset (dx, dy).

## Playfield

A fixed grid of cells with the claiming logic.

| Field / member         | Notes                                                                |
|------------------------|----------------------------------------------------------------------|
| `Width`, `Height`      | 78 × 22 including the frame (R7); the constructor accepts any size ≥ 3×3 for tests |
| `this[Point]`          | Read a cell's state                                                  |
| `InteriorCount`        | `(Width − 2) × (Height − 2)`                                         |
| `ClaimedPercent`       | `floor(100 × nonEmptyInterior / InteriorCount)` (R6)                 |
| `SetTrail(Point)`      | Mark an `Empty` cell as `Trail`                                      |
| `CloseTrail()`         | Run R5: trail → border, claim every region except the largest, demote border cells |

**Initial state**: The outer ring is `Border`; everything else is `Empty`.

**Invariants**

- The outer ring is never `Empty` or `Trail`.
- After `CloseTrail()`, no `Trail` cells remain, and every `Border` cell has at least one `Empty`
  cell among its 8 neighbours (or there are no `Empty` cells left at all).
- Outside of drawing, there is at most one connected `Empty` region.
- `ClaimedPercent` never decreases.

## Marker

| Field      | Type           | Notes                                                       |
|------------|----------------|-------------------------------------------------------------|
| `Position` | `Point`        | Starts at the bottom frame, centered: (39, 21)              |
| `Mode`     | `MarkerMode`   | `OnBorder` or `Drawing`                                     |
| `Trail`    | `List<Point>`  | Cells drawn since leaving the border; empty when `OnBorder` |

### Marker mode transitions (one `Move(direction)` call, target = Position + Direction)

`Draw on` is the toggle state (`Game.DrawOn`), flipped by Space.

| Mode       | Draw on   | Target cell | Result                                                     |
|------------|-----------|-------------|------------------------------------------------------------|
| `OnBorder` | any       | `Border`    | Move                                                       |
| `OnBorder` | no        | `Empty`     | Blocked                                                    |
| `OnBorder` | yes       | `Empty`     | Move, target becomes `Trail`, mode → `Drawing`             |
| `OnBorder` | any       | `Claimed` / outside | Blocked                                            |
| `Drawing`  | no        | any         | Blocked (marker stays put, FR-008)                         |
| `Drawing`  | yes       | `Empty`     | Move, target becomes `Trail`                               |
| `Drawing`  | yes       | `Border`    | Move, `CloseTrail()`, mode → `OnBorder`, draw → off        |
| `Drawing`  | yes       | `Trail` / `Claimed` | Blocked                                            |

The marker never moves without a `Move` call (one per arrow key press).

## Game

Ties together the playfield, the marker, and the session phase.

| Member                    | Notes                                                        |
|---------------------------|--------------------------------------------------------------|
| `Playfield`, `Marker`     | Current state                                                |
| `Phase`                   | `Playing` or `Complete`                                      |
| `TargetPercent`           | 75                                                           |
| `DrawOn`                  | Draw toggle; starts off                                      |
| `ToggleDraw()`            | Flips `DrawOn` (Space); ignored while `Complete`             |
| `Move(Direction)`         | Applies one move (table above); ignored while `Complete`     |
| `Restart()`               | Creates a fresh playfield and marker; draw off; phase → `Playing` |

### Phase transitions

```text
Playing --(CloseTrail makes ClaimedPercent >= 75)--> Complete
Complete --(R pressed)--> Playing   (via Restart: fresh playfield, 0%)
any      --(Esc / Ctrl+C)--> exit   (handled by the terminal layer)
```
