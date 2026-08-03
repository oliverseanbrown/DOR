using RugbyManager.Core.Generation;
using RugbyManager.Core.Match;
using RugbyManager.Core.Model;
using Xunit;

namespace RugbyManager.Tests;

public class BreakawayTests
{
    private static Team Team(string name, string sh, int quality, int seed) =>
        SquadGenerator.Generate(name, sh, quality, new Tactics(), seed);

    [Fact]
    public void Carry_IsAlwaysFollowedByAMissedTackleFromTheOtherTeam()
    {
        var home = Team("Alpha", "ALP", 70, 1);
        var away = Team("Beta", "BET", 65, 2);
        int found = 0;

        for (int seed = 0; seed < 300 && found < 20; seed++)
        {
            var events = new MatchEngine(home, away, seed).Simulate().Events;
            for (int i = 0; i < events.Count - 1; i++)
            {
                if (events[i].Type != MatchEventType.Carry) continue;
                found++;
                var carry = events[i];
                var missed = events[i + 1];

                Assert.Equal(MatchEventType.MissedTackle, missed.Type);
                Assert.Equal(Drama.Highlight, carry.Drama);
                Assert.Equal(Drama.Highlight, missed.Drama);
                Assert.NotEqual(carry.Team, missed.Team); // carrier's side vs. the side that missed
            }
        }

        Assert.True(found > 0, "No breakaway Carry events occurred across 300 seeds.");
    }

    [Fact]
    public void LastDefenderContest_CanGoEitherWay()
    {
        // The contest beyond the missed tackle must be genuine: across enough breakaways we
        // should see the attack score AND the defence win it back outright or hold them up —
        // never a guaranteed try just because a break happened.
        var home = Team("Alpha", "ALP", 65, 1);
        var away = Team("Beta", "BET", 65, 2);

        bool sawTry = false, sawDefenceWinsBallBack = false, sawHeldUpShort = false;

        for (int seed = 0; seed < 400; seed++)
        {
            var events = new MatchEngine(home, away, seed).Simulate().Events;
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i].Type != MatchEventType.Carry) continue;
                var attackingTeam = events[i].Team;

                for (int j = i + 2; j < Math.Min(i + 4, events.Count); j++)
                {
                    if (events[j].Type == MatchEventType.Try) sawTry = true;
                    if (events[j].Type is MatchEventType.Turnover or MatchEventType.KnockOn) sawDefenceWinsBallBack = true;
                    if (events[j].Type == MatchEventType.LineBreak && events[j].Team != attackingTeam) sawHeldUpShort = true;
                }
            }
            if (sawTry && (sawDefenceWinsBallBack || sawHeldUpShort)) break;
        }

        Assert.True(sawTry, "Breakaways should sometimes result in a try.");
        Assert.True(sawDefenceWinsBallBack || sawHeldUpShort,
            "The last-defender contest should sometimes fail for the attack (turnover, knock-on, or held up short).");
    }

    [Fact]
    public void RoutineEvents_AreNotTaggedHighlight()
    {
        var home = Team("Alpha", "ALP", 65, 1);
        var away = Team("Beta", "BET", 65, 2);
        var events = new MatchEngine(home, away, 7).Simulate().Events;

        Assert.Contains(events, e => e.Type == MatchEventType.KickToTouch && e.Drama == Drama.Routine);
        Assert.Contains(events, e => e.Type is MatchEventType.Try && e.Drama == Drama.Highlight);
    }
}
