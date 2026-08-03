using RugbyManager.Core.Competition;
using RugbyManager.Core.Generation;
using RugbyManager.Core.Transfers;
using Xunit;

namespace RugbyManager.Tests;

public class SeasonTransitionTests
{
    private static Career NewCareer(int seed, int quality)
    {
        var league = LeagueGenerator.Generate(Pyramid.Name(Pyramid.StartingTier), 10, seed,
            firstClubQuality: quality, baseQuality: Pyramid.BaseQuality(Pyramid.StartingTier));
        return new Career(league.CreateSeason(seed), 0, new TransferMarket(Array.Empty<Core.Model.Player>()))
        {
            SeasonNumber = 1,
            Tier = Pyramid.StartingTier,
        };
    }

    [Fact]
    public void Promotion_MovesUpATier_AndBumpsSeasonNumber()
    {
        // A far stronger club than its division should finish top 2 and go up.
        var career = NewCareer(seed: 4, quality: 90);
        career.Season.PlayAll();

        int position = career.Season.BuildTable().RowFor(career.MyClub).Position;
        var (next, result) = SeasonTransition.Advance(career);

        Assert.InRange(position, 1, Pyramid.PromotionSpots);
        Assert.Equal(SeasonResult.Promoted, result);
        Assert.Equal(Pyramid.StartingTier - 1, next.Tier);
        Assert.Equal(2, next.SeasonNumber);
    }

    [Fact]
    public void Relegation_MovesDownATier()
    {
        var career = NewCareer(seed: 8, quality: 40); // hopelessly outmatched
        career.Season.PlayAll();

        int position = career.Season.BuildTable().RowFor(career.MyClub).Position;
        var (next, result) = SeasonTransition.Advance(career);

        Assert.True(position > 10 - Pyramid.RelegationSpots);
        Assert.Equal(SeasonResult.Relegated, result);
        Assert.Equal(Pyramid.StartingTier + 1, next.Tier);
    }

    [Fact]
    public void NewSeason_AgesTheSquad_KeepsTheClub_AndIsFreshAndFit()
    {
        var career = NewCareer(seed: 4, quality: 68);
        var club = career.MyClub;
        var (name, agesBefore) = (club.Name, club.Squad.Select(p => (p.FirstName + p.LastName, p.Age)).ToDictionary(x => x.Item1, x => x.Item2));
        career.Season.PlayAll();

        var (next, _) = SeasonTransition.Advance(career);

        Assert.Equal(name, next.MyClub.Name);                 // same club persists
        Assert.False(next.Season.IsComplete);                 // fresh fixtures
        Assert.Equal(0, next.Season.NextRound);
        Assert.All(next.MyClub.Squad, p => Assert.Equal(100, p.Condition)); // pre-season fresh
        Assert.All(next.MyClub.Squad, p => Assert.True(p.IsFit));
        // Surviving players are a year older.
        foreach (var p in next.MyClub.Squad)
            if (agesBefore.TryGetValue(p.FirstName + p.LastName, out int before))
                Assert.True(p.Age >= before, "players should not get younger");
        Assert.NotEmpty(next.News);
    }
}
