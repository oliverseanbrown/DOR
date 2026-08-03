using RugbyManager.Core.Model;
using static RugbyManager.Core.Model.Position;

namespace RugbyManager.Core.Generation;

/// <summary>
/// Generates plausible teams for testing the match engine. Each position gets an
/// attribute profile tilted toward what that role actually needs, around a base
/// "quality" that sets the club's overall level. Deterministic for a given seed.
/// </summary>
public static class SquadGenerator
{
    private static readonly string[] FirstNames =
    {
        "Jonah", "Sione", "Owen", "Finn", "Dan", "Beauden", "Jamie", "Ardie", "Maro", "Rhys",
        "Cheslin", "Antoine", "Johnny", "Sam", "Tadhg", "Handre", "Faf", "Bundee", "Ellis", "Kwagga",
        "Marcus", "George", "Levani", "Semi", "Pita", "Ben", "Liam", "Caelan", "Josh", "Duhan",
    };

    private static readonly string[] LastNames =
    {
        "Lomu", "Farrell", "Carter", "Barrett", "Itoje", "Kolbe", "Dupont", "Sexton", "Furlong",
        "Pollard", "Genia", "Aki", "Smith", "Williams", "Nakarawa", "Radradra", "Tuilagi", "Youngs",
        "Watson", "Etzebeth", "du Plessis", "Cane", "Savea", "Hogg", "May", "Daly", "Vunipola",
        "Ford", "Nienaber", "Marx", "O'Mahony", "Ringrose", "Beard", "Faletau",
    };

    // Reserve slots so every unit has some cover; the rest is handled out of position.
    private static readonly Position[] ReservePositions =
    {
        Position.LooseheadProp, Position.Hooker, Position.TightheadProp, Position.Lock5,
        Position.BlindsideFlanker, Position.ScrumHalf, Position.FlyHalf,
        Position.OutsideCentre, Position.RightWing,
    };

    public static Team Generate(string name, string shortName, int quality, Tactics tactics, int seed)
    {
        var rng = new Random(seed);
        var usedNames = new HashSet<string>();

        var squad = new List<Player>();
        // 15 first-choice players, one per position, at the club's quality.
        foreach (Position pos in Enum.GetValues<Position>())
            squad.Add(CreatePlayer(pos, quality, rng, usedNames));
        // Reserves, a notch below, providing bench cover.
        foreach (var pos in ReservePositions)
            squad.Add(CreatePlayer(pos, Math.Max(1, quality - 6), rng, usedNames));

        var team = new Team { Name = name, ShortName = shortName, Squad = squad, Tactics = tactics };
        team.SelectBestXV();
        return team;
    }

    /// <summary>
    /// Create a single player for a position at a given quality. Reused by the transfer
    /// market. Pass <paramref name="usedNames"/> to avoid duplicate names within a squad.
    /// </summary>
    public static Player CreatePlayer(Position pos, int quality, Random rng, ISet<string>? usedNames = null,
        int? ageOverride = null, int? potentialOverride = null)
    {
        string first, last, full;
        do
        {
            first = FirstNames[rng.Next(FirstNames.Length)];
            last = LastNames[rng.Next(LastNames.Length)];
            full = $"{first} {last}";
        } while (usedNames is not null && !usedNames.Add(full));

        var attributes = BuildAttributes(pos, quality, rng);
        if (potentialOverride is { } pot) attributes.Potential = Math.Clamp(pot, 1, 99);

        return new Player
        {
            FirstName = first,
            LastName = last,
            Age = ageOverride ?? rng.Next(19, 34),
            NaturalPosition = pos,
            Attributes = attributes,
        };
    }

    private static PlayerAttributes BuildAttributes(Position pos, int quality, Random rng)
    {
        // Every attribute starts near the club's quality, then position tilts are applied.
        int V(int bonus = 0) => Math.Clamp((int)Math.Round(quality + Gauss(rng, 0, 7) + bonus), 1, 99);

        // Baselines
        int str = V(), pace = V(), accel = V(), stam = V(), agil = V();
        int hand = V(), pass = V(), tackle = V(), kick = V(-25), goal = V(-30), lineout = V(-25), scrum = V(-30), bd = V();
        int decision = V(-5), comp = V(), disc = V(), work = V(), lead = V(-5), posn = V(), vision = V(-5), aggr = V();
        int potential = Math.Clamp(quality + rng.Next(0, 20), 1, 99), consistency = V(), injury = rng.Next(1, 60);

        // Position specialisations (bonuses layered on top).
        switch (pos)
        {
            case LooseheadProp:
            case TightheadProp:
                scrum += 35; str += 15; bd += 5; pace -= 12; accel -= 10; hand -= 8; break;
            case Hooker:
                scrum += 28; lineout += 35; str += 10; work += 5; pace -= 8; break;
            case Lock4:
            case Lock5:
                lineout += 34; str += 16; scrum += 12; work += 4; pace -= 8; break;
            case BlindsideFlanker:
                bd += 20; tackle += 14; str += 10; work += 12; lineout += 12; break;
            case OpensideFlanker:
                bd += 26; tackle += 12; work += 15; pace += 6; agil += 6; break;
            case Number8:
                bd += 16; str += 12; hand += 10; pace += 6; tackle += 8; lineout += 8; break;
            case ScrumHalf:
                pass += 24; vision += 16; decision += 14; pace += 8; accel += 8; str -= 8; break;
            case FlyHalf:
                decision += 22; kick += 34; goal += 30; pass += 18; comp += 14; vision += 16; str -= 6; break;
            case InsideCentre:
                tackle += 14; hand += 12; str += 10; pass += 10; pace += 6; break;
            case OutsideCentre:
                pace += 14; accel += 12; hand += 12; agil += 10; tackle += 8; break;
            case LeftWing:
            case RightWing:
                pace += 22; accel += 20; agil += 14; hand += 8; tackle -= 6; break;
            case Fullback:
                kick += 20; goal += 18; posn += 16; hand += 12; pace += 12; comp += 8; break;
        }

        return new PlayerAttributes
        {
            Strength = Clamp(str), Pace = Clamp(pace), Acceleration = Clamp(accel), Stamina = Clamp(stam), Agility = Clamp(agil),
            Handling = Clamp(hand), Passing = Clamp(pass), Tackling = Clamp(tackle), Kicking = Clamp(kick),
            GoalKicking = Clamp(goal), Lineout = Clamp(lineout), Scrummaging = Clamp(scrum), Breakdown = Clamp(bd),
            DecisionMaking = Clamp(decision), Composure = Clamp(comp), Discipline = Clamp(disc), WorkRate = Clamp(work),
            Leadership = Clamp(lead), Positioning = Clamp(posn), Vision = Clamp(vision), Aggression = Clamp(aggr),
            Potential = Clamp(potential), Consistency = Clamp(consistency), InjuryProneness = Clamp(injury),
        };
    }

    private static int Clamp(int v) => Math.Clamp(v, 1, 99);

    private static double Gauss(Random rng, double mean, double sd)
    {
        double u1 = 1.0 - rng.NextDouble();
        double u2 = 1.0 - rng.NextDouble();
        return mean + sd * Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}
