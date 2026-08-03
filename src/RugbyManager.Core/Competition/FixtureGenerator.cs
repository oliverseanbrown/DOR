using RugbyManager.Core.Model;

namespace RugbyManager.Core.Competition;

/// <summary>
/// Builds a balanced double round-robin schedule using the circle method.
/// Every pair of clubs meets twice — once at each ground — so each team ends the
/// season with an equal number of home and away fixtures.
/// </summary>
public static class FixtureGenerator
{
    public static List<Fixture> DoubleRoundRobin(IReadOnlyList<Team> teams)
    {
        if (teams.Count < 2)
            throw new ArgumentException("A league needs at least two teams.", nameof(teams));

        // The circle method needs an even count; a null slot acts as a "bye".
        var slots = new List<Team?>(teams);
        bool hasBye = slots.Count % 2 != 0;
        if (hasBye) slots.Add(null);

        int n = slots.Count;
        int roundsPerLeg = n - 1;
        var fixtures = new List<Fixture>();
        int index = 0;

        // First leg: rotate all but the first slot each round.
        var arrangement = new List<Team?>(slots);
        for (int round = 0; round < roundsPerLeg; round++)
        {
            for (int i = 0; i < n / 2; i++)
            {
                var a = arrangement[i];
                var b = arrangement[n - 1 - i];
                if (a is null || b is null) continue; // bye — that team rests this round

                // Alternate home/away by round parity so the schedule isn't lopsided.
                var (home, away) = round % 2 == 0 ? (a, b) : (b, a);
                fixtures.Add(new Fixture { Index = index++, Round = round, Home = home, Away = away });
            }
            Rotate(arrangement);
        }

        // Second leg: mirror the first with home/away swapped. This guarantees each
        // ordered pairing (A-at-home-v-B and B-at-home-v-A) occurs exactly once.
        int firstLegCount = fixtures.Count;
        for (int f = 0; f < firstLegCount; f++)
        {
            var fx = fixtures[f];
            fixtures.Add(new Fixture
            {
                Index = index++,
                Round = fx.Round + roundsPerLeg,
                Home = fx.Away,
                Away = fx.Home,
            });
        }

        return fixtures;
    }

    /// <summary>Keep slot 0 fixed; rotate the rest one place clockwise (circle method).</summary>
    private static void Rotate(List<Team?> arrangement)
    {
        if (arrangement.Count < 3) return;
        var last = arrangement[^1];
        arrangement.RemoveAt(arrangement.Count - 1);
        arrangement.Insert(1, last);
    }
}
