namespace Qix.Core;

/// <summary>A playfield position: X is the column (0 = left), Y is the row (0 = top).</summary>
public readonly record struct Point(int X, int Y)
{
    public static Point operator +(Point p, Direction d)
    {
        var (dx, dy) = d.Offset();
        return new Point(p.X + dx, p.Y + dy);
    }
}

public enum Direction
{
    Up,
    Down,
    Left,
    Right,
}

public static class DirectionExtensions
{
    public static (int Dx, int Dy) Offset(this Direction d) => d switch
    {
        Direction.Up => (0, -1),
        Direction.Down => (0, 1),
        Direction.Left => (-1, 0),
        Direction.Right => (1, 0),
        _ => throw new ArgumentOutOfRangeException(nameof(d)),
    };
}

/// <summary>Diagonal travel direction of the Qix; each component is −1 or +1 while moving diagonally.</summary>
public readonly record struct Velocity(int Dx, int Dy)
{
    /// <summary>The four diagonals, for picking a random start direction.</summary>
    public static Velocity[] Diagonals { get; } = [new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)];
}
