using RugbyManager.Core.Competition;
using RugbyManager.Core.Match;
using RugbyManager.Core.Model;
using RugbyManager.Core.Training;

namespace RugbyManager.Core.Persistence;

/// <summary>
/// The serialisable snapshot of a career. Deliberately compact: because the fixture
/// schedule is regenerated deterministically from team order, only match *results* and the
/// season's progress are stored — not the schedule, and not past-match commentary.
/// </summary>
public sealed record SaveData
{
    public int Version { get; init; } = 1;
    public required string LeagueName { get; init; }
    public int Seed { get; init; }
    public int MyTeamIndex { get; init; }
    public int NextRound { get; init; }
    public int SeasonNumber { get; init; } = 1;
    public int Tier { get; init; } = Pyramid.StartingTier;
    public List<string> News { get; init; } = new();
    public TrainingFocus Training { get; init; } = TrainingFocus.Fitness;
    public required List<TeamData> Teams { get; init; }
    public required List<ResultData> Results { get; init; }
    public List<PlayerData> Market { get; init; } = new();
    public List<Coach> CoachMarket { get; init; } = new();
}

public sealed record TeamData
{
    public required string Name { get; init; }
    public required string ShortName { get; init; }
    public int Money { get; init; }
    public required Tactics Tactics { get; init; }
    public required List<PlayerData> Players { get; init; }
    public List<Coach> Coaches { get; init; } = new();
    public List<SetPlay> Playbook { get; init; } = new();
    public Dictionary<string, int> PlayFamiliarity { get; init; } = new();
}

public sealed record PlayerData
{
    public Position Position { get; init; } // the player's natural position
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public int Age { get; init; }
    public int Condition { get; init; } = 100;
    public int InjuredWeeksRemaining { get; init; }
    public int Scouted { get; init; } = 100;
    public Dictionary<string, int> TrainingGains { get; init; } = new();
    public required PlayerAttributes Attributes { get; init; }
}

/// <summary>One played fixture's outcome, keyed back to its deterministic fixture index.</summary>
public sealed record ResultData
{
    public int FixtureIndex { get; init; }
    public int HomeScore { get; init; }
    public int AwayScore { get; init; }
    public required MatchStats HomeStats { get; init; }
    public required MatchStats AwayStats { get; init; }
}
