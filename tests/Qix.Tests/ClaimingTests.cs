using Qix.Core;
using static Qix.Tests.PlayfieldAssert;

namespace Qix.Tests;

public class ClaimingTests
{
    private static void Close(Playfield playfield, params (int X, int Y)[] trail)
    {
        foreach (var (x, y) in trail)
        {
            playfield.SetTrail(new Point(x, y));
        }

        playfield.CloseTrail();
    }

    [Fact]
    public void Straight_cut_claims_the_smaller_side()
    {
        // 10×6, interior x 1..8, y 1..4. Trail at x = 3: left 2×4 = 8 cells, right 5×4 = 20 cells.
        var playfield = new Playfield(10, 6);

        Close(playfield, (3, 1), (3, 2), (3, 3), (3, 4));

        Rect(playfield, 1, 1, 2, 4, Cell.Claimed);
        Rect(playfield, 4, 1, 8, 4, Cell.Empty);
        Rect(playfield, 3, 1, 3, 4, Cell.Border);
        BorderTouchesEmpty(playfield);
        Assert.Equal(1, EmptyRegionCount(playfield));
    }

    [Fact]
    public void Straight_cut_demotes_frame_cells_that_no_longer_touch_open_area()
    {
        var playfield = new Playfield(10, 6);

        Close(playfield, (3, 1), (3, 2), (3, 3), (3, 4));

        Rect(playfield, 0, 0, 2, 0, Cell.Claimed);
        Rect(playfield, 0, 5, 2, 5, Cell.Claimed);
        Rect(playfield, 0, 0, 0, 5, Cell.Claimed);
        Assert.Equal(Cell.Border, playfield[new Point(3, 0)]);
        Assert.Equal(Cell.Border, playfield[new Point(3, 5)]);
    }

    [Fact]
    public void L_shaped_line_claims_only_the_enclosed_corner()
    {
        // From the top frame at (3, 0) down to (3, 2), then left to the left frame at (0, 2).
        var playfield = new Playfield(10, 6);

        Close(playfield, (3, 1), (3, 2), (2, 2), (1, 2));

        Rect(playfield, 1, 1, 2, 1, Cell.Claimed);
        Rect(playfield, 4, 1, 8, 4, Cell.Empty);
        Rect(playfield, 1, 3, 3, 4, Cell.Empty);
        BorderTouchesEmpty(playfield);
    }

    [Fact]
    public void One_cell_line_cutting_a_corner_closes_correctly()
    {
        // From (1, 0) down into (1, 1), then left onto the frame at (0, 1).
        var playfield = new Playfield(10, 6);

        Close(playfield, (1, 1));

        Assert.Equal(Cell.Border, playfield[new Point(1, 1)]);
        Assert.Equal(Cell.Claimed, playfield[new Point(0, 0)]);
        Assert.DoesNotContain(Cell.Trail, AllCells(playfield));
        Assert.Equal(31, AllCells(playfield).Count(c => c == Cell.Empty));
        BorderTouchesEmpty(playfield);
    }

    [Fact]
    public void Line_ending_on_a_claimed_edge_fills_like_any_other()
    {
        var playfield = new Playfield(10, 6);
        Close(playfield, (3, 1), (3, 2), (3, 3), (3, 4));

        // From the top frame at (6, 0) down to (6, 2), then left onto the old line at (3, 2).
        // Encloses (4, 1) and (5, 1): 2 cells against 14.
        Close(playfield, (6, 1), (6, 2), (5, 2), (4, 2));

        Rect(playfield, 4, 1, 5, 1, Cell.Claimed);
        Rect(playfield, 7, 1, 8, 4, Cell.Empty);
        Rect(playfield, 4, 3, 6, 4, Cell.Empty);
        BorderTouchesEmpty(playfield);
        Assert.Equal(1, EmptyRegionCount(playfield));
    }

    [Fact]
    public void Line_that_encloses_a_pocket_claims_the_pocket_and_the_smaller_side()
    {
        // 12×8, interior 10×6 = 60. From the left frame at (0, 2): right along y = 2 to x = 5,
        // down to (5, 4), left to (3, 4), up to (3, 3), left to (2, 3), down to the bottom frame.
        // Regions: pocket {(4, 3)} = 1, bottom-left x = 1, y = 3..6 = 4, the rest = 41.
        var playfield = new Playfield(12, 8);

        Close(playfield,
            (1, 2), (2, 2), (3, 2), (4, 2), (5, 2),
            (5, 3), (5, 4), (4, 4), (3, 4), (3, 3),
            (2, 3), (2, 4), (2, 5), (2, 6));

        Assert.Equal(Cell.Claimed, playfield[new Point(4, 3)]);
        Rect(playfield, 1, 3, 1, 6, Cell.Claimed);
        Assert.Equal(41, AllCells(playfield).Count(c => c == Cell.Empty));
        Assert.Equal(1, EmptyRegionCount(playfield));
        BorderTouchesEmpty(playfield);
    }

    [Fact]
    public void Equal_regions_keep_the_one_with_the_topmost_leftmost_cell()
    {
        // 11×6, interior x 1..9. Trail at x = 5 splits it into two 4×4 halves.
        // The left half contains (1, 1), the topmost-then-leftmost Empty cell, so it stays.
        var playfield = new Playfield(11, 6);

        Close(playfield, (5, 1), (5, 2), (5, 3), (5, 4));

        Rect(playfield, 1, 1, 4, 4, Cell.Empty);
        Rect(playfield, 6, 1, 9, 4, Cell.Claimed);
    }

    [Fact]
    public void SetTrail_on_a_non_empty_cell_throws()
    {
        var playfield = new Playfield(10, 6);

        Assert.Throws<InvalidOperationException>(() => playfield.SetTrail(new Point(0, 0)));
    }

    [Fact]
    public void After_a_close_the_marker_walks_the_new_edge_but_not_into_claimed_area()
    {
        // 10×6, marker at (5, 5). Straight cut up x = 5: left 4×4 = 16 stays, right 3×4 = 12 is claimed.
        var game = new Game(new Playfield(10, 6));
        game.ToggleDraw();
        for (var i = 0; i < 5; i++)
        {
            game.Move(Direction.Up);
        }

        Assert.Equal(new Point(5, 0), game.Marker.Position);
        Rect(game.Playfield, 6, 1, 8, 4, Cell.Claimed);

        game.Move(Direction.Down);
        Assert.Equal(new Point(5, 1), game.Marker.Position);

        game.Move(Direction.Right);
        Assert.Equal(new Point(5, 1), game.Marker.Position);

        game.Move(Direction.Left);
        Assert.Equal(new Point(5, 1), game.Marker.Position);
    }
}
