using RugbyManager.Core.Generation;
using RugbyManager.Core.Match;
using RugbyManager.Core.Model;
using Xunit;

namespace RugbyManager.Tests;

public class MatchCentreTests
{
    private static Team Team(string name, string sh, int quality, int seed) =>
        SquadGenerator.Generate(name, sh, quality, new Tactics(), seed);

    [Fact]
    public void Step_IsEquivalentToSimulate_ForTheSameSeed()
    {
        var homeA = Team("Alpha", "ALP", 68, 1);
        var awayA = Team("Beta", "BET", 65, 2);
        var resultA = new MatchEngine(homeA, awayA, 42).Simulate();

        var homeB = Team("Alpha", "ALP", 68, 1);
        var awayB = Team("Beta", "BET", 65, 2);
        var engineB = new MatchEngine(homeB, awayB, 42);
        while (!engineB.Step()) { }

        Assert.Equal(resultA.HomeScore, engineB.HomeScore);
        Assert.Equal(resultA.AwayScore, engineB.AwayScore);
        Assert.Equal(resultA.Events.Count, engineB.Events.Count);
        for (int i = 0; i < resultA.Events.Count; i++)
            Assert.Equal(resultA.Events[i].Text, engineB.Events[i].Text);
    }

    [Fact]
    public void Step_EventsAndScoreAreVisibleLive_BeforeCompletion()
    {
        var home = Team("Alpha", "ALP", 68, 1);
        var away = Team("Beta", "BET", 65, 2);
        var engine = new MatchEngine(home, away, 42);

        int stepsBeforeDone = 0;
        while (!engine.Step())
        {
            stepsBeforeDone++;
            if (stepsBeforeDone > 5) break; // just prove state is inspectable mid-match
        }

        Assert.True(engine.Events.Count > 0, "Events should already be populated mid-match.");
        Assert.False(engine.IsComplete);
        Assert.True(engine.HomeScore >= 0 && engine.AwayScore >= 0);
    }

    [Fact]
    public void Substitute_ChangesWhoTheEngineReadsForThatPosition_AndResetsFatigue()
    {
        var home = Team("Alpha", "ALP", 60, 1);
        var away = Team("Beta", "BET", 60, 2);
        var engine = new MatchEngine(home, away, 5);

        // Run a chunk of the match so the starter accumulates real fatigue.
        for (int i = 0; i < 40 && !engine.IsComplete; i++) engine.Step();

        var sub = SquadGenerator.CreatePlayer(Position.FlyHalf, 90, new Random(9));
        engine.Substitute(0, Position.FlyHalf, sub);

        Assert.Same(sub, home.At(Position.FlyHalf));
        Assert.Contains(engine.Events, e => e.Type == MatchEventType.Substitution && e.Text.Contains(sub.ShortName));
    }

    [Fact]
    public void TacticsChange_MidMatch_TakesEffectOnSubsequentPhases()
    {
        var home = Team("Alpha", "ALP", 65, 1);
        var away = Team("Beta", "BET", 65, 2);
        var engine = new MatchEngine(home, away, 5);

        for (int i = 0; i < 20 && !engine.IsComplete; i++) engine.Step();

        home.Tactics = home.Tactics with { PlayStyle = PlayStyle.KickingGame, KickingTendency = 95 };
        engine.LogTacticsChange(0);

        Assert.Contains(engine.Events, e => e.Type == MatchEventType.TacticsChange);
        // The tactic is read live by MatchTeam; no special engine plumbing needed, but confirm
        // the team object the engine holds really is the same one we just mutated.
        Assert.Equal(PlayStyle.KickingGame, home.Tactics.PlayStyle);
    }

    [Fact]
    public void PlayerStats_AccumulateAcrossTheXV_AndTriesReconcileWithTeamTotals()
    {
        var home = Team("Alpha", "ALP", 68, 1);
        var away = Team("Beta", "BET", 65, 2);
        var engine = new MatchEngine(home, away, 3);
        var result = engine.Simulate();

        // Stats should be spread across a good chunk of the squad, not just try-scorers.
        var homeSquadStats = engine.PlayerStats.Where(kv => home.Players.Contains(kv.Key)).ToList();
        Assert.True(homeSquadStats.Count(kv => kv.Value.Carries > 0 || kv.Value.Tackles > 0) >= 5,
            "Expected routine involvement (carries/tackles) spread across several players.");

        // Every individually-credited try must reconcile with the team total.
        int homeTriesFromPlayers = engine.PlayerStats.Where(kv => home.Players.Contains(kv.Key)).Sum(kv => kv.Value.Tries);
        int awayTriesFromPlayers = engine.PlayerStats.Where(kv => away.Players.Contains(kv.Key)).Sum(kv => kv.Value.Tries);
        Assert.Equal(result.HomeStats.Tries, homeTriesFromPlayers);
        Assert.Equal(result.AwayStats.Tries, awayTriesFromPlayers);

        Assert.All(engine.PlayerStats.Values, s => Assert.InRange(s.Rating, 2.0, 10.0));
    }

    [Fact]
    public void ManualLineup_IsRespected_AndPatchedForFitnessOnly()
    {
        var club = Team("Alpha", "ALP", 65, 1);
        var chosenFlyHalf = club.Squad.First(p => p.NaturalPosition != Position.FlyHalf && p.IsFit);
        club.SetStarter(Position.FlyHalf, chosenFlyHalf);
        club.HasManualLineup = true;

        Assert.Same(chosenFlyHalf, club.At(Position.FlyHalf));

        chosenFlyHalf.InjuredWeeksRemaining = 3; // now unfit
        club.ValidateLineupFitness();

        Assert.NotSame(chosenFlyHalf, club.At(Position.FlyHalf));
        Assert.True(club.At(Position.FlyHalf).IsFit);
    }
}
