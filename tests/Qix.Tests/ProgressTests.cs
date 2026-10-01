using Qix.Core;

namespace Qix.Tests;

public class ProgressTests
{
    // 12×6: interior 10×4 = 40 cells, so each cell is 2.5%.
    private static Playfield NewPlayfield() => new(12, 6);

    /// <summary>Draws the trail and closes it, keeping the Empty region that contains <paramref name="keep"/>.</summary>
    private static void Close(Playfield playfield, Point keep, params (int X, int Y)[] trail)
    {
        foreach (var (x, y) in trail)
        {
            playfield.SetTrail(new Point(x, y));
        }

        playfield.CloseTrail(keep);
    }

    /// <summary>
    /// Two closes worth 26 of 40 cells (65%): a cut at x = 8 claims x = 9..10 (12 cells with the
    /// line), then a cut along y = 2 claims the top row x = 1..7 (14 more). What stays open is
    /// x = 1..7, y = 3..4, and the marker start (6, 5) sits below it.
    /// </summary>
    private static Playfield PlayfieldAt65Percent()
    {
        var playfield = NewPlayfield();
        Close(playfield, new Point(2, 3), (8, 1), (8, 2), (8, 3), (8, 4));
        Close(playfield, new Point(2, 3), (1, 2), (2, 2), (3, 2), (4, 2), (5, 2), (6, 2), (7, 2));
        return playfield;
    }

    [Fact]
    public void New_game_is_at_zero_percent_and_playing()
    {
        var game = NewGame(NewPlayfield());

        Assert.Equal(0, game.Playfield.ClaimedPercent);
        Assert.Equal(GamePhase.Playing, game.Phase);
        Assert.Equal(75, Game.TargetPercent);
    }

    [Fact]
    public void Percentage_is_rounded_down()
    {
        // A one-cell corner cut: 1 of 40 cells = 2.5%.
        var playfield = NewPlayfield();

        Close(playfield, new Point(2, 3), (1, 1));

        Assert.Equal(2, playfield.ClaimedPercent);
    }

    [Fact]
    public void Lines_count_as_claimed()
    {
        // Trail 4 cells + claimed left side 2×4 = 12 of 40 = 30%.
        var playfield = NewPlayfield();

        Close(playfield, new Point(6, 3), (3, 1), (3, 2), (3, 3), (3, 4));

        Assert.Equal(30, playfield.ClaimedPercent);
    }

    [Fact]
    public void Reaching_exactly_75_percent_completes_the_playfield()
    {
        var game = NewGame(PlayfieldAt65Percent());
        Assert.Equal(65, game.Playfield.ClaimedPercent);

        // Cut x = 6 from the start (6, 5) up to the y = 2 line: 2 trail + 2 claimed (x = 7) = 30 of 40.
        game.ToggleDraw();
        game.Move(Direction.Up);
        game.Move(Direction.Up);
        game.Move(Direction.Up);

        Assert.Equal(75, game.Playfield.ClaimedPercent);
        Assert.Equal(GamePhase.Complete, game.Phase);
    }

    [Fact]
    public void Below_75_percent_the_game_keeps_playing()
    {
        var game = NewGame(PlayfieldAt65Percent());

        // Cut x = 7 instead: 2 trail cells, nothing enclosed = 28 of 40 = 70%.
        game.Move(Direction.Right);
        game.ToggleDraw();
        game.Move(Direction.Up);
        game.Move(Direction.Up);
        game.Move(Direction.Up);

        Assert.Equal(70, game.Playfield.ClaimedPercent);
        Assert.Equal(GamePhase.Playing, game.Phase);
    }

    [Fact]
    public void While_complete_moves_and_draw_toggles_are_ignored()
    {
        var game = CompletedGame();
        var position = game.Marker.Position;

        game.ToggleDraw();
        game.Move(Direction.Left);
        game.Move(Direction.Right);

        Assert.False(game.DrawOn);
        Assert.Equal(position, game.Marker.Position);
    }

    [Fact]
    public void Restart_gives_a_fresh_playfield_and_marker()
    {
        var game = CompletedGame();

        game.Restart();

        Assert.Equal(12, game.Playfield.Width);
        Assert.Equal(6, game.Playfield.Height);
        Assert.Equal(0, game.Playfield.ClaimedPercent);
        Assert.Equal(new Point(6, 5), game.Marker.Position);
        Assert.Equal(MarkerMode.OnBorder, game.Marker.Mode);
        Assert.False(game.DrawOn);
        Assert.Equal(GamePhase.Playing, game.Phase);
    }

    [Fact]
    public void Percentage_never_decreases_across_closes()
    {
        var playfield = NewPlayfield();
        var history = new List<int> { playfield.ClaimedPercent };

        Close(playfield, new Point(2, 3), (8, 1), (8, 2), (8, 3), (8, 4));
        history.Add(playfield.ClaimedPercent);
        Close(playfield, new Point(2, 3), (1, 2), (2, 2), (3, 2), (4, 2), (5, 2), (6, 2), (7, 2));
        history.Add(playfield.ClaimedPercent);
        Close(playfield, new Point(2, 3), (6, 3), (6, 4));
        history.Add(playfield.ClaimedPercent);

        Assert.Equal(new[] { 0, 30, 65, 75 }, history);
    }

    [Fact]
    public void Percentage_does_not_change_while_drawing()
    {
        var game = NewGame(NewPlayfield());
        game.ToggleDraw();

        game.Move(Direction.Up);
        game.Move(Direction.Up);

        Assert.Equal(MarkerMode.Drawing, game.Marker.Mode);
        Assert.Equal(0, game.Playfield.ClaimedPercent);
    }

    /// <summary>The Qix is placed at (2, 3), inside the area that stays open (x 1..5, y 3..4).</summary>
    private static Game NewGame(Playfield playfield)
    {
        var game = new Game(playfield);
        game.Qix.Position = new Point(2, 3);
        return game;
    }

    private static Game CompletedGame()
    {
        var game = NewGame(PlayfieldAt65Percent());
        game.ToggleDraw();
        game.Move(Direction.Up);
        game.Move(Direction.Up);
        game.Move(Direction.Up);
        Assert.Equal(GamePhase.Complete, game.Phase);
        return game;
    }
}
