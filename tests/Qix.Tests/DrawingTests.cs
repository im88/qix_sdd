using Qix.Core;

namespace Qix.Tests;

public class DrawingTests
{
    // 10×6 field, marker starts at (5, 5) on the bottom frame.
    private static Game NewGame() => new(new Playfield(10, 6));

    [Fact]
    public void ToggleDraw_flips_draw_on_and_off()
    {
        var game = NewGame();

        game.ToggleDraw();
        Assert.True(game.DrawOn);

        game.ToggleDraw();
        Assert.False(game.DrawOn);
    }

    [Fact]
    public void Moving_into_empty_area_with_draw_on_starts_a_trail()
    {
        var game = NewGame();
        game.ToggleDraw();

        game.Move(Direction.Up);

        Assert.Equal(new Point(5, 4), game.Marker.Position);
        Assert.Equal(Cell.Trail, game.Playfield[new Point(5, 4)]);
        Assert.Equal(MarkerMode.Drawing, game.Marker.Mode);
        Assert.Equal(new[] { new Point(5, 4) }, game.Marker.Trail);
    }

    [Fact]
    public void Switching_draw_off_mid_line_stops_the_marker_until_switched_back_on()
    {
        var game = NewGame();
        game.ToggleDraw();
        game.Move(Direction.Up);

        game.ToggleDraw();
        game.Move(Direction.Up);
        Assert.Equal(new Point(5, 4), game.Marker.Position);

        game.ToggleDraw();
        game.Move(Direction.Up);
        Assert.Equal(new Point(5, 3), game.Marker.Position);
    }

    [Fact]
    public void Moving_onto_the_trail_is_blocked()
    {
        var game = NewGame();
        game.ToggleDraw();
        game.Move(Direction.Up);    // (5, 4)
        game.Move(Direction.Up);    // (5, 3)
        game.Move(Direction.Left);  // (4, 3)
        game.Move(Direction.Down);  // (4, 4)

        game.Move(Direction.Right); // (5, 4) is trail

        Assert.Equal(new Point(4, 4), game.Marker.Position);
        Assert.Equal(MarkerMode.Drawing, game.Marker.Mode);
    }

    [Fact]
    public void With_draw_on_the_marker_still_moves_along_the_border()
    {
        var game = NewGame();
        game.ToggleDraw();

        game.Move(Direction.Left);

        Assert.Equal(new Point(4, 5), game.Marker.Position);
        Assert.Equal(MarkerMode.OnBorder, game.Marker.Mode);
        Assert.Empty(game.Marker.Trail);
    }

    [Fact]
    public void Reaching_the_border_closes_the_line_and_switches_draw_off()
    {
        var game = NewGame();
        game.ToggleDraw();

        for (var i = 0; i < 5; i++)
        {
            game.Move(Direction.Up);
        }

        Assert.Equal(new Point(5, 0), game.Marker.Position);
        Assert.Equal(MarkerMode.OnBorder, game.Marker.Mode);
        Assert.Empty(game.Marker.Trail);
        Assert.False(game.DrawOn);
        Assert.DoesNotContain(Cell.Trail, PlayfieldAssert.AllCells(game.Playfield));
    }
}
