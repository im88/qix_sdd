using Qix.Core;

namespace Qix.Tests;

public class LivesTests
{
    private static readonly TimeSpan OneMs = TimeSpan.FromMilliseconds(1);

    // 10×6 field; the marker starts at (5, 5) on the bottom frame.
    private static Game NewGame() => new(new Playfield(10, 6), new Random(1));

    /// <summary>Draws one cell up from (5, 5) and lets the Qix at (4, 3) run into it.</summary>
    private static void LoseLife(Game game)
    {
        game.Qix.Position = new Point(4, 3);
        game.Qix.Velocity = new Velocity(1, 1);
        game.ToggleDraw();
        game.Move(Direction.Up);
        game.Advance(Game.QixStepInterval);
    }

    /// <summary>Advance caps each call at 250 ms, so longer spans are passed in small pieces.</summary>
    private static void AdvanceBy(Game game, TimeSpan total)
    {
        var chunk = TimeSpan.FromMilliseconds(100);
        while (total > TimeSpan.Zero)
        {
            var step = total < chunk ? total : chunk;
            game.Advance(step);
            total -= step;
        }
    }

    [Fact]
    public void A_new_game_has_three_lives()
    {
        var game = NewGame();

        Assert.Equal(3, Game.StartingLives);
        Assert.Equal(Game.StartingLives, game.Lives);
        Assert.Equal(GamePhase.Playing, game.Phase);
    }

    [Fact]
    public void The_pause_lasts_one_second()
    {
        Assert.Equal(TimeSpan.FromSeconds(1), Game.LifeLostPause);
    }

    [Fact]
    public void During_the_pause_the_line_stays_and_nothing_moves()
    {
        var game = NewGame();
        LoseLife(game);
        Assert.Equal(GamePhase.LifeLost, game.Phase);
        var marker = game.Marker.Position;
        var qix = game.Qix.Position;

        game.Move(Direction.Left);
        game.ToggleDraw();
        game.Advance(Game.QixStepInterval);

        Assert.Equal(Cell.Trail, game.Playfield[new Point(5, 4)]);
        Assert.Equal(marker, game.Marker.Position);
        Assert.True(game.DrawOn);
        Assert.Equal(qix, game.Qix.Position);
    }

    [Fact]
    public void After_the_pause_the_line_is_gone_and_the_marker_is_back_at_the_line_start()
    {
        var game = NewGame();
        LoseLife(game);

        AdvanceBy(game, Game.LifeLostPause - OneMs);
        Assert.Equal(GamePhase.LifeLost, game.Phase);

        game.Advance(OneMs);

        Assert.Equal(GamePhase.Playing, game.Phase);
        Assert.DoesNotContain(Cell.Trail, PlayfieldAssert.AllCells(game.Playfield));
        Assert.Empty(game.Marker.Trail);
        Assert.Equal(new Point(5, 5), game.Marker.Position);
        Assert.Equal(MarkerMode.OnBorder, game.Marker.Mode);
        Assert.False(game.DrawOn);
        Assert.Equal(0, game.Playfield.ClaimedPercent);
    }

    [Fact]
    public void Line_start_is_the_frame_cell_the_marker_left_from()
    {
        var game = NewGame();
        game.Qix.Position = new Point(1, 1);
        game.ToggleDraw();

        game.Move(Direction.Left);  // (4, 5), still on the frame
        game.Move(Direction.Up);    // (4, 4), drawing

        Assert.Equal(new Point(4, 5), game.Marker.LineStart);
    }

    [Fact]
    public void Losing_the_last_life_ends_the_game()
    {
        var game = NewGame();
        LoseLife(game);
        AdvanceBy(game, Game.LifeLostPause);
        LoseLife(game);
        AdvanceBy(game, Game.LifeLostPause);

        LoseLife(game);

        Assert.Equal(0, game.Lives);
        Assert.Equal(GamePhase.GameOver, game.Phase);

        var marker = game.Marker.Position;
        var qix = game.Qix.Position;
        AdvanceBy(game, TimeSpan.FromSeconds(2));
        game.Move(Direction.Up);

        Assert.Equal(GamePhase.GameOver, game.Phase);
        Assert.Equal(marker, game.Marker.Position);
        Assert.Equal(qix, game.Qix.Position);
        Assert.Equal(Cell.Trail, game.Playfield[new Point(5, 4)]);
    }

    [Fact]
    public void Restart_after_game_over_starts_fresh()
    {
        var game = NewGame();
        for (var i = 0; i < 3; i++)
        {
            LoseLife(game);
            AdvanceBy(game, Game.LifeLostPause);
        }

        Assert.Equal(GamePhase.GameOver, game.Phase);

        game.Restart();

        Assert.Equal(3, game.Lives);
        Assert.Equal(0, game.Playfield.ClaimedPercent);
        Assert.Equal(GamePhase.Playing, game.Phase);
        Assert.Equal(new Point(5, 5), game.Marker.Position);
        Assert.Equal(new Point(5, 3), game.Qix.Position);
        Assert.DoesNotContain(Cell.Trail, PlayfieldAssert.AllCells(game.Playfield));
    }
}
