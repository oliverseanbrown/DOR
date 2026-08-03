using RugbyManager.Core.Facilities;
using RugbyManager.Core.Model;

namespace RugbyManager.Core.Finance;

/// <summary>One week's cash flow for a club.</summary>
public sealed record WeeklyLedger(int Wages, int Sponsorship, int Gate, int Net);

/// <summary>
/// Club cash flow. Each match week a club pays wages and earns sponsorship (from the
/// clubhouse's sponsor deals), plus gate receipts when at home — a higher league position pulls
/// bigger demand, but the stadium's own <see cref="StadiumLevelInfo.GateCap"/> is what actually
/// caps how much of that demand a small ground can convert into money. Applied to the player's
/// club only.
/// </summary>
public static class FinanceService
{
    private const int BaseGateDemand = 11_000;

    /// <summary>A player's weekly wage, scaled off ability (with a small floor).</summary>
    public static int WeeklyWage(Player p) => PlayerRating.Overall(p) * 5 + 200;

    public static int WeeklyWageBill(Team club)
        => club.Squad.Sum(WeeklyWage) + club.Coaches.Sum(c => c.Wage);

    /// <summary>Process a week's finances and apply the net to the club's balance.</summary>
    public static WeeklyLedger ProcessWeek(Team club, bool homeGame, int leaguePosition, int teamCount)
    {
        int wages = WeeklyWageBill(club);
        int sponsorship = FacilityService.ClubhouseInfo(club).SponsorshipPerWeek;

        int gate = 0;
        if (homeGame)
        {
            int demand = Math.Max(5_000, BaseGateDemand + (teamCount / 2 - leaguePosition) * 700);
            gate = Math.Min(demand, FacilityService.StadiumInfo(club).GateCap);
        }

        int net = sponsorship + gate - wages;
        club.Money += net;
        return new WeeklyLedger(wages, sponsorship, gate, net);
    }
}
