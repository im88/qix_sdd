# Contract: Command Line, Controls, and Screen

The game is a console application. These are its only external interfaces.

## Launch

```text
dotnet run --project src/Qix
```

or the built executable `Qix.exe`. **No arguments** are accepted in this feature; any arguments
are ignored.

| Exit code | Meaning                                                            |
|-----------|--------------------------------------------------------------------|
| 0         | The player quit (Esc, Ctrl+C, or Esc on the resize/completion screen) |
| 1         | Not running in an interactive console (input/output redirected); a message is written to stderr |

## Controls

| Key        | Screen      | Effect                                                     |
|------------|-------------|------------------------------------------------------------|
| ← ↑ → ↓    | Playing     | Move the marker while held; the most recently pressed arrow wins |
| Space      | Playing     | Hold to draw into the unclaimed area                       |
| Esc        | Any         | Quit and restore the terminal                              |
| Ctrl+C     | Any         | Same as Esc                                                |
| R          | Complete    | Start a fresh playfield                                    |

## Screen layout (80×25 minimum)

```text
row 0     Claimed: 23% / 75%                       (status line)
rows 1-22 ██████████ ... 78×22 playfield, frame included, starting at column 1
row 23    ←↑→↓ move   Space (hold) draw   Esc quit   (controls hint)
row 24    (unused)
```

| Element    | Glyph | Color        |
|------------|-------|--------------|
| Unclaimed  | space | default      |
| Claimed    | `░`   | dark cyan    |
| Border     | `█`   | white        |
| Trail      | `█`   | yellow       |
| Marker     | `◆`   | bright red   |

**Completion screen**: The final playfield stays visible, and a centered box reads
`Playfield complete! NN% claimed   R = restart   Esc = quit`.

**Too-small screen**: The game shows `Please resize the window to at least 80×25 (current: W×H).
Esc to quit.` It starts as soon as the size is sufficient.

## Terminal guarantees

On every exit path (Esc, Ctrl+C, completion screen, unhandled exception), the game restores the
main screen buffer, shows the cursor again, and resets colors.
