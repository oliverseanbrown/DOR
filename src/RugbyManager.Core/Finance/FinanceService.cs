using RugbyManager.Core.Model;

namespace RugbyManager.Core.Finance;

/// <summary>One week's cash flow for a club.</summary>
public sealed record WeeklyLedger(int Wages, int Sponsorship, int Gate, int Net);

/// <summary>
/// Club cash flow. Each match week a club pays wages and earns sponsorship, plus gate
/// receipts when at home — and a higher league position pulls a bigger crowd, so success pays.
/// Applied to the player's club only in Phase 2.
/// </summary>
public static class FinanceService
{
    private const int SponsorshipPerWeek = 8_000;
    private const int BaseGate = 11_000;

    /// <summary>A player's weekly wage, scaled off ability (with a small floor).</summary>
    public static int WeeklyWage(Player p) => PlayerRating.Overall(p) * 5 + 200;

    public static int WeeklyWageBill(Team club)
        => club.Squad.Sum(WeeklyWage) + club.Coaches.Sum(c => c.Wage);

    /// <summary>Process a week's finances and apply the net to the club's balance.</summary>
    public static WeeklyLedger ProcessWeek(Team club, bool homeGame, int leaguePosition, int teamCount)
    {
        int wages = WeeklyWageBill(club);
        int sponsorship = SponsorshipPerWeek;
        int gate = homeGame
            ? Math.Max(5_000, BaseGate + (teamCount / 2 - leaguePosition) * 700)
            : 0;

        int net = sponsorship + gate - wages;
        club.Money += net;
        return new WeeklyLedger(wages, sponsorship, gate, net);
    }
}
