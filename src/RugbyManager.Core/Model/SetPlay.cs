namespace RugbyManager.Core.Model;

/// <summary>Where on the field a set play is launched from.</summary>
public enum SetPlayArea
{
    /// <summary>A backline strike move in open play near the opposition line.</summary>
    BacklineMove,
    /// <summary>A play off the lineout (catch-and-drive, off-the-top strike, peel).</summary>
    LineoutPlay,
    /// <summary>A move off the scrum (8-9 pick, blindside break).</summary>
    ScrumPlay,
}

/// <summary>
/// A rehearsed set play. Higher <see cref="Difficulty"/> plays are more spectacular but harder
/// to pull off; a coach of the matching <see cref="Coaching"/> specialty executes them better —
/// which is exactly why coaching staff and the playbook go hand in hand.
/// </summary>
public sealed class SetPlay
{
    public required string Name { get; init; }
    public SetPlayArea Area { get; init; }

    /// <summary>1-100. Higher = bigger reward but lower base execution.</summary>
    public int Difficulty { get; init; }

    /// <summary>The coaching specialty that helps run this play well.</summary>
    public CoachSpecialty Coaching { get; init; }

    /// <summary>
    /// The 1-3 positions central to actually running this play (e.g. scrum-half and number 8
    /// for a pick-and-go). Execution quality is drawn from exactly these players — sell or
    /// injure one and the move genuinely gets worse, regardless of how "known" it is.
    /// Defaults to empty for backward compatibility with older saved playbooks.
    /// </summary>
    public Position[] KeyPositions { get; init; } = Array.Empty<Position>();
}
