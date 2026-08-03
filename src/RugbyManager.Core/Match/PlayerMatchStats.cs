namespace RugbyManager.Core.Match;

/// <summary>
/// One player's contribution to a match so far — updated live as the engine steps through
/// events, so a UI can show it evolving during play, not just at full time.
/// </summary>
public sealed class PlayerMatchStats
{
    public int Carries { get; set; }
    public double MetresGained { get; set; }
    public int DefendersBeaten { get; set; }
    public int Passes { get; set; }
    public int Kicks { get; set; }
    public int Tackles { get; set; }
    public int TurnoversWon { get; set; }
    public int Errors { get; set; }
    public int Tries { get; set; }
    public int Conversions { get; set; }
    public int PenaltyGoals { get; set; }

    /// <summary>FM-style 1-10 match rating, recomputed live from the contribution so far.</summary>
    public double Rating
    {
        get
        {
            double r = 6.0
                + Tries * 1.5
                + Conversions * 0.4
                + PenaltyGoals * 0.5
                + DefendersBeaten * 0.3
                + TurnoversWon * 0.4
                + Tackles * 0.12
                + MetresGained * 0.01
                + Passes * 0.03
                - Errors * 0.5;
            return Math.Clamp(Math.Round(r, 1), 2.0, 10.0);
        }
    }
}
