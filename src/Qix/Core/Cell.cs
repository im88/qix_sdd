namespace Qix.Core;

/// <summary>The state of one playfield cell.</summary>
public enum Cell
{
    /// <summary>Unclaimed area.</summary>
    Empty,

    /// <summary>Frame or edge between claimed and unclaimed area.</summary>
    Border,

    /// <summary>Claimed territory, including demoted border.</summary>
    Claimed,

    /// <summary>Part of the line currently being drawn.</summary>
    Trail,
}
