using Qix.Core;

namespace Qix.Tests;

public class QixMovementTests
{
    // 10×6: interior x 1..8, y 1..4.
    private static Playfield NewPlayfield() => new(10, 6);

    /// <summary>Draws the trail and closes it, keeping the Empty region that contains <paramref name="keep"/>.</summary>
    private static void Close(Playfield playfield, Point keep, params (int X, int Y)[] trail)
    {
        foreach (var (x, y) in trail)
        {
            playfield.SetTrail(new Point(x, y));
        }

        playfield.CloseTrail(keep);
    }

    [Fact]
    public void In_open_space_it_moves_one_diagonal_cell()
    {
        var qix = new Core.Qix(new Point(4, 2), new Velocity(1, 1));

        var outcome = qix.Step(NewPlayfield());

        Assert.Equal(QixStepOutcome.Moved, outcome);
        Assert.Equal(new Point(5, 3), qix.Position);
        Assert.Equal(new Velocity(1, 1), qix.Velocity);
    }

    [Fact]
    public void It_bounces_off_a_vertical_wall()
    {
        var qix = new Core.Qix(new Point(8, 2), new Velocity(1, 1));

        qix.Step(NewPlayfield());

        Assert.Equal(new Velocity(-1, 1), qix.Velocity);
        Assert.Equal(new Point(7, 3), qix.Position);
    }

    [Fact]
    public void It_bounces_off_a_horizontal_wall()
    {
        var qix = new Core.Qix(new Point(4, 4), new Velocity(1, 1));

        qix.Step(NewPlayfield());

        Assert.Equal(new Velocity(1, -1), qix.Velocity);
        Assert.Equal(new Point(5, 3), qix.Position);
    }

    [Fact]
    public void It_bounces_back_out_of_a_corner()
    {
        var qix = new Core.Qix(new Point(8, 4), new Velocity(1, 1));

        qix.Step(NewPlayfield());

        Assert.Equal(new Velocity(-1, -1), qix.Velocity);
        Assert.Equal(new Point(7, 3), qix.Position);
    }

    [Fact]
    public void Hitting_the_tip_of_a_corner_flips_both_components()
    {
        // A one-cell corner cut makes (1, 1) border; its neighbours (1, 2) and (2, 1) stay open.
        var playfield = NewPlayfield();
        Close(playfield, new Point(5, 3), (1, 1));
        var qix = new Core.Qix(new Point(2, 2), new Velocity(-1, -1));

        qix.Step(playfield);

        Assert.Equal(new Velocity(1, 1), qix.Velocity);
        Assert.Equal(new Point(3, 3), qix.Position);
    }

    [Fact]
    public void It_does_not_cut_between_two_walls_that_meet_diagonally()
    {
        // Line A: top frame down to (4, 2), then left to the frame. Line B: bottom frame up to
        // (5, 3), then right to the frame. The walls (4, 2) and (5, 3) meet diagonally and split
        // the open area into two halves; the top-right one, holding (5, 2), is kept open.
        // A Qix at (4, 3) can only get there by slipping between the two walls, so it must not.
        var playfield = NewPlayfield();
        Close(playfield, new Point(5, 2), (4, 1), (4, 2), (3, 2), (2, 2), (1, 2));
        Close(playfield, new Point(5, 2), (5, 4), (5, 3), (6, 3), (7, 3), (8, 3));
        Assert.Equal(Cell.Empty, playfield[new Point(5, 2)]);
        var qix = new Core.Qix(new Point(4, 3), new Velocity(1, -1));

        var outcome = qix.Step(playfield);

        Assert.Equal(QixStepOutcome.Stayed, outcome);
        Assert.Equal(new Point(4, 3), qix.Position);
    }

    [Fact]
    public void In_a_one_cell_wide_corridor_it_runs_straight_and_reverses_at_the_end()
    {
        // 10×3: the interior is the single row y = 1, x 1..8.
        var playfield = new Playfield(10, 3);
        var qix = new Core.Qix(new Point(4, 1), new Velocity(1, 1));
        var xs = new List<int>();

        for (var i = 0; i < 7; i++)
        {
            Assert.Equal(QixStepOutcome.Moved, qix.Step(playfield));
            Assert.Equal(1, qix.Position.Y);
            xs.Add(qix.Position.X);
        }

        Assert.Equal(new[] { 5, 6, 7, 8, 7, 6, 5 }, xs);
    }

    [Fact]
    public void In_a_single_cell_region_it_stays_put()
    {
        var qix = new Core.Qix(new Point(1, 1), new Velocity(1, 1));

        var outcome = qix.Step(new Playfield(3, 3));

        Assert.Equal(QixStepOutcome.Stayed, outcome);
        Assert.Equal(new Point(1, 1), qix.Position);
    }

    [Fact]
    public void It_never_leaves_open_area()
    {
        // 20×12 with the strip left of x = 5 claimed; the Qix starts at the center (10, 6).
        var playfield = new Playfield(20, 12);
        Close(playfield, new Point(10, 6), (5, 1), (5, 2), (5, 3), (5, 4), (5, 5), (5, 6), (5, 7), (5, 8), (5, 9), (5, 10));
        var random = new Random(1);
        var qix = new Core.Qix(new Point(10, 6), Velocity.Diagonals[random.Next(4)]);

        for (var i = 0; i < 2000; i++)
        {
            qix.Step(playfield);
            Assert.True(playfield[qix.Position] == Cell.Empty, $"Step {i}: Qix on {playfield[qix.Position]} at {qix.Position}");
        }
    }
}
