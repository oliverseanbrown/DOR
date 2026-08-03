using RugbyManager.Core.Competition;
using RugbyManager.Core.Generation;
using RugbyManager.Core.Match;
using RugbyManager.Core.Model;
using Xunit;

namespace RugbyManager.Tests;

public class SeasonTests
{
    [Theory]
    [InlineData(10)]
    [InlineData(8)]
    [InlineData(5)] // odd count -> byes
    public void DoubleRoundRobin_HasCorrectShapeAndBalance(int n)
    {
        var league = LeagueGenerator.Generate("Test", n, seed: 1);
        var fixtures = FixtureGenerator.DoubleRoundRobin(league.Teams);

        // Every ordered pairing occurs exactly once (A-home-v-B and B-home-v-A).
        Assert.Equal(n * (n - 1), fixtures.Count);
        var pairings = fixtures.Select(f => (f.Home.ShortName, f.Away.ShortName)).ToHashSet();
        Assert.Equal(fixtures.Count, pairings.Count);

        // Each team has an equal number of home and away games.
        foreach (var team in league.Teams)
        {
            int home = fixtures.Count(f => ReferenceEquals(f.Home, team));
            int away = fixtures.Count(f => ReferenceEquals(f.Away, team));
            Assert.Equal(n - 1, home);
            Assert.Equal(n - 1, away);
        }

        // No team is scheduled twice in the same round.
        foreach (var round in fixtures.GroupBy(f => f.Round))
        {
            var teamsThisRound = round.SelectMany(f => new[] { f.Home, f.Away }).ToList();
            Assert.Equal(teamsThisRound.Count, teamsThisRound.Distinct().Count());
        }
    }

    [Fact]
    public void Table_AwardsTryAndLosingBonusPoints()
    {
        var league = LeagueGenerator.Generate("Test", 2, seed: 1);
        var (home, away) = (league.Teams[0], league.Teams[1]);

        // Home wins 31-24 with 5 tries; away scores 3 tries and loses by 7.
        var fixture = new Fixture
        {
            Index = 0,
            Round = 0,
            Home = home,
            Away = away,
            Result = Result(home, away, homeScore: 31, awayScore: 24, homeTries: 5, awayTries: 3),
        };

        var table = LeagueTable.Build(league, new[] { fixture });
        var homeRow = table.RowFor(home);
        var awayRow = table.RowFor(away);

        // Home: win (4) + try bonus (5 tries) = 5.
        Assert.Equal(1, homeRow.TryBonuses);
        Assert.Equal(0, homeRow.LosingBonuses);
        Assert.Equal(5, homeRow.LeaguePoints);

        // Away: lost by exactly 7 -> losing bonus only = 1.
        Assert.Equal(0, awayRow.TryBonuses);
        Assert.Equal(1, awayRow.LosingBonuses);
        Assert.Equal(1, awayRow.LeaguePoints);
    }

    [Fact]
    public void Table_DrawGivesTwoPointsEachAndNoLosingBonus()
    {
        var league = LeagueGenerator.Generate("Test", 2, seed: 1);
        var (home, away) = (league.Teams[0], league.Teams[1]);

        var fixture = new Fixture
        {
            Index = 0,
            Round = 0,
            Home = home,
            Away = away,
            Result = Result(home, away, homeScore: 15, awayScore: 15, homeTries: 2, awayTries: 2),
        };

        var table = LeagueTable.Build(league, new[] { fixture });
        Assert.Equal(2, table.RowFor(home).LeaguePoints);
        Assert.Equal(2, table.RowFor(away).LeaguePoints);
        Assert.Equal(0, table.RowFor(home).LosingBonuses);
        Assert.Equal(0, table.RowFor(away).LosingBonuses);
    }

    [Fact]
    public void PlayRoundExcept_LeavesTheExcludedFixtureUnplayed_ButFinishesTheRest()
    {
        var league = LeagueGenerator.Generate("Test", 8, seed: 3);
        var season = league.CreateSeason(seed: 99);
        season.BeginRound();

        var mine = season.FixturesInRound(0).First();
        var played = season.PlayRoundExcept(mine);

        Assert.False(mine.IsPlayed);
        Assert.All(played.Where(f => !ReferenceEquals(f, mine)), f => Assert.True(f.IsPlayed));

        // Recording it myself and completing the round should leave the season exactly where
        // the all-in-one PlayNextRound() would have.
        var result = new RugbyManager.Core.Match.MatchEngine(mine.Home, mine.Away, season.MatchSeedFor(mine)).Simulate();
        season.RecordFixtureResult(mine, result);
        season.CompleteRound();

        Assert.True(mine.IsPlayed);
        Assert.Equal(1, season.NextRound);
    }

    [Fact]
    public void PlayAll_PlaysEveryFixture()
    {
        var league = LeagueGenerator.Generate("Test", 8, seed: 3);
        var season = league.CreateSeason(seed: 99);
        season.PlayAll();

        Assert.True(season.IsComplete);
        Assert.All(season.Fixtures, f => Assert.True(f.IsPlayed));
        foreach (var row in season.BuildTable().Rows)
            Assert.Equal(2 * (league.TeamCount - 1), row.Played);
    }

    [Fact]
    public void Season_IsDeterministicForSameSeed()
    {
        // Independent leagues (fresh team instances) so season A's injuries don't bleed into B.
        var a = LeagueGenerator.Generate("Test", 10, seed: 7).CreateSeason(seed: 500);
        var b = LeagueGenerator.Generate("Test", 10, seed: 7).CreateSeason(seed: 500);
        a.PlayAll();
        b.PlayAll();

        var rowsA = a.BuildTable().Rows;
        var rowsB = b.BuildTable().Rows;
        Assert.Equal(rowsA.Count, rowsB.Count);
        for (int i = 0; i < rowsA.Count; i++)
        {
            Assert.Equal(rowsA[i].Team.ShortName, rowsB[i].Team.ShortName);
            Assert.Equal(rowsA[i].LeaguePoints, rowsB[i].LeaguePoints);
            Assert.Equal(rowsA[i].PointsDiff, rowsB[i].PointsDiff);
        }
    }

    [Fact]
    public void KnownSeed_ReproducesExactChampion()
    {
        // Golden-master reproducibility anchor: a fixed seed must always yield the same
        // season. Guards against process-randomised seeding (e.g. HashCode.Combine).
        // Expected values may legitimately change if the match engine is intentionally
        // re-tuned — update them deliberately if so.
        var league = LeagueGenerator.Generate("Golden", teamCount: 10, seed: 3, firstClubQuality: 64);
        var season = league.CreateSeason(seed: 3);
        season.PlayAll();

        var champion = season.BuildTable().Rows[0];
        Assert.Equal("HIL", champion.Team.ShortName);
        Assert.Equal(76, champion.LeaguePoints);
        Assert.Equal(364, champion.PointsDiff);
    }

    private static MatchResult Result(Team home, Team away, int homeScore, int awayScore, int homeTries, int awayTries)
        => new()
        {
            HomeName = home.Name,
            AwayName = away.Name,
            HomeShort = home.ShortName,
            AwayShort = away.ShortName,
            HomeScore = homeScore,
            AwayScore = awayScore,
            HomeStats = new MatchStats { Tries = homeTries },
            AwayStats = new MatchStats { Tries = awayTries },
            Events = Array.Empty<MatchEvent>(),
        };
}
