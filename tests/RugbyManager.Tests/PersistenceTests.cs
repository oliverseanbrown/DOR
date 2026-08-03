using RugbyManager.Core.Competition;
using RugbyManager.Core.Generation;
using RugbyManager.Core.Persistence;
using Xunit;

namespace RugbyManager.Tests;

public class PersistenceTests
{
    [Fact]
    public void SaveThenLoad_PreservesTableAndProgress()
    {
        var league = LeagueGenerator.Generate("Save Test", 10, seed: 11, firstClubQuality: 64);
        league.Teams[0].Money = 120_000;
        var career = new Career(league.CreateSeason(seed: 11), myTeamIndex: 0, MarketGenerator.Generate(20, 11));

        // Play part of the season and change a tactic so state is non-trivial.
        for (int i = 0; i < 6; i++) career.Season.PlayNextRound();

        string path = Path.Combine(Path.GetTempPath(), $"rm_save_{Guid.NewGuid():N}.json");
        try
        {
            CareerStore.Save(career, path);
            var loaded = CareerStore.Load(path);

            Assert.Equal(career.Season.NextRound, loaded.Season.NextRound);
            Assert.Equal(career.MyTeamIndex, loaded.MyTeamIndex);
            Assert.Equal(career.MyClub.Name, loaded.MyClub.Name);
            Assert.Equal(career.MyClub.Money, loaded.MyClub.Money);
            Assert.Equal(career.Market.Available.Count, loaded.Market.Available.Count);

            var before = career.Season.BuildTable().Rows;
            var after = loaded.Season.BuildTable().Rows;
            Assert.Equal(before.Count, after.Count);
            for (int i = 0; i < before.Count; i++)
            {
                Assert.Equal(before[i].Team.ShortName, after[i].Team.ShortName);
                Assert.Equal(before[i].LeaguePoints, after[i].LeaguePoints);
                Assert.Equal(before[i].PointsDiff, after[i].PointsDiff);
                Assert.Equal(before[i].TriesFor, after[i].TriesFor);
            }
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void ContinuingAfterLoad_IsDeterministic()
    {
        var league = LeagueGenerator.Generate("Save Test", 8, seed: 21, firstClubQuality: 64);
        var original = new Career(league.CreateSeason(seed: 21), myTeamIndex: 0, MarketGenerator.Generate(20, 21));
        for (int i = 0; i < 4; i++) original.Season.PlayNextRound();

        string path = Path.Combine(Path.GetTempPath(), $"rm_save_{Guid.NewGuid():N}.json");
        try
        {
            CareerStore.Save(original, path);
            var loaded = CareerStore.Load(path);

            // Play the rest on both; final tables must be identical.
            original.Season.PlayAll();
            loaded.Season.PlayAll();

            var a = original.Season.BuildTable().Rows;
            var b = loaded.Season.BuildTable().Rows;
            for (int i = 0; i < a.Count; i++)
            {
                Assert.Equal(a[i].Team.ShortName, b[i].Team.ShortName);
                Assert.Equal(a[i].LeaguePoints, b[i].LeaguePoints);
                Assert.Equal(a[i].PointsFor, b[i].PointsFor);
            }
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void PlaybookAndFamiliarity_RoundTrip()
    {
        var league = LeagueGenerator.Generate("Save Test", 6, seed: 8, firstClubQuality: 64);
        var career = new Career(league.CreateSeason(seed: 8), myTeamIndex: 0, MarketGenerator.Generate(20, 8));
        var play = Core.Model.SetPlayLibrary.All[0];
        career.MyClub.Playbook.Add(play);
        career.MyClub.BumpFamiliarity(play.Name, 37);

        string path = Path.Combine(Path.GetTempPath(), $"rm_save_{Guid.NewGuid():N}.json");
        try
        {
            CareerStore.Save(career, path);
            var loaded = CareerStore.Load(path);

            Assert.Contains(loaded.MyClub.Playbook, p => p.Name == play.Name);
            Assert.Equal(career.MyClub.GetFamiliarity(play.Name), loaded.MyClub.GetFamiliarity(play.Name));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void TrainingGains_RoundTrip()
    {
        var league = LeagueGenerator.Generate("Save Test", 6, seed: 9, firstClubQuality: 64);
        var career = new Career(league.CreateSeason(seed: 9), myTeamIndex: 0, MarketGenerator.Generate(20, 9));
        var player = career.MyClub.Squad[0];
        player.TrainingGains["Strength"] = 4;
        player.TrainingGains["Passing"] = 2;

        string path = Path.Combine(Path.GetTempPath(), $"rm_save_{Guid.NewGuid():N}.json");
        try
        {
            CareerStore.Save(career, path);
            var loaded = CareerStore.Load(path);
            var loadedPlayer = loaded.MyClub.Squad.First(p => p.FullName == player.FullName);

            Assert.Equal(4, loadedPlayer.TrainingGains["Strength"]);
            Assert.Equal(2, loadedPlayer.TrainingGains["Passing"]);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void SavedPlayers_RoundTripAttributes()
    {
        var league = LeagueGenerator.Generate("Save Test", 6, seed: 5, firstClubQuality: 64);
        var career = new Career(league.CreateSeason(seed: 5), myTeamIndex: 0, MarketGenerator.Generate(20, 5));

        string path = Path.Combine(Path.GetTempPath(), $"rm_save_{Guid.NewGuid():N}.json");
        try
        {
            CareerStore.Save(career, path);
            var loaded = CareerStore.Load(path);

            foreach (Core.Model.Position pos in Enum.GetValues<Core.Model.Position>())
            {
                var before = career.MyClub.At(pos);
                var after = loaded.MyClub.At(pos);
                Assert.Equal(before.FullName, after.FullName);
                Assert.Equal(before.Attributes.Scrummaging, after.Attributes.Scrummaging);
                Assert.Equal(before.Attributes.Pace, after.Attributes.Pace);
                Assert.Equal(before.Attributes.GoalKicking, after.Attributes.GoalKicking);
            }
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
