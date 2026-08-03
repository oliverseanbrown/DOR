using static RugbyManager.Core.Model.Position;

namespace RugbyManager.Core.Model;

/// <summary>
/// Computes a single position-weighted "overall" rating (1-99) for a player, for squad
/// screens and (later) transfer valuations. This is a summary for humans — the match engine
/// still reads individual attributes, it never uses this number.
/// </summary>
public static class PlayerRating
{
    public static int Overall(Player p)
    {
        var a = p.Attributes;
        double r = p.NaturalPosition switch
        {
            LooseheadProp or TightheadProp =>
                W((a.Scrummaging, 3), (a.Strength, 2), (a.Tackling, 1), (a.Breakdown, 1), (a.WorkRate, 1)),
            Hooker =>
                W((a.Scrummaging, 2), (a.Lineout, 2), (a.Strength, 1), (a.Tackling, 1), (a.WorkRate, 1)),
            Lock4 or Lock5 =>
                W((a.Lineout, 2), (a.Strength, 2), (a.Scrummaging, 1), (a.Tackling, 1), (a.WorkRate, 1)),
            BlindsideFlanker =>
                W((a.Breakdown, 2), (a.Tackling, 2), (a.Strength, 1), (a.WorkRate, 1), (a.Lineout, 1)),
            OpensideFlanker =>
                W((a.Breakdown, 3), (a.Tackling, 2), (a.WorkRate, 2), (a.Pace, 1)),
            Number8 =>
                W((a.Breakdown, 2), (a.Strength, 2), (a.Handling, 1), (a.Pace, 1), (a.Tackling, 1)),
            ScrumHalf =>
                W((a.Passing, 3), (a.Vision, 2), (a.DecisionMaking, 2), (a.Pace, 1)),
            FlyHalf =>
                W((a.DecisionMaking, 2), (a.Kicking, 2), (a.Passing, 2), (a.GoalKicking, 1), (a.Composure, 1), (a.Vision, 1)),
            InsideCentre =>
                W((a.Tackling, 2), (a.Handling, 2), (a.Strength, 1), (a.Passing, 1), (a.Pace, 1)),
            OutsideCentre =>
                W((a.Pace, 2), (a.Handling, 2), (a.Agility, 1), (a.Tackling, 1), (a.Acceleration, 1)),
            LeftWing or RightWing =>
                W((a.Pace, 3), (a.Acceleration, 2), (a.Agility, 1), (a.Handling, 1)),
            Fullback =>
                W((a.Positioning, 2), (a.Handling, 2), (a.Kicking, 1), (a.Pace, 1), (a.Composure, 1)),
            _ => W((a.Handling, 1), (a.Tackling, 1), (a.WorkRate, 1)),
        };
        return Math.Clamp((int)Math.Round(r), 1, 99);
    }

    private static double W(params (int value, int weight)[] terms)
    {
        double sum = 0, wsum = 0;
        foreach (var (value, weight) in terms) { sum += value * weight; wsum += weight; }
        return wsum > 0 ? sum / wsum : 0;
    }
}
