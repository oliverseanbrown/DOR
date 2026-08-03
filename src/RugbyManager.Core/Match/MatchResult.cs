namespace RugbyManager.Core.Match;

/// <summary>The complete outcome of a simulated match: scores, box score, and the full event feed.</summary>
public sealed class MatchResult
{
    public required string HomeName { get; init; }
    public required string AwayName { get; init; }
    public required string HomeShort { get; init; }
    public required string AwayShort { get; init; }

    public int HomeScore { get; init; }
    public int AwayScore { get; init; }

    public required MatchStats HomeStats { get; init; }
    public required MatchStats AwayStats { get; init; }

    public required IReadOnlyList<MatchEvent> Events { get; init; }

    /// <summary>The seed the match was generated from — replay it to reproduce exactly.</summary>
    public int Seed { get; init; }

    public string ScoreLine => $"{HomeShort} {HomeScore} - {AwayScore} {AwayShort}";

    public string Winner =>
        HomeScore > AwayScore ? HomeName :
        AwayScore > HomeScore ? AwayName : "Draw";
}
