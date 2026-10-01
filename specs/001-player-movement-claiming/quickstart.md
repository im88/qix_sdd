# Quickstart & Validation: Player Movement and Territory Claiming

Controls and the screen layout are defined in [contracts/cli-and-controls.md](contracts/cli-and-controls.md).
Game rules are in [data-model.md](data-model.md).

## Prerequisites

- Windows with the .NET 10 SDK (`dotnet --list-sdks` shows a `10.0.x` entry)
- Windows Terminal or Git Bash, window at least 80×25

## Build, test, run

```text
dotnet build
dotnet test
dotnet run --project src/Qix
```

**Expected**: The build succeeds, all tests pass, and the game opens showing the empty playfield,
`Claimed: 0% / 75%`, and the controls hint.

## Validation scenarios (manual playtest)

| #   | Scenario | Steps | Expected |
|-----|----------|-------|----------|
| V1  | **Keys work in both terminals (R2, do this first)** | In Git Bash and in Windows Terminal: press → a few times, then hold it; press Space, then ↑ | Each press moves one cell, holding repeats, release stops. After Space the status shows `Draw: ON` and ↑ leaves the frame drawing a yellow line. |
| V2  | Border movement (US1) | Move around the full frame using all four arrows | The marker follows the frame and turns corners; ↑/↓ on a horizontal edge without Space does nothing |
| V3  | Straight cut (US2) | From the bottom edge, press Space, then hold ↑ until you reach the top frame | The smaller side fills with `░`, the percentage updates immediately, the status shows `Draw: OFF`, and the marker can walk along the new line |
| V4  | L-shaped and zig-zag lines (US2) | Draw lines with 1–3 turns that end on the frame or on a previous line | The smaller enclosed region fills each time (SC-002: aim for 20 varied shapes) |
| V5  | Blocks while drawing (US2) | While drawing, steer back onto your own line or into claimed area | The marker doesn't move |
| V6  | Draw off mid-line (US2) | Draw away from the frame, press Space (`Draw: OFF`), press arrows | The marker stays put; pressing Space again resumes drawing |
| V7  | Pocket | Draw a line that runs right next to itself so that it encloses a small gap, then close it | The gap is claimed as well as the smaller main side |
| V8  | Completion (US3) | Keep claiming until ≥ 75% | The completion box appears; arrows do nothing; R gives a fresh 0% playfield |
| V9  | Quit and restore (FR-016) | Press Esc during play; relaunch and press Ctrl+C | Back at the prompt both times: cursor visible, normal colors, scrollback intact |
| V10 | Small window (FR-017) | Shrink the window below 80×25, launch | The resize message appears; enlarging the window starts the game |
| V11 | Redirected input | `echo x \| dotnet run --project src/Qix` | Error on stderr, exit code 1 |

**If V1 fails** in either terminal, stop: the input approach in research R2 doesn't work there,
and it needs to be revisited before implementation continues.
