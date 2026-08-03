using RugbyManager.Core.Competition;
using RugbyManager.Core.Finance;
using RugbyManager.Core.Match;
using RugbyManager.Core.Model;
using RugbyManager.Core.Training;

namespace RugbyManager.Web.Services;

/// <summary>Everything that happened to the player's club in one round, for the Play screen.</summary>
public sealed class RoundReport
{
    public int Round { get; init; }
    public required TrainingReport Training { get; init; }
    public List<Player> NewInjuries { get; init; } = new();
    public Fixture? MyFixture { get; init; }
    public MatchResult? MyResult { get; init; }
    public bool Home { get; init; }
    public required WeeklyLedger Ledger { get; init; }
    public List<Fixture> AllFixtures { get; init; } = new();
    public int TablePosition { get; init; }
    public int LeaguePoints { get; init; }
}
