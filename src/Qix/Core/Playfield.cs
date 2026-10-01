namespace Qix.Core;

/// <summary>The fixed grid of cells. The outer ring is the frame.</summary>
public sealed class Playfield
{
    public const int DefaultWidth = 78;
    public const int DefaultHeight = 22;

    private readonly Cell[,] _cells;

    public Playfield(int width = DefaultWidth, int height = DefaultHeight)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 3);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 3);

        Width = width;
        Height = height;
        _cells = new Cell[width, height];
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                var onFrame = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                _cells[x, y] = onFrame ? Cell.Border : Cell.Empty;
            }
        }
    }

    public int Width { get; }

    public int Height { get; }

    public int InteriorCount => (Width - 2) * (Height - 2);

    /// <summary>Callers check <see cref="Contains"/> first.</summary>
    public Cell this[Point p] => _cells[p.X, p.Y];

    public bool Contains(Point p) => p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height;
}
