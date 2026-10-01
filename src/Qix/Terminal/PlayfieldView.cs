using Qix.Core;

namespace Qix.Terminal;

/// <summary>Draws the game onto the renderer (contract "Screen layout").</summary>
internal static class PlayfieldView
{
    public const int MinWidth = 80;
    public const int MinHeight = 25;

    // Playfield (0,0) is drawn at screen column 1, row 1 (research R7).
    private const int OriginCol = 1;
    private const int OriginRow = 1;
    private const int StatusRow = 0;
    private const int DrawStatusCol = 22;
    private const int HintRow = 23;

    public static void Draw(Renderer r, Game game)
    {
        var playfield = game.Playfield;
        for (var y = 0; y < playfield.Height; y++)
        {
            for (var x = 0; x < playfield.Width; x++)
            {
                var (glyph, color) = playfield[new Point(x, y)] switch
                {
                    Cell.Claimed => ('░', ConsoleColor.DarkCyan),
                    Cell.Border => ('█', ConsoleColor.White),
                    Cell.Trail => ('█', ConsoleColor.Yellow),
                    _ => (' ', (ConsoleColor?)null),
                };
                r.Put(OriginCol + x, OriginRow + y, glyph, color);
            }
        }

        var marker = game.Marker.Position;
        r.Put(OriginCol + marker.X, OriginRow + marker.Y, '◆', ConsoleColor.Red);

        r.Text(OriginCol, StatusRow, $"Claimed: {playfield.ClaimedPercent}% / {Game.TargetPercent}%");
        r.Text(DrawStatusCol, StatusRow, game.DrawOn ? "Draw: ON" : "Draw: OFF", game.DrawOn ? ConsoleColor.Yellow : null);
        r.Text(OriginCol, HintRow, "←↑→↓ move   Space draw on/off   Esc quit");

        if (game.Phase == GamePhase.Complete)
        {
            DrawCompletionBox(r, playfield);
        }
    }

    public static void DrawTooSmall(Renderer r, int width, int height)
    {
        r.Text(0, 0, $"Please resize the window to at least {MinWidth}×{MinHeight} (current: {width}×{height}).");
        r.Text(0, 1, "Esc to quit.");
    }

    /// <summary>A box centered over the playfield; the playfield stays visible around it.</summary>
    private static void DrawCompletionBox(Renderer r, Playfield playfield)
    {
        var message = $"Playfield complete! {playfield.ClaimedPercent}% claimed   R = restart   Esc = quit";
        var width = message.Length + 4;
        var left = OriginCol + (playfield.Width - width) / 2;
        var top = OriginRow + playfield.Height / 2 - 1;

        r.Text(left, top, "┌" + new string('─', width - 2) + "┐");
        r.Text(left, top + 1, "│ " + message + " │");
        r.Text(left, top + 2, "└" + new string('─', width - 2) + "┘");
    }
}
