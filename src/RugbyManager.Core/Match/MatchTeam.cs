using RugbyManager.Core.Model;
using static RugbyManager.Core.Model.Position;

namespace RugbyManager.Core.Match;

/// <summary>
/// The live, in-match view of a team: tracks per-player energy and exposes derived,
/// position-weighted ratings that the engine contests against. Every rating already
/// reflects current fatigue and the team's tactical setup, so the engine just reads them.
/// </summary>
public sealed class MatchTeam
{
    // Position groupings the ratings are built from.
    private static readonly Position[] FrontRow = { LooseheadProp, Hooker, TightheadProp };
    private static readonly Position[] Locks = { Lock4, Lock5 };
    private static readonly Position[] BackRow = { BlindsideFlanker, OpensideFlanker, Number8 };
    private static readonly Position[] Jumpers = { Lock4, Lock5, BlindsideFlanker, Number8 };
    private static readonly Position[] BackThree = { LeftWing, RightWing, Fullback };
    private static readonly Position[] Centres = { InsideCentre, OutsideCentre };
    private static readonly Position[] Backs = { ScrumHalf, FlyHalf, LeftWing, InsideCentre, OutsideCentre, RightWing, Fullback };
    private static readonly Position[] BallHandlers = { ScrumHalf, FlyHalf, InsideCentre, OutsideCentre, Number8, Fullback };
    private static readonly Position[] AllXV = Enum.GetValues<Position>();

    // How much a phase drains energy. Tuned so a hard-working side finishes ~60/100.
    private const double DrainScale = 0.5;

    private readonly Dictionary<Position, double> _energy = new();

    public Team Team { get; }
    public MatchStats Stats { get; } = new();

    public MatchTeam(Team team)
    {
        Team = team;
        // Players start the match at their current condition, so tired/undercooked squads
        // begin below their best. A fully fresh player (condition 100) starts at full energy.
        foreach (var p in AllXV) _energy[p] = Math.Clamp(team.At(p).Condition, 1, 100);
    }

    public int Score => Stats.Tries * 5 + Stats.Conversions * 2 + Stats.PenaltyGoals * 3 + Stats.DropGoals * 3;

    private Tactics T => Team.Tactics;

    /// <summary>0.72 (spent) .. 1.0 (fresh) multiplier applied to every attribute reading.</summary>
    private double FatigueMult(Position p) => 0.72 + 0.28 * (_energy[p] / 100.0);

    /// <summary>A single player's attribute, degraded by their current fatigue.</summary>
    public double Effective(Position p, Func<PlayerAttributes, int> sel)
        => sel(Team.At(p).Attributes) * FatigueMult(p);

    private double Avg(IEnumerable<Position> ps, Func<PlayerAttributes, int> sel)
        => ps.Average(p => Effective(p, sel));

    /// <summary>Drain energy for a passage of play. Higher work rate & lower stamina drain faster.
    /// A fitness coach reduces how quickly the team tires.</summary>
    public void Tick(double minutes)
    {
        double fitnessRelief = 1.0 - Team.CoachRating(CoachSpecialty.Fitness) / 100.0 * 0.2;
        foreach (var p in AllXV)
        {
            var a = Team.At(p).Attributes;
            double drain = minutes * (0.5 + a.WorkRate / 100.0) * (1.2 - a.Stamina / 200.0) * DrainScale * fitnessRelief;
            _energy[p] = Math.Max(0, _energy[p] - drain);
        }
    }

    /// <summary>Matchday rating bump from the best coach in a specialty (up to ~+6).</summary>
    private double CoachBonus(CoachSpecialty specialty) => Team.CoachRating(specialty) * 0.06;

    /// <summary>Resets a position's fatigue to whoever is now there — used when a substitute
    /// comes on, so they start fresh rather than inheriting the player they replaced.</summary>
    public void ResetPositionEnergy(Position p) => _energy[p] = Math.Clamp(Team.At(p).Condition, 1, 100);

    // --- Derived ratings (all fatigue- and tactics-aware) ---

    /// <summary>Set-piece scrum power: front row technique with second-row shove.</summary>
    public double ScrumRating => 0.70 * Avg(FrontRow, a => a.Scrummaging) + 0.30 * Avg(Locks, a => a.Strength)
        + CoachBonus(CoachSpecialty.Scrum);

    /// <summary>Lineout: hooker's throw plus the jumping pod.</summary>
    public double LineoutRating => 0.45 * Effective(Hooker, a => a.Lineout) + 0.55 * Avg(Jumpers, a => a.Lineout)
        + CoachBonus(CoachSpecialty.Lineout);

    /// <summary>Attacking threat: handling, pace and the fly-half's game control.</summary>
    public double AttackRating
    {
        get
        {
            double handling = Avg(Backs, a => a.Handling);
            double pace = Avg(BackThree.Concat(Centres), a => a.Pace);
            double brain = 0.5 * Effective(FlyHalf, a => a.DecisionMaking) + 0.5 * Effective(FlyHalf, a => a.Vision);
            double r = 0.40 * handling + 0.30 * pace + 0.30 * brain;
            r += T.PlayStyle switch
            {
                PlayStyle.Expansive => 4,
                PlayStyle.ForwardsOriented => -2,
                PlayStyle.KickingGame => -3,
                _ => 0,
            };
            return r + CoachBonus(CoachSpecialty.Attack);
        }
    }

    /// <summary>Defensive solidity: line tackling plus back-row/centre reading.</summary>
    public double DefenceRating
    {
        get
        {
            double r = 0.60 * Avg(AllXV, a => a.Tackling) + 0.40 * Avg(BackRow.Concat(Centres), a => a.Positioning);
            r += T.DefensiveLine switch
            {
                DefensiveLine.Rush => 3,
                DefensiveLine.Drift => -2,
                _ => 0,
            };
            return r + CoachBonus(CoachSpecialty.Defence);
        }
    }

    /// <summary>Breakdown contest: back-row jackaling plus overall work rate.</summary>
    public double BreakdownRating
    {
        get
        {
            double r = 0.60 * Avg(BackRow, a => a.Breakdown) + 0.40 * Avg(AllXV, a => a.WorkRate);
            r += T.BreakdownFocus switch
            {
                BreakdownFocus.Aggressive => 5,
                BreakdownFocus.Conservative => -4,
                _ => 0,
            };
            return r + CoachBonus(CoachSpecialty.Breakdown);
        }
    }

    public double KickFromHand => 0.60 * Effective(FlyHalf, a => a.Kicking) + 0.40 * Effective(Fullback, a => a.Kicking)
        + CoachBonus(CoachSpecialty.Kicking);

    /// <summary>The nominated kicker's boot (holds up well under fatigue).</summary>
    public double GoalKickRating => Team.GoalKicker.Attributes.GoalKicking + CoachBonus(CoachSpecialty.Kicking);

    public double HandlingRating => Avg(BallHandlers, a => a.Handling);

    /// <summary>Higher = fewer penalties. Aggression at breakdown & a rush line cost discipline.</summary>
    public double DisciplineRating
    {
        get
        {
            double d = Avg(AllXV, a => a.Discipline);
            if (T.BreakdownFocus == BreakdownFocus.Aggressive) d -= 6;
            if (T.DefensiveLine == DefensiveLine.Rush) d -= 3;
            return d;
        }
    }

    public double Composure => Avg(AllXV, a => a.Composure);
}
