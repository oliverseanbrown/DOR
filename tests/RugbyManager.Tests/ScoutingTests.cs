using RugbyManager.Core.Generation;
using RugbyManager.Core.Model;
using RugbyManager.Core.Transfers;
using Xunit;

namespace RugbyManager.Tests;

public class ScoutingTests
{
    [Fact]
    public void OwnPlayers_AreFullyKnown()
    {
        var club = SquadGenerator.Generate("A", "A", 65, new Tactics(), 1);
        Assert.All(club.Squad, p => Assert.True(Scouting.FullyKnown(p)));
        var (lo, hi) = Scouting.OverallRange(club.Squad[0]);
        Assert.Equal(lo, hi); // exact
    }

    [Fact]
    public void Scouting_NarrowsTheRangeUntilConfirmed()
    {
        var player = SquadGenerator.CreatePlayer(Position.FlyHalf, 78, new Random(5));
        player.Scouted = 0;

        var (lo0, hi0) = Scouting.OverallRange(player);
        int width0 = hi0 - lo0;
        Assert.True(width0 > 0, "An unknown player should show a range, not an exact value.");
        Assert.InRange(PlayerRating.Overall(player), lo0, hi0); // truth lies in the range

        Scouting.Scout(player);
        var (lo1, hi1) = Scouting.OverallRange(player);
        Assert.True(hi1 - lo1 < width0, "One report should narrow the estimate.");

        Scouting.Scout(player);
        Scouting.Scout(player);
        Assert.True(Scouting.FullyKnown(player));
        var (lo2, hi2) = Scouting.OverallRange(player);
        Assert.Equal(lo2, hi2); // fully revealed
    }

    [Fact]
    public void MarketPlayers_StartUnscouted()
    {
        var market = MarketGenerator.Generate(30, seed: 3);
        Assert.Contains(market.Available, p => !Scouting.FullyKnown(p));
    }
}
