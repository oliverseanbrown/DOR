namespace RugbyManager.Core.Match;

/// <summary>
/// A technique a ball-carrier can use to try to beat the last defender in a breakaway.
/// Which one a player reaches for depends on their skill set (see
/// <see cref="MatchEngine"/>'s move-scoring), and whether it works is a real contest against
/// the defender's attributes — it can fail.
/// </summary>
public enum BeatMove
{
    /// <summary>Power through the tackle attempt. Strength-led.</summary>
    RunThrough,
    /// <summary>A stiff-arm fend. Strength and a little agility.</summary>
    HandOff,
    /// <summary>A sharp change of direction. Agility-led.</summary>
    Sidestep,
    /// <summary>A sudden change of pace. Acceleration-led.</summary>
    Goosestep,
    /// <summary>Sell the pass that never comes. Decision-making and composure.</summary>
    Dummy,
    /// <summary>Kick past the defender and win the race. Kicking and pace.</summary>
    ChipAndChase,
    /// <summary>Draw the defender and put a support runner through. Passing and vision.</summary>
    OffloadPass,
}
