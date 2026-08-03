namespace RugbyManager.Core.Model;

/// <summary>
/// A player's cumulative contribution for one season of their career, rolled up from every
/// match they appeared in. Kept per season (rather than a single running total) so a manager
/// can review "how did they do this season" as well as their career body of work.
/// </summary>
public sealed class CareerPlayerStats
{
    public int MatchesPlayed { get; set; }
    public int Tries { get; set; }
    public int Assists { get; set; }
    public int Conversions { get; set; }
    public int PenaltyGoals { get; set; }
    public int Carries { get; set; }
    public double MetresGained { get; set; }
    public int DefendersBeaten { get; set; }
    public int Passes { get; set; }
    public int Tackles { get; set; }
    public int TurnoversWon { get; set; }
    public int Errors { get; set; }
}
