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

    /// <summary>
    /// Share of interior cells that are not Empty, rounded down (research R6). Updated only by
    /// <see cref="CloseTrail"/>, so a line being drawn doesn't change it.
    /// </summary>
    public int ClaimedPercent { get; private set; }

    /// <summary>Callers check <see cref="Contains"/> first.</summary>
    public Cell this[Point p] => _cells[p.X, p.Y];

    public bool Contains(Point p) => p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height;

    /// <summary>Marks an <see cref="Cell.Empty"/> cell as part of the line being drawn.</summary>
    public void SetTrail(Point p)
    {
        if (_cells[p.X, p.Y] != Cell.Empty)
        {
            throw new InvalidOperationException($"Cell ({p.X}, {p.Y}) is {_cells[p.X, p.Y]}, not Empty.");
        }

        _cells[p.X, p.Y] = Cell.Trail;
    }

    /// <summary>
    /// Completes the line: it becomes border, every Empty region except the largest is claimed,
    /// and border cells that no longer touch open area are demoted (research R5).
    /// </summary>
    public void CloseTrail()
    {
        Replace(Cell.Trail, Cell.Border);
        ClaimAllButLargestRegion();
        DemoteEnclosedBorder();
        ClaimedPercent = 100 * CountNonEmptyInterior() / InteriorCount;
    }

    private int CountNonEmptyInterior()
    {
        var count = 0;
        for (var x = 1; x < Width - 1; x++)
        {
            for (var y = 1; y < Height - 1; y++)
            {
                if (_cells[x, y] != Cell.Empty)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private void Replace(Cell from, Cell to)
    {
        for (var x = 0; x < Width; x++)
        {
            for (var y = 0; y < Height; y++)
            {
                if (_cells[x, y] == from)
                {
                    _cells[x, y] = to;
                }
            }
        }
    }

    private void ClaimAllButLargestRegion()
    {
        // Rows are scanned top to bottom, left to right, so region 0 holds the topmost-leftmost
        // Empty cell. Keeping the first of equally large regions is the spec's tie rule.
        var regionOf = new int[Width, Height];
        var sizes = new List<int>();
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                if (_cells[x, y] == Cell.Empty && regionOf[x, y] == 0)
                {
                    sizes.Add(FloodFill(new Point(x, y), regionOf, sizes.Count + 1));
                }
            }
        }

        if (sizes.Count < 2)
        {
            return;
        }

        var keep = sizes.IndexOf(sizes.Max()) + 1;
        for (var x = 0; x < Width; x++)
        {
            for (var y = 0; y < Height; y++)
            {
                if (_cells[x, y] == Cell.Empty && regionOf[x, y] != keep)
                {
                    _cells[x, y] = Cell.Claimed;
                }
            }
        }
    }

    /// <summary>Labels the 4-connected Empty region containing <paramref name="start"/>; returns its size.</summary>
    private int FloodFill(Point start, int[,] regionOf, int label)
    {
        var queue = new Queue<Point>();
        regionOf[start.X, start.Y] = label;
        queue.Enqueue(start);
        var size = 0;
        while (queue.TryDequeue(out var p))
        {
            size++;
            foreach (var d in Enum.GetValues<Direction>())
            {
                var n = p + d;
                if (Contains(n) && _cells[n.X, n.Y] == Cell.Empty && regionOf[n.X, n.Y] == 0)
                {
                    regionOf[n.X, n.Y] = label;
                    queue.Enqueue(n);
                }
            }
        }

        return size;
    }

    private void DemoteEnclosedBorder()
    {
        // Collect first, then apply, so the result doesn't depend on scan order.
        var demote = new List<Point>();
        for (var x = 0; x < Width; x++)
        {
            for (var y = 0; y < Height; y++)
            {
                if (_cells[x, y] == Cell.Border && !TouchesEmpty(x, y))
                {
                    demote.Add(new Point(x, y));
                }
            }
        }

        foreach (var p in demote)
        {
            _cells[p.X, p.Y] = Cell.Claimed;
        }
    }

    /// <summary>True if any of the 8 neighbours is Empty.</summary>
    private bool TouchesEmpty(int x, int y)
    {
        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                var n = new Point(x + dx, y + dy);
                if (Contains(n) && _cells[n.X, n.Y] == Cell.Empty)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
