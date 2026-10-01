using Qix.Core;

namespace Qix.Tests;

public class QixTimingTests
{
    private static readonly TimeSpan OneMs = TimeSpan.FromMilliseconds(1);

    private static Game NewGame() => new(new Playfield(20, 12), new Random(1));

    [Fact]
    public void The_qix_starts_at_the_center_moving_diagonally()
    {
        var game = NewGame();

        Assert.Equal(new Point(10, 6), game.Qix.Position);
        Assert.Contains(game.Qix.Velocity, Velocity.Diagonals);
    }

    [Fact]
    public void Step_interval_is_125_ms()
    {
        Assert.Equal(TimeSpan.FromMilliseconds(125), Game.QixStepInterval);
    }

    [Fact]
    public void The_qix_moves_one_cell_per_full_interval()
    {
        var game = NewGame();
        var start = game.Qix.Position;

        Assert.False(game.Advance(Game.QixStepInterval - OneMs));
        Assert.Equal(start, game.Qix.Position);

        Assert.True(game.Advance(OneMs));
        Assert.Equal(1, Distance(start, game.Qix.Position));
    }

    [Fact]
    public void Leftover_time_carries_over_to_the_next_call()
    {
        var game = NewGame();
        var start = game.Qix.Position;
        var half = Game.QixStepInterval / 2;

        var moves = new[] { game.Advance(half), game.Advance(half), game.Advance(half) };

        Assert.Equal(new[] { false, true, false }, moves);
        Assert.Equal(1, Distance(start, game.Qix.Position));
    }

    [Fact]
    public void A_long_stall_causes_at_most_two_steps()
    {
        var game = NewGame();
        var start = game.Qix.Position;

        game.Advance(TimeSpan.FromSeconds(10));

        Assert.Equal(2, Distance(start, game.Qix.Position));
    }

    [Fact]
    public void Restart_puts_the_qix_back_at_the_center()
    {
        var game = NewGame();
        game.Advance(Game.QixStepInterval);
        Assert.NotEqual(new Point(10, 6), game.Qix.Position);

        game.Restart();

        Assert.Equal(new Point(10, 6), game.Qix.Position);
    }

    /// <summary>Chebyshev distance: the number of diagonal steps between two cells in open space.</summary>
    private static int Distance(Point a, Point b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
}
