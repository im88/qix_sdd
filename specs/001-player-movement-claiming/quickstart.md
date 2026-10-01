# Quickstart & Validation: Player Movement and Territory Claiming

Controls and the screen layout are defined in [contracts/cli-and-controls.md](contracts/cli-and-controls.md).
Game rules are in [data-model.md](data-model.md).

## Prerequisites

- Windows with the .NET 10 SDK (`dotnet --list-sdks` shows a `10.0.x` entry)
- Windows Terminal, window at least 80×25

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
| V1  | **Held keys work (R2 risk check, do this first)** | Hold → for 2 s, release. Then hold Space, and while holding it, hold ↑ | The marker moves steadily while → is held and stops immediately on release. With Space + ↑ it leaves the frame drawing a yellow line. |
| V2  | Border movement (US1) | Move around the full frame using all four arrows | The marker follows the frame and turns corners; ↑/↓ on a horizontal edge without Space does nothing |
| V3  | Straight cut (US2) | From the bottom edge, hold Space + ↑ until you reach the top frame | The smaller side fills with `░`, the percentage updates immediately, and the marker can walk along the new line |
| V4  | L-shaped and zig-zag lines (US2) | Draw lines with 1–3 turns that end on the frame or on a previous line | The smaller enclosed region fills each time (SC-002: aim for 20 varied shapes) |
| V5  | Blocks while drawing (US2) | While drawing, steer back onto your own line or into claimed area | The marker doesn't move |
| V6  | Release Space mid-line (US2) | Draw away from the frame, release Space, press arrows | The marker stays put; holding Space again resumes drawing |
| V7  | Pocket | Draw a line that runs right next to itself so that it encloses a small gap, then close it | The gap is claimed as well as the smaller main side |
| V8  | Completion (US3) | Keep claiming until ≥ 75% | The completion box appears; arrows do nothing; R gives a fresh 0% playfield |
| V9  | Quit and restore (FR-016) | Press Esc during play; relaunch and press Ctrl+C | Back at the prompt both times: cursor visible, normal colors, scrollback intact |
| V10 | Small window (FR-017) | Shrink the window below 80×25, launch | The resize message appears; enlarging the window starts the game |
| V11 | Redirected input | `echo x \| dotnet run --project src/Qix` | Error on stderr, exit code 1 |

**If V1 fails** (the marker keeps moving after the key is released, or Space + arrow doesn't
draw), stop. The input approach in research R2 doesn't work in this terminal, and the controls
need to be revisited (fallback: Space toggles draw mode) through `/speckit-clarify` before
implementation continues.
