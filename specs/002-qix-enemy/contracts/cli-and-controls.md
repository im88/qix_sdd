# Contract: Command Line, Controls, and Screen (002 changes)

This amends [001's contract](../../001-player-movement-claiming/contracts/cli-and-controls.md).
Launch, exit codes, and terminal guarantees are unchanged.

## Controls

| Key        | Screen               | Effect                                              |
|------------|----------------------|-----------------------------------------------------|
| ← ↑ → ↓    | Playing              | Unchanged; ignored during the life-lost pause       |
| Space      | Playing              | Unchanged; ignored during the life-lost pause       |
| Esc        | Any                  | Quit and restore the terminal                       |
| Ctrl+C     | Any                  | Same as Esc                                         |
| R          | Complete, Game over  | Start a fresh playfield: 0%, 3 lives, Qix at center |

## Screen layout (80×25 minimum)

```text
row 0     Claimed: 23% / 75%   Draw: OFF     Lives: 3          (status line)
rows 1-22 78×22 playfield, frame included, starting at column 1
row 23    ←↑→↓ move   Space draw on/off   Esc quit             (controls hint)
```

`Lives: N` starts at column 36. During the life-lost pause it reads `Lives: N  Hit!` in red.

| Element                    | Glyph | Color           |
|----------------------------|-------|-----------------|
| Qix                        | `●`   | bright magenta  |
| Trail during life-lost pause | `█` | bright red      |

All other glyphs are unchanged.

**Game-over screen**: The final playfield stays visible, with the line that was hit still shown
in red, and a centered box reads `Game over! NN% claimed   R = restart   Esc = quit`.

**Too-small screen**: While it is shown, the game is paused; the Qix does not move.
