namespace RugbyManager.Core.Match;

public enum MatchEventType
{
    Info,
    Kickoff,
    Try,
    Conversion,
    ConversionMissed,
    PenaltyGoal,
    PenaltyMissed,
    DropGoal,
    DropGoalMissed,
    Penalty,
    Scrum,
    Lineout,
    LineoutSteal,
    LineBreak,
    Carry,
    MissedTackle,
    Turnover,
    KnockOn,
    KickToTouch,
    KickTerritory,
    Substitution,
    TacticsChange,
    HalfTime,
    FullTime,
}

/// <summary>
/// How big a moment this is. A presentation layer (console, web) can use this to pace
/// playback — lingering on <see cref="Highlight"/> events instead of rushing past them like
/// routine phase-play. The simulation itself never reads this; it's purely descriptive.
/// </summary>
public enum Drama
{
    Routine,
    Highlight,
}

/// <summary>
/// A single moment in a match — the atomic unit the commentary feed and (later) the
/// visual match renderer both play back. The score is captured at the moment of the event.
/// </summary>
/// <param name="Minute">Match minute (rounded).</param>
/// <param name="Type">What happened.</param>
/// <param name="Text">Commentary line.</param>
/// <param name="Team">0 = home, 1 = away, or null for neutral events.</param>
/// <param name="Drama">How significant the moment is, for pacing playback.</param>
/// <param name="Scorer">For Try/Conversion/PenaltyGoal/DropGoal events, the scoring player's short name — lets a summary panel list "who, when" without re-parsing commentary prose.</param>
public readonly record struct MatchEvent(
    int Minute,
    MatchEventType Type,
    string Text,
    int? Team,
    int HomeScore,
    int AwayScore,
    Drama Drama = Drama.Routine,
    string? Scorer = null);
