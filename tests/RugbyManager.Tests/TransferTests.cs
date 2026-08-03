using RugbyManager.Core.Generation;
using RugbyManager.Core.Model;
using RugbyManager.Core.Transfers;
using Xunit;

namespace RugbyManager.Tests;

public class TransferTests
{
    private static Team Club(int quality, int seed) =>
        SquadGenerator.Generate("My Club", "MYC", quality, new Tactics(), seed);

    [Fact]
    public void Sign_AddsToSquad_ReducesBudget_AndUpgradesXV()
    {
        var club = Club(60, 1);
        club.Money = 500_000;
        var target = SquadGenerator.CreatePlayer(Position.FlyHalf, 90, new Random(2));
        var market = new TransferMarket(new[] { target });

        int fee = TransferValue.Estimate(target);
        int beforeMoney = club.Money;
        int beforeSize = club.Squad.Count;

        var result = TransferService.Sign(club, market, target);

        Assert.True(result.Success);
        Assert.Contains(target, club.Squad);
        Assert.Equal(beforeSize + 1, club.Squad.Count);
        Assert.Equal(beforeMoney - fee, club.Money);
        Assert.DoesNotContain(target, market.Available);
        // A 90-rated fly-half should walk into the XV over a quality-60 squad.
        Assert.Same(target, club.At(Position.FlyHalf));
    }

    [Fact]
    public void Sign_FailsWhenTooExpensive()
    {
        var club = Club(60, 1);
        club.Money = 1_000;
        var target = SquadGenerator.CreatePlayer(Position.OpensideFlanker, 88, new Random(3));
        var market = new TransferMarket(new[] { target });

        var result = TransferService.Sign(club, market, target);

        Assert.False(result.Success);
        Assert.Contains(target, market.Available);
        Assert.DoesNotContain(target, club.Squad);
        Assert.Equal(1_000, club.Money);
    }

    [Fact]
    public void Sell_RemovesFromSquad_AndAddsBudget()
    {
        var club = Club(65, 1);
        club.Money = 0;
        var player = club.Squad[club.Squad.Count - 1]; // a reserve
        int expected = (int)(TransferValue.Estimate(player) * TransferService.SellBackFactor);
        var market = new TransferMarket(Array.Empty<Player>());

        var result = TransferService.Sell(club, market, player);

        Assert.True(result.Success);
        Assert.DoesNotContain(player, club.Squad);
        Assert.Equal(expected, club.Money);
        Assert.Contains(player, market.Available);
    }

    [Fact]
    public void Sell_FailsAtMinimumSquadSize()
    {
        var club = Club(65, 1);
        var market = new TransferMarket(Array.Empty<Player>());
        // Trim down to the minimum.
        while (club.Squad.Count > TransferService.MinSquad)
            TransferService.Sell(club, market, club.Squad[^1]);

        var result = TransferService.Sell(club, market, club.Squad[0]);
        Assert.False(result.Success);
        Assert.Equal(TransferService.MinSquad, club.Squad.Count);
    }

    [Fact]
    public void Value_RewardsAbilityAndPeakAge()
    {
        var reference = SquadGenerator.CreatePlayer(Position.Fullback, 80, new Random(1));
        var prime = new Player { FirstName = "A", LastName = "Prime", Age = 26, NaturalPosition = Position.Fullback, Attributes = reference.Attributes };
        var veteran = new Player { FirstName = "B", LastName = "Vet", Age = 34, NaturalPosition = Position.Fullback, Attributes = reference.Attributes };
        Assert.True(TransferValue.Estimate(prime) > TransferValue.Estimate(veteran));

        var weak = SquadGenerator.CreatePlayer(Position.Fullback, 55, new Random(9));
        var strong = SquadGenerator.CreatePlayer(Position.Fullback, 85, new Random(9));
        Assert.True(TransferValue.Estimate(strong) > TransferValue.Estimate(weak));
    }
}
