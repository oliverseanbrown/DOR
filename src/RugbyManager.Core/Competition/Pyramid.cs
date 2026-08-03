namespace RugbyManager.Core.Competition;

public enum SeasonResult { Promoted, Stayed, Relegated }

/// <summary>
/// The league pyramid the player's club climbs and falls through across seasons. Rival clubs
/// are regenerated each season at the tier's strength (a deliberate simplification); the
/// player's own club, squad and finances persist and progress.
/// </summary>
public static class Pyramid
{
    private static readonly string[] TierNames =
    {
        "Premier Division",   // 0 — the top
        "Championship",       // 1
        "National League 1",  // 2
        "National League 2",  // 3
        "Regional Premier",   // 4 — where a new career starts
        "Regional One",       // 5
        "County League",      // 6 — the bottom
    };

    public const int TopTier = 0;
    public static int BottomTier => TierNames.Length - 1;
    public const int StartingTier = 4;

    /// <summary>Number of clubs promoted/relegated between adjacent tiers each season.</summary>
    public const int PromotionSpots = 2;
    public const int RelegationSpots = 2;

    public static string Name(int tier) => TierNames[Math.Clamp(tier, TopTier, BottomTier)];

    /// <summary>Typical squad quality at a tier: the top flight is far stronger than the county league.</summary>
    public static int BaseQuality(int tier) => 84 - Math.Clamp(tier, TopTier, BottomTier) * 5;

    public static SeasonResult ResultFor(int finalPosition, int teamCount)
    {
        if (finalPosition <= PromotionSpots) return SeasonResult.Promoted;
        if (finalPosition > teamCount - RelegationSpots) return SeasonResult.Relegated;
        return SeasonResult.Stayed;
    }

    /// <summary>Apply promotion/relegation to a tier, respecting the top and bottom of the pyramid.</summary>
    public static int NextTier(int tier, SeasonResult result) => result switch
    {
        SeasonResult.Promoted => Math.Max(TopTier, tier - 1),
        SeasonResult.Relegated => Math.Min(BottomTier, tier + 1),
        _ => tier,
    };
}
