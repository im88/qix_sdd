using Qix.Core;

namespace Qix.Tests;

public class CollisionTests
{
    // 10×6 field, interior x 1..8, y 1..4; the marker starts at (5, 5) on the bottom frame.
    private static Game NewGame(Point qix, Velocity velocity)
    {
        var game = new Game(new Playfield(10, 6), new Random(1));
        game.Qix.Position = qix;
        game.Qix.Velocity = velocity;
        return game;
    }

    private static void Step(Game game) => game.Advance(Game.QixStepInterval);

    private static void AssertLifeLost(Game game)
    {
        Assert.Equal(2, game.Lives);
        Assert.Equal(GamePhase.LifeLost, game.Phase);
    }

    [Fact]
    public void Qix_stepping_onto_the_line_costs_a_life()
    {
        var game = NewGame(new Point(4, 2), new Velocity(1, 1));
        game.ToggleDraw();
        game.Move(Direction.Up);    // (5, 4)
        game.Move(Direction.Up);    // (5, 3)

        Step(game);                 // diagonal target (5, 3) is the line

        AssertLifeLost(game);
        Assert.Equal(new Point(4, 2), game.Qix.Position);
    }

    [Fact]
    public void Qix_stepping_onto_the_drawing_marker_costs_a_life()
    {
        var game = NewGame(new Point(4, 3), new Velocity(1, 1));
        game.ToggleDraw();
        game.Move(Direction.Up);    // (5, 4)
        Assert.Equal(new Point(5, 4), game.Marker.Position);

        Step(game);

        AssertLifeLost(game);
    }

    [Fact]
    public void Qix_squeezing_diagonally_past_the_line_costs_a_life()
    {
        // The line runs (5, 4) → (6, 4) → (6, 2) → (4, 2) → (4, 3), around (5, 3), which stays
        // Empty. The Qix at (4, 4) moving up-right has the line on both sides of its diagonal.
        var game = NewGame(new Point(4, 4), new Velocity(1, -1));
        game.ToggleDraw();
        foreach (var d in new[] { Direction.Up, Direction.Right, Direction.Up, Direction.Up, Direction.Left, Direction.Left, Direction.Down })
        {
            game.Move(d);
        }

        Assert.Equal(new Point(4, 3), game.Marker.Position);
        Assert.Equal(Cell.Empty, game.Playfield[new Point(5, 3)]);

        Step(game);

        AssertLifeLost(game);
        Assert.Equal(new Point(4, 4), game.Qix.Position);
    }

    [Fact]
    public void Drawing_marker_moving_onto_the_qix_costs_a_life()
    {
        var game = NewGame(new Point(5, 3), new Velocity(1, 1));
        game.ToggleDraw();
        game.Move(Direction.Up);    // (5, 4)

        game.Move(Direction.Up);    // (5, 3), where the Qix is

        AssertLifeLost(game);
    }

    [Fact]
    public void Marker_on_the_frame_without_a_line_is_safe()
    {
        // The Qix runs along row 4, right next to the marker on the bottom frame.
        var game = NewGame(new Point(2, 4), new Velocity(1, 1));

        for (var i = 0; i < 20; i++)
        {
            Step(game);
        }

        Assert.Equal(3, game.Lives);
        Assert.Equal(GamePhase.Playing, game.Phase);
    }

    [Fact]
    public void Line_stays_vulnerable_with_draw_switched_off()
    {
        var game = NewGame(new Point(4, 3), new Velocity(1, 1));
        game.ToggleDraw();
        game.Move(Direction.Up);    // (5, 4)
        game.ToggleDraw();

        Step(game);

        AssertLifeLost(game);
    }

    [Fact]
    public void Closing_the_line_before_the_qix_steps_wins()
    {
        // The Qix's next step would enter the line at (5, 2), but the player closes it first.
        var game = NewGame(new Point(4, 1), new Velocity(1, 1));
        game.ToggleDraw();
        for (var i = 0; i < 4; i++)
        {
            game.Move(Direction.Up);
        }

        game.Move(Direction.Up);    // onto the top frame: closes the line
        Step(game);

        Assert.True(game.Playfield.ClaimedPercent > 0);
        Assert.Equal(3, game.Lives);
        Assert.Equal(GamePhase.Playing, game.Phase);
    }
}
