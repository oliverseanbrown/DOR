using RugbyManager.Core.Generation;
using RugbyManager.Core.Match;
using RugbyManager.Core.Model;
using Xunit;

namespace RugbyManager.Tests;

public class MatchEngineTests
{
    private static Team Team(string name, string sh, int quality, int seed) =>
        SquadGenerator.Generate(name, sh, quality, new Tactics(), seed);

    [Fact]
    public void SameSeed_ProducesIdenticalMatch()
    {
        var home = Team("Alpha", "ALP", 65, 1);
        var away = Team("Beta", "BET", 65, 2);

        var a = new MatchEngine(home, away, 12345).Simulate();
        var b = new MatchEngine(home, away, 12345).Simulate();

        Assert.Equal(a.HomeScore, b.HomeScore);
        Assert.Equal(a.AwayScore, b.AwayScore);
        Assert.Equal(a.Events.Count, b.Events.Count);
    }

    [Fact]
    public void DifferentSeeds_ProduceVariety()
    {
        var home = Team("Alpha", "ALP", 65, 1);
        var away = Team("Beta", "BET", 65, 2);

        var scores = new HashSet<(int, int)>();
        for (int seed = 0; seed < 30; seed++)
        {
            var r = new MatchEngine(home, away, seed).Simulate();
            scores.Add((r.HomeScore, r.AwayScore));
        }

        // 30 seeds should not all collapse to one scoreline.
        Assert.True(scores.Count > 10, $"Expected variety, got {scores.Count} distinct scorelines.");
    }

    [Fact]
    public void StrongerTeam_WinsMajorityOfMatches()
    {
        var strong = Team("Strong", "STR", 82, 1);
        var weak = Team("Weak", "WEK", 52, 2);

        int strongWins = 0, played = 60;
        for (int seed = 0; seed < played; seed++)
        {
            var r = new MatchEngine(strong, weak, seed).Simulate();
            if (r.HomeScore > r.AwayScore) strongWins++;
        }

        // A 30-point quality gap should win comfortably more often than not.
        Assert.True(strongWins >= 42, $"Strong team only won {strongWins}/{played}.");
    }

    [Fact]
    public void Scores_AreReasonableRugbyTotals()
    {
        var home = Team("Alpha", "ALP", 65, 1);
        var away = Team("Beta", "BET", 65, 2);

        for (int seed = 0; seed < 40; seed++)
        {
            var r = new MatchEngine(home, away, seed).Simulate();
            Assert.InRange(r.HomeScore, 0, 100);
            Assert.InRange(r.AwayScore, 0, 100);
            // Points only come from 5/2/3 combinations; 1, 2 and 4 are impossible standalone totals.
            Assert.False(r.HomeScore is 1 or 2 or 4, $"Impossible total {r.HomeScore}.");
            Assert.False(r.AwayScore is 1 or 2 or 4, $"Impossible total {r.AwayScore}.");
        }
    }

    [Fact]
    public void EveryEvent_HasSaneMinute()
    {
        var r = new MatchEngine(Team("A", "A", 60, 1), Team("B", "B", 60, 2), 7).Simulate();
        Assert.All(r.Events, e => Assert.InRange(e.Minute, 0, 90));
    }
}
