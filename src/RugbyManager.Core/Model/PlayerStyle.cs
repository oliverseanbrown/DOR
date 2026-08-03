namespace RugbyManager.Core.Model;

/// <summary>A player's natural on-field identity, read off their attributes.</summary>
public enum PlayStyleTag
{
    Powerhouse,   // strength/aggression — carries through contact
    Speedster,    // pace/acceleration — finishes in space
    Playmaker,    // vision/decision-making/composure — controls the game
    Enforcer,     // tackling/breakdown — the defensive/collision specialist
    Kicker,       // kicking/goal-kicking
    Distributor,  // passing/handling — the link player
    Allrounder,   // no standout — competent everywhere
}

/// <summary>
/// Derives a readable "role" from a player's attributes — no separate stored data, so it's
/// always in sync with training-driven growth. This is literally the same signal that already
/// drives which breakaway technique a player reaches for (see <c>MatchEngine.PickMove</c>);
/// this just surfaces it as a visible trait instead of leaving it implicit in the dice roll.
/// </summary>
public static class PlayerStyle
{
    private const double AllrounderThreshold = 6.0;

    public static PlayStyleTag Determine(Player p)
    {
        var a = p.Attributes;
        var scores = new (PlayStyleTag Tag, double Score)[]
        {
            (PlayStyleTag.Powerhouse, a.Strength * 0.7 + a.Aggression * 0.3),
            (PlayStyleTag.Speedster, a.Pace * 0.6 + a.Acceleration * 0.4),
            (PlayStyleTag.Playmaker, a.Vision * 0.5 + a.DecisionMaking * 0.3 + a.Composure * 0.2),
            (PlayStyleTag.Enforcer, a.Tackling * 0.6 + a.Breakdown * 0.4),
            (PlayStyleTag.Kicker, a.Kicking * 0.6 + a.GoalKicking * 0.4),
            (PlayStyleTag.Distributor, a.Passing * 0.6 + a.Handling * 0.4),
        };

        var ordered = scores.OrderByDescending(s => s.Score).ToList();
        bool standsOut = ordered[0].Score - ordered[1].Score >= AllrounderThreshold;
        return standsOut ? ordered[0].Tag : PlayStyleTag.Allrounder;
    }
}
