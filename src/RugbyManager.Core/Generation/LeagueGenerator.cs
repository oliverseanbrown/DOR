using RugbyManager.Core.Competition;
using RugbyManager.Core.Model;

namespace RugbyManager.Core.Generation;

/// <summary>
/// Builds a division of grass-roots clubs with a spread of quality and distinct tactical
/// identities, so a simulated season produces a believable, varied table. Deterministic
/// for a given seed.
/// </summary>
public static class LeagueGenerator
{
    private static readonly (string Name, string Short)[] Clubs =
    {
        ("Ashcombe RFC", "ASH"),
        ("Riverside Rangers", "RIV"),
        ("Old Castellians", "OCA"),
        ("Marsh Lane RUFC", "MRL"),
        ("Hillcrest Harriers", "HIL"),
        ("Kingsford RFC", "KGF"),
        ("Barrowfield RUFC", "BAR"),
        ("Meadowvale RFC", "MDV"),
        ("Dunbridge Wanderers", "DUN"),
        ("Fenwick RUFC", "FEN"),
        ("Thornbury RFC", "THB"),
        ("Stormont Park", "STP"),
        ("Colliers Wood", "COL"),
        ("Wraysbury RFC", "WRY"),
        ("Netherby RUFC", "NET"),
        ("Oakhill Foresters", "OAK"),
    };

    private static readonly Tactics[] Identities =
    {
        new() { PlayStyle = PlayStyle.ForwardsOriented, BreakdownFocus = BreakdownFocus.Aggressive, DefensiveLine = DefensiveLine.Standard, PenaltyPhilosophy = PenaltyPhilosophy.Pragmatic, KickingTendency = 55 },
        new() { PlayStyle = PlayStyle.Expansive, BreakdownFocus = BreakdownFocus.Balanced, DefensiveLine = DefensiveLine.Rush, PenaltyPhilosophy = PenaltyPhilosophy.Ambitious, KickingTendency = 28 },
        new() { PlayStyle = PlayStyle.Balanced, BreakdownFocus = BreakdownFocus.Balanced, DefensiveLine = DefensiveLine.Standard, PenaltyPhilosophy = PenaltyPhilosophy.Balanced, KickingTendency = 42 },
        new() { PlayStyle = PlayStyle.KickingGame, BreakdownFocus = BreakdownFocus.Conservative, DefensiveLine = DefensiveLine.Drift, PenaltyPhilosophy = PenaltyPhilosophy.Pragmatic, KickingTendency = 68 },
    };

    /// <param name="firstClubQuality">
    /// If set, fixes the quality of the first club (typically the player's club) so the
    /// starting experience is fair; the rest keep a genuine but not brutal spread.
    /// </param>
    /// <param name="baseQuality">
    /// If set, centres opponent quality on this value (used to make higher pyramid tiers
    /// stronger). Defaults to a mid-table regional spread.
    /// </param>
    public static League Generate(string name, int teamCount, int seed, int? firstClubQuality = null, int? baseQuality = null)
    {
        if (teamCount < 2) throw new ArgumentException("A league needs at least two teams.", nameof(teamCount));
        if (teamCount > Clubs.Length) throw new ArgumentException($"Only {Clubs.Length} club names are available.", nameof(teamCount));

        var rng = new Random(seed);
        var teams = new List<Team>(teamCount);
        int lo = baseQuality is { } b ? b - 6 : 58;
        int hi = baseQuality is { } b2 ? b2 + 5 : 73;

        for (int i = 0; i < teamCount; i++)
        {
            var (clubName, shortName) = Clubs[i];
            int quality = i == 0 && firstClubQuality is { } q
                ? Math.Clamp(q, 1, 99)
                : Math.Clamp(rng.Next(lo, hi), 35, 95);
            var tactics = Identities[rng.Next(Identities.Length)];
            teams.Add(SquadGenerator.Generate(clubName, shortName, quality, tactics, seed: seed * 100 + i));
        }

        return new League { Name = name, Teams = teams };
    }
}
