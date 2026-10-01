using Qix.Core;
using static Qix.Tests.PlayfieldAssert;

namespace Qix.Tests;

public class QixClaimingTests
{
    [Fact]
    public void Cutting_off_the_qix_claims_the_larger_side()
    {
        // 10×6, interior 8×4 = 32. The Qix sits in the left column; a cut up x = 2 leaves it
        // only x = 1 and claims the right side x 3..8: 24 cells plus 4 line cells = 87%.
        var game = new Game(new Playfield(10, 6), new Random(1));
        game.Qix.Position = new Point(1, 1);
        for (var i = 0; i < 3; i++)
        {
            game.Move(Direction.Left);
        }

        game.ToggleDraw();
        for (var i = 0; i < 5; i++)
        {
            game.Move(Direction.Up);
        }

        Rect(game.Playfield, 3, 1, 8, 4, Cell.Claimed);
        Rect(game.Playfield, 1, 1, 1, 4, Cell.Empty);
        Assert.Equal(Cell.Empty, game.Playfield[game.Qix.Position]);
        Assert.Equal(87, game.Playfield.ClaimedPercent);
        Assert.Equal(GamePhase.Complete, game.Phase);
    }
}
