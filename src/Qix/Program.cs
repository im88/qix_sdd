using System.Diagnostics;
using Qix.Core;
using Qix.Terminal;

if (Console.IsInputRedirected || Console.IsOutputRedirected)
{
    Console.Error.WriteLine("Qix must be run in an interactive console.");
    return 1;
}

// Disposing the session restores the terminal on every exit path, exceptions included.
using var session = new TerminalSession();
var renderer = new Renderer();
var game = new Game();
var windowSize = (Width: -1, Height: -1);
var running = true;
var stopwatch = Stopwatch.StartNew();
while (running)
{
    var currentSize = (Width: Console.WindowWidth, Height: Console.WindowHeight);
    var tooSmall = currentSize.Width < PlayfieldView.MinWidth || currentSize.Height < PlayfieldView.MinHeight;
    var dirty = false;
    while (running && Console.KeyAvailable)
    {
        var key = Console.ReadKey(intercept: true);
        dirty = true;
        if (key.Key == ConsoleKey.Escape || (key.Key == ConsoleKey.C && key.Modifiers.HasFlag(ConsoleModifiers.Control)))
        {
            running = false;
        }
        else if (!tooSmall)
        {
            HandleGameKey(game, key.Key);
        }
    }

    // Keys are handled before time passes, so a move that closes the line beats the Qix (R13).
    // While the window is too small the game is paused; restarting the stopwatch keeps the Qix
    // from jumping once the window is big enough again.
    if (running && !tooSmall)
    {
        dirty |= game.Advance(stopwatch.Elapsed);
    }

    stopwatch.Restart();

    if (currentSize != windowSize)
    {
        windowSize = currentSize;
        renderer.Invalidate();
        dirty = true;
    }

    if (running && dirty)
    {
        renderer.Clear();
        if (tooSmall)
        {
            PlayfieldView.DrawTooSmall(renderer, currentSize.Width, currentSize.Height);
        }
        else
        {
            PlayfieldView.Draw(renderer, game);
        }

        renderer.Present();
    }

    Thread.Sleep(10);
}

return 0;

static void HandleGameKey(Game game, ConsoleKey key)
{
    switch (key)
    {
        case ConsoleKey.LeftArrow:
            game.Move(Direction.Left);
            break;
        case ConsoleKey.RightArrow:
            game.Move(Direction.Right);
            break;
        case ConsoleKey.UpArrow:
            game.Move(Direction.Up);
            break;
        case ConsoleKey.DownArrow:
            game.Move(Direction.Down);
            break;
        case ConsoleKey.Spacebar:
            game.ToggleDraw();
            break;
        case ConsoleKey.R when game.Phase is GamePhase.Complete or GamePhase.GameOver:
            game.Restart();
            break;
    }
}
