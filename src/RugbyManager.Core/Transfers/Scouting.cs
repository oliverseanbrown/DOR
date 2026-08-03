using RugbyManager.Core.Model;

namespace RugbyManager.Core.Transfers;

/// <summary>
/// Fog-of-war over player ability. An unscouted player's overall is only known as a range;
/// scouting narrows it until the true value is revealed. Signing a player reveals them fully.
/// </summary>
public static class Scouting
{
    public const int ScoutCost = 2_000;   // per scouting report
    public const int ScoutGain = 40;      // knowledge added per report
    private const int MaxMargin = 12;      // ± on a completely unknown player

    public static bool FullyKnown(Player p) => p.Scouted >= 100;

    /// <summary>The estimated overall range shown to the manager (narrows with knowledge).</summary>
    public static (int Low, int High) OverallRange(Player p)
    {
        int ovr = PlayerRating.Overall(p);
        int margin = (int)Math.Round((100 - Math.Clamp(p.Scouted, 0, 100)) / 100.0 * MaxMargin);
        return (Math.Max(1, ovr - margin), Math.Min(99, ovr + margin));
    }

    /// <summary>Commission one scouting report on a player.</summary>
    public static void Scout(Player p) => p.Scouted = Math.Min(100, p.Scouted + ScoutGain);
}
