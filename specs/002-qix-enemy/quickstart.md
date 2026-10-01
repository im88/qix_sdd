# Quickstart & Validation: Qix Enemy and Lives

Controls and screen changes are in [contracts/cli-and-controls.md](contracts/cli-and-controls.md).
Rules are in [data-model.md](data-model.md). Prerequisites and build commands are the same as in
[001's quickstart](../001-player-movement-claiming/quickstart.md).

## Build, test, run

```text
dotnet build
dotnet test
dotnet run --project src/Qix
```

**Expected**: The build succeeds, all tests pass (including the updated 001 tests), and the game
opens with a magenta `●` near the center, already moving, and `Lives: 3` in the status line.

## Validation scenarios (manual playtest)

| #   | Scenario | Steps | Expected |
|-----|----------|-------|----------|
| Q1  | Idle Qix (US1) | Launch and press nothing for 30 s | The Qix moves steadily (about 8 cells/s), bounces off the frame, never touches or enters it. No flicker. |
| Q2  | Bounce off claimed area (US1) | Claim a region, then watch the Qix near it | It bounces off the new edge and never enters claimed area (SC-001) |
| Q3  | Border is safe (US2) | With drawing off, walk the marker along the frame while the Qix passes close by | Nothing happens |
| Q4  | Line hit (US2) | Press Space, draw a line into the Qix's path, and stop | Lives drop to 2, the line turns red with `Hit!` for about 1 s, then the line disappears and the marker is back where the line started with `Draw: OFF` |
| Q5  | Running into the Qix (US2) | While drawing, steer the marker straight onto the Qix | Same as Q4 |
| Q6  | Draw off is not safe (US2) | Draw away from the frame, press Space to stop, wait for the Qix | A hit still costs a life |
| Q7  | Game over (US2) | Lose all three lives | The game-over box appears; arrows and Space do nothing; R gives 0%, 3 lives, Qix at center |
| Q8  | Claim the Qix-free side (US3) | Wait until the Qix is in a small part of the field, then cut that part off | The *large* side is claimed, the Qix keeps bouncing in the small side, and the percentage jumps (completion if ≥ 75%) |
| Q9  | Pockets with the Qix (US3) | Close a line that encloses a pocket against itself | The pocket is claimed along with the Qix-free side |
| Q10 | Close just ahead of the Qix | Close a line as the Qix approaches it | If the close lands first, the fill happens and no life is lost |
| Q11 | Paused while too small | Shrink the window below 80×25 mid-game, wait, enlarge it | The resize message appears; afterwards the Qix continues from where it was, without jumping |
| Q12 | Restart after completion | Reach 75%, press R | 0%, 3 lives, Qix at center moving in a (possibly different) diagonal |

Re-run 001's V2–V9 briefly to confirm movement, drawing, completion, and quitting still work.
