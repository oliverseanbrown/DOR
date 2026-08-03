using RugbyManager.Core.Model;
using RugbyManager.Core.Training;
using RugbyManager.Core.Util;
using Xunit;
using static RugbyManager.Core.Model.Position;

namespace RugbyManager.Tests;

public class TrainingTests
{
    private static PlayerAttributes Attr(int value, int potential)
    {
        var a = PlayerAttributes.Uniform(value);
        a.Potential = potential;
        return a;
    }

    private static Team UniformClub(int value, int potential, int age)
    {
        var squad = new List<Player>();
        foreach (Position pos in Enum.GetValues<Position>())
            squad.Add(new Player
            {
                FirstName = "Test", LastName = pos.ToString(), Age = age,
                NaturalPosition = pos, Attributes = Attr(value, potential),
            });
        var team = new Team { Name = "Young FC", ShortName = "YNG", Squad = squad };
        team.SelectBestXV();
        return team;
    }

    [Fact]
    public void Fitness_DevelopsAttributes_ForYoungPlayersBelowPotential()
    {
        var club = UniformClub(value: 50, potential: 85, age: 20);
        var dice = new Dice(1);

        for (int week = 0; week < 40; week++)
            TrainingService.ApplyWeek(club, TrainingFocus.Fitness, dice);

        double avgStamina = club.Players.Average(p => p.Attributes.Stamina);
        Assert.True(avgStamina > 50, $"Expected stamina to grow from 50, got {avgStamina:0.0}.");
        // Handling was not trained, so it should be untouched.
        Assert.All(club.Players, p => Assert.Equal(50, p.Attributes.Handling));
    }

    [Fact]
    public void Development_IsCappedByPotential()
    {
        var club = UniformClub(value: 80, potential: 80, age: 19); // already at ceiling
        var dice = new Dice(2);

        for (int week = 0; week < 40; week++)
            TrainingService.ApplyWeek(club, TrainingFocus.Fitness, dice);

        Assert.All(club.Players, p => Assert.Equal(80, p.Attributes.Stamina));
    }

    [Fact]
    public void Rest_RecoversConditionWithoutDeveloping()
    {
        var club = UniformClub(value: 60, potential: 90, age: 20);
        foreach (var p in club.Players) p.Condition = 50;

        var report = TrainingService.ApplyWeek(club, TrainingFocus.Rest, new Dice(3));

        Assert.Equal(0, report.Improvements);
        Assert.All(club.Players, p => Assert.True(p.Condition > 50));
        Assert.All(club.Players, p => Assert.Equal(60, p.Attributes.Strength));
    }

    [Fact]
    public void MatchDepletion_ReducesCondition()
    {
        var club = UniformClub(value: 60, potential: 90, age: 25);
        TrainingService.DepleteAfterMatch(club);
        Assert.All(club.Players, p => Assert.True(p.Condition < 100));
    }

    [Fact]
    public void TrainingReport_ListsConcreteGains_AndPlayersAccumulateThem()
    {
        var club = UniformClub(value: 50, potential: 85, age: 20);
        var dice = new Dice(1);

        var report = TrainingService.ApplyWeek(club, TrainingFocus.Fitness, dice);

        Assert.Equal(report.Improvements, report.Gains.Count);
        if (report.Gains.Count > 0)
        {
            var g = report.Gains[0];
            Assert.Contains(g.Attribute, new[] { "Stamina", "Strength", "WorkRate" });
            var player = club.Squad.First(p => p.FullName == g.PlayerName);
            Assert.Equal(g.NewValue, GetAttr(player, g.Attribute));
            Assert.Equal(1, player.TrainingGains[g.Attribute]);
        }

        // Run more weeks and confirm gains keep accumulating on the player record.
        for (int week = 0; week < 20; week++)
            TrainingService.ApplyWeek(club, TrainingFocus.Fitness, dice);

        Assert.Contains(club.Squad, p => p.TrainingGains.Values.Sum() > 1);
    }

    private static int GetAttr(Player p, string name) => name switch
    {
        "Stamina" => p.Attributes.Stamina,
        "Strength" => p.Attributes.Strength,
        "WorkRate" => p.Attributes.WorkRate,
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };
}
