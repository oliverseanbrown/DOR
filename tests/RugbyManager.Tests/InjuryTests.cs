using RugbyManager.Core.Generation;
using RugbyManager.Core.Injuries;
using RugbyManager.Core.Model;
using RugbyManager.Core.Util;
using Xunit;

namespace RugbyManager.Tests;

public class InjuryTests
{
    [Fact]
    public void HealWeek_ReducesInjuryByOne()
    {
        var club = SquadGenerator.Generate("A", "A", 65, new Tactics(), 1);
        var p = club.Squad[0];
        p.InjuredWeeksRemaining = 3;

        InjuryService.HealWeek(club);
        Assert.Equal(2, p.InjuredWeeksRemaining);

        p.InjuredWeeksRemaining = 0;
        InjuryService.HealWeek(club);
        Assert.Equal(0, p.InjuredWeeksRemaining); // never goes negative
    }

    [Fact]
    public void InjuredPlayers_AreLeftOutOfTheXV()
    {
        var club = SquadGenerator.Generate("A", "A", 65, new Tactics(), 1);
        club.SelectBestXV();
        var starter = club.At(Position.FlyHalf);

        starter.InjuredWeeksRemaining = 4;
        club.SelectBestXV();

        Assert.DoesNotContain(starter, club.Players);
        Assert.Equal(15, club.Players.Count());       // still a full XV
        Assert.All(club.Players, p => Assert.True(p.IsFit));
    }

    [Fact]
    public void HighProneness_ProducesMoreInjuriesThanLow()
    {
        int Count(int proneness)
        {
            int total = 0;
            for (int seed = 0; seed < 40; seed++)
            {
                var club = SquadGenerator.Generate("A", "A", 65, new Tactics(), seed);
                foreach (var p in club.Squad) p.Attributes.InjuryProneness = proneness;
                club.SelectBestXV();
                total += InjuryService.RollMatchInjuries(club, new Dice(seed)).Count;
            }
            return total;
        }

        Assert.True(Count(95) > Count(5), "Injury-prone squads should pick up more injuries.");
    }
}
