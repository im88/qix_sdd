using Qix.Core;

namespace Qix.Tests;

public class MovementTests
{
    private static Game NewGame() => new(new Playfield(10, 6));

    [Fact]
    public void Marker_starts_on_bottom_frame_centered_and_not_drawing()
    {
        var game = NewGame();

        Assert.Equal(new Point(5, 5), game.Marker.Position);
        Assert.Equal(MarkerMode.OnBorder, game.Marker.Mode);
        Assert.False(game.DrawOn);
    }

    [Fact]
    public void Default_playfield_start_position_is_39_21()
    {
        Assert.Equal(new Point(39, 21), new Game().Marker.Position);
    }

    [Theory]
    [InlineData(Direction.Left, 4)]
    [InlineData(Direction.Right, 6)]
    public void Moves_along_horizontal_edge(Direction direction, int expectedX)
    {
        var game = NewGame();

        game.Move(direction);

        Assert.Equal(new Point(expectedX, 5), game.Marker.Position);
    }

    [Fact]
    public void Up_into_empty_area_without_draw_is_blocked()
    {
        var game = NewGame();

        game.Move(Direction.Up);

        Assert.Equal(new Point(5, 5), game.Marker.Position);
    }

    [Fact]
    public void Down_off_the_playfield_is_blocked()
    {
        var game = NewGame();

        game.Move(Direction.Down);

        Assert.Equal(new Point(5, 5), game.Marker.Position);
    }

    [Fact]
    public void Turns_the_corner()
    {
        var game = NewGame();
        MoveTimes(game, Direction.Left, 5);
        Assert.Equal(new Point(0, 5), game.Marker.Position);

        game.Move(Direction.Up);

        Assert.Equal(new Point(0, 4), game.Marker.Position);
    }

    [Fact]
    public void Left_at_the_corner_is_blocked()
    {
        var game = NewGame();
        MoveTimes(game, Direction.Left, 5);

        game.Move(Direction.Left);

        Assert.Equal(new Point(0, 5), game.Marker.Position);
    }

    [Fact]
    public void Each_move_moves_at_most_one_cell()
    {
        var game = NewGame();

        game.Move(Direction.Right);
        game.Move(Direction.Right);

        Assert.Equal(new Point(7, 5), game.Marker.Position);
    }

    [Fact]
    public void Full_lap_of_the_frame_returns_to_start_and_stays_on_border()
    {
        var game = NewGame();
        var visited = new List<Point>();

        void Walk(Direction direction, int times)
        {
            for (var i = 0; i < times; i++)
            {
                game.Move(direction);
                visited.Add(game.Marker.Position);
            }
        }

        Walk(Direction.Left, 5);
        Walk(Direction.Up, 5);
        Walk(Direction.Right, 9);
        Walk(Direction.Down, 5);
        Walk(Direction.Left, 4);

        Assert.Equal(new Point(5, 5), game.Marker.Position);
        Assert.Equal(28, visited.Distinct().Count());
        Assert.All(visited, p => Assert.Equal(Cell.Border, game.Playfield[p]));
    }

    private static void MoveTimes(Game game, Direction direction, int times)
    {
        for (var i = 0; i < times; i++)
        {
            game.Move(direction);
        }
    }
}
