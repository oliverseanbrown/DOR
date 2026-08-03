namespace RugbyManager.Core.Match;

/// <summary>Box-score style tallies accumulated for one team over a match.</summary>
public sealed class MatchStats
{
    public int Tries { get; set; }
    public int Conversions { get; set; }
    public int PenaltyGoals { get; set; }
    public int DropGoals { get; set; }

    public int LineBreaks { get; set; }
    public int TurnoversWon { get; set; }
    public int PenaltiesConceded { get; set; }
    public int KnockOns { get; set; }

    /// <summary>Accumulated in-match minutes; converted to percentages at full time.</summary>
    public double PossessionMinutes { get; set; }
    public double TerritoryMinutes { get; set; }

    public double PossessionPct { get; set; }
    public double TerritoryPct { get; set; }
}
