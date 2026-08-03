using RugbyManager.Core.Facilities;
using RugbyManager.Core.Model;
using RugbyManager.Core.Util;

namespace RugbyManager.Core.Training;

/// <summary>Summary of a training week's effect on a squad.</summary>
public sealed record TrainingReport(
    TrainingFocus Focus,
    int Improvements,
    double AverageCondition,
    IReadOnlyList<AttributeGain> Gains);

/// <summary>
/// Applies a training week and post-match fatigue to a club's players. Development is
/// youth-weighted and capped by each player's potential, so training young talent pays off
/// over a career. Condition is the week-to-week freshness the manager must balance against it.
/// </summary>
public static class TrainingService
{
    private const int MatchDepletion = 20;

    /// <summary>Run a training week: recover condition and (unless resting) develop attributes.</summary>
    public static TrainingReport ApplyWeek(Team club, TrainingFocus focus, Dice dice)
    {
        int recovery = focus switch
        {
            TrainingFocus.Rest => 35,
            TrainingFocus.Fitness => 8, // hardest work, least recovery
            _ => 14,
        };

        double coachMult = CoachMultiplier(club, focus);
        double groundMult = FacilityService.TrainingGroundInfo(club).DevelopmentMultiplier;
        var gains = new List<AttributeGain>();
        foreach (var player in club.Squad) // the whole squad trains, not just the XV
        {
            player.Condition = Math.Clamp(player.Condition + recovery, 0, 100);
            if (focus != TrainingFocus.Rest)
                gains.AddRange(Develop(player, focus, dice, coachMult * groundMult));
        }

        TrainPlaybook(club, focus, dice);

        return new TrainingReport(focus, gains.Count, club.Squad.Average(p => p.Condition), gains);
    }

    /// <summary>Deplete condition after a match for the XV that played.</summary>
    public static void DepleteAfterMatch(Team club)
    {
        foreach (var player in club.Players)
        {
            int loss = MatchDepletion + (int)((100 - player.Attributes.Stamina) * 0.1);
            player.Condition = Math.Max(0, player.Condition - loss);
        }
    }

    private static List<AttributeGain> Develop(Player player, TrainingFocus focus, Dice dice, double facilityMult)
    {
        double ageFactor = player.Age switch
        {
            <= 21 => 1.0,
            <= 25 => 0.7,
            <= 29 => 0.4,
            _ => 0.15,
        };

        var a = player.Attributes;
        int potential = a.Potential;
        var gains = new List<AttributeGain>();

        foreach (var (name, get, set) in AttributesFor(focus, a))
        {
            int cur = get();
            if (cur >= potential || cur >= 99) continue;
            // Harder to improve as an attribute climbs; youth, a matching coach and a better
            // training ground all accelerate it.
            double chance = 0.30 * ageFactor * (1 - cur / 100.0) * facilityMult;
            if (dice.Chance(chance))
            {
                int newValue = cur + 1;
                set(newValue);
                player.TrainingGains[name] = player.TrainingGains.GetValueOrDefault(name) + 1;
                gains.Add(new AttributeGain(player.FullName, name, newValue));
            }
        }

        return gains;
    }

    /// <summary>Development boost (>=1.0) from a coach whose specialty matches the training focus.</summary>
    private static double CoachMultiplier(Team club, TrainingFocus focus)
    {
        CoachSpecialty[] specs = focus switch
        {
            TrainingFocus.Fitness => new[] { CoachSpecialty.Fitness },
            TrainingFocus.Handling => new[] { CoachSpecialty.Attack },
            TrainingFocus.SetPiece => new[] { CoachSpecialty.Scrum, CoachSpecialty.Lineout },
            TrainingFocus.Defence => new[] { CoachSpecialty.Defence },
            TrainingFocus.Kicking => new[] { CoachSpecialty.Kicking },
            _ => Array.Empty<CoachSpecialty>(),
        };
        int best = specs.Select(club.CoachRating).DefaultIfEmpty(0).Max();
        return 1.0 + best / 100.0 * 0.6;
    }

    /// <summary>
    /// A play's familiarity moves with what the team actually spends the week on: Handling
    /// drills sharpen backline moves, Set-Piece drills sharpen scrum and lineout plays. A play
    /// that isn't the week's focus goes slightly rusty rather than staying frozen — a playbook
    /// needs upkeep, not just a one-off study session.
    /// </summary>
    private static void TrainPlaybook(Team club, TrainingFocus focus, Dice dice)
    {
        foreach (var play in club.Playbook)
        {
            bool drilled = focus switch
            {
                TrainingFocus.Handling => play.Area == SetPlayArea.BacklineMove,
                TrainingFocus.SetPiece => play.Area is SetPlayArea.LineoutPlay or SetPlayArea.ScrumPlay,
                _ => false,
            };

            if (drilled)
                club.BumpFamiliarity(play.Name, 3 + dice.NextInt(0, 3));
            else if (club.GetFamiliarity(play.Name) > 20)
                club.BumpFamiliarity(play.Name, -1);
        }
    }

    private static IEnumerable<(string Name, Func<int> Get, Action<int> Set)> AttributesFor(TrainingFocus focus, PlayerAttributes a)
        => focus switch
        {
            TrainingFocus.Fitness => new (string, Func<int>, Action<int>)[]
            {
                ("Stamina", () => a.Stamina, v => a.Stamina = v),
                ("Strength", () => a.Strength, v => a.Strength = v),
                ("WorkRate", () => a.WorkRate, v => a.WorkRate = v),
            },
            TrainingFocus.Handling => new (string, Func<int>, Action<int>)[]
            {
                ("Handling", () => a.Handling, v => a.Handling = v),
                ("Passing", () => a.Passing, v => a.Passing = v),
                ("Vision", () => a.Vision, v => a.Vision = v),
            },
            TrainingFocus.SetPiece => new (string, Func<int>, Action<int>)[]
            {
                ("Scrummaging", () => a.Scrummaging, v => a.Scrummaging = v),
                ("Lineout", () => a.Lineout, v => a.Lineout = v),
            },
            TrainingFocus.Defence => new (string, Func<int>, Action<int>)[]
            {
                ("Tackling", () => a.Tackling, v => a.Tackling = v),
                ("Positioning", () => a.Positioning, v => a.Positioning = v),
                ("Breakdown", () => a.Breakdown, v => a.Breakdown = v),
            },
            TrainingFocus.Kicking => new (string, Func<int>, Action<int>)[]
            {
                ("Kicking", () => a.Kicking, v => a.Kicking = v),
                ("GoalKicking", () => a.GoalKicking, v => a.GoalKicking = v),
            },
            _ => Array.Empty<(string, Func<int>, Action<int>)>(),
        };
}
