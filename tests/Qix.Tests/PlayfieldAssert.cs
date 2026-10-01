using Qix.Core;

namespace Qix.Tests;

/// <summary>Helpers for checking playfield state in tests.</summary>
internal static class PlayfieldAssert
{
    public static IEnumerable<Cell> AllCells(Playfield playfield) =>
        AllPoints(playfield).Select(p => playfield[p]);

    public static IEnumerable<Point> AllPoints(Playfield playfield)
    {
        for (var y = 0; y < playfield.Height; y++)
        {
            for (var x = 0; x < playfield.Width; x++)
            {
                yield return new Point(x, y);
            }
        }
    }

    /// <summary>Asserts every cell in the rectangle [x0..x1] × [y0..y1] has the given state.</summary>
    public static void Rect(Playfield playfield, int x0, int y0, int x1, int y1, Cell expected)
    {
        for (var y = y0; y <= y1; y++)
        {
            for (var x = x0; x <= x1; x++)
            {
                Assert.True(playfield[new Point(x, y)] == expected, $"({x}, {y}) is {playfield[new Point(x, y)]}, expected {expected}");
            }
        }
    }

    /// <summary>Data-model invariant: every Border cell has an Empty cell among its 8 neighbours.</summary>
    public static void BorderTouchesEmpty(Playfield playfield)
    {
        if (!AllCells(playfield).Contains(Cell.Empty))
        {
            return;
        }

        foreach (var p in AllPoints(playfield).Where(p => playfield[p] == Cell.Border))
        {
            var touches = false;
            for (var dy = -1; dy <= 1; dy++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    var n = new Point(p.X + dx, p.Y + dy);
                    touches |= playfield.Contains(n) && playfield[n] == Cell.Empty;
                }
            }

            Assert.True(touches, $"Border cell ({p.X}, {p.Y}) has no Empty neighbour");
        }
    }

    /// <summary>Counts the 4-connected regions of Empty cells.</summary>
    public static int EmptyRegionCount(Playfield playfield)
    {
        var seen = new HashSet<Point>();
        var regions = 0;
        foreach (var start in AllPoints(playfield).Where(p => playfield[p] == Cell.Empty))
        {
            if (!seen.Add(start))
            {
                continue;
            }

            regions++;
            var queue = new Queue<Point>([start]);
            while (queue.TryDequeue(out var p))
            {
                foreach (var d in Enum.GetValues<Direction>())
                {
                    var n = p + d;
                    if (playfield.Contains(n) && playfield[n] == Cell.Empty && seen.Add(n))
                    {
                        queue.Enqueue(n);
                    }
                }
            }
        }

        return regions;
    }
}
