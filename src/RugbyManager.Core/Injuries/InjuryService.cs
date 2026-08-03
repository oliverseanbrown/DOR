using RugbyManager.Core.Facilities;
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
        double pitchMult = FacilityService.PitchInfo(team).InjuryMultiplier;
        double recoveryMult = FacilityService.TrainingGroundInfo(team).InjuryRecoveryMultiplier;

        var injuries = new List<(Player, int)>();
        foreach (var p in team.Players)
        {
            if (!p.IsFit) continue;
            double prob = (0.015 + p.Attributes.InjuryProneness / 100.0 * 0.05) * pitchMult; // ~1.5%–6.5% at baseline
            if (dice.Chance(prob))
            {
                // 1-7 weeks out at baseline; a better medical/conditioning setup shortens it.
                int weeks = Math.Max(1, (int)Math.Round((1 + dice.NextInt(0, 7)) / recoveryMult));
                p.InjuredWeeksRemaining = weeks;
                injuries.Add((p, weeks));
            }
        }
        return injuries;
    }
}
