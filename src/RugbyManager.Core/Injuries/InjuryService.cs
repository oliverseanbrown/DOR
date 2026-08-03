using RugbyManager.Core.Model;
using RugbyManager.Core.Util;

namespace RugbyManager.Core.Injuries;

/// <summary>
/// Handles injuries across a season: each week (round) knocks a week off existing injuries,
/// and every match rolls new ones for the players who took the field, weighted by their
/// injury-proneness. Injured players are automatically left out of the XV by
/// <see cref="Team.SelectBestXV"/>.
/// </summary>
public static class InjuryService
{
    /// <summary>Reduce every player's outstanding injury by one week.</summary>
    public static void HealWeek(Team team)
    {
        foreach (var p in team.Squad)
            if (p.InjuredWeeksRemaining > 0) p.InjuredWeeksRemaining--;
    }

    /// <summary>Roll injuries for the XV that just played. Returns who got hurt, and for how long.</summary>
    public static List<(Player Player, int Weeks)> RollMatchInjuries(Team team, Dice dice)
    {
        var injuries = new List<(Player, int)>();
        foreach (var p in team.Players)
        {
            if (!p.IsFit) continue;
            double prob = 0.015 + p.Attributes.InjuryProneness / 100.0 * 0.05; // ~1.5%–6.5%
            if (dice.Chance(prob))
            {
                int weeks = 1 + dice.NextInt(0, 7); // 1–7 weeks out
                p.InjuredWeeksRemaining = weeks;
                injuries.Add((p, weeks));
            }
        }
        return injuries;
    }
}
