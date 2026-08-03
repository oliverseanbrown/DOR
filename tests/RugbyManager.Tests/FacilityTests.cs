using RugbyManager.Core.Facilities;
using RugbyManager.Core.Model;
using Xunit;
using static RugbyManager.Core.Model.Position;

namespace RugbyManager.Tests;

public class FacilityTests
{
    private static Team NewClub(int money = 150_000)
    {
        var squad = new List<Player>();
        foreach (Position pos in Enum.GetValues<Position>())
            squad.Add(new Player { FirstName = "Test", LastName = pos.ToString(), Age = 22, NaturalPosition = pos, Attributes = PlayerAttributes.Uniform(60) });
        var team = new Team { Name = "Rec Ground FC", ShortName = "REC", Squad = squad, Money = money };
        team.SelectBestXV();
        return team;
    }

    [Fact]
    public void NewClub_StartsAtGrassrootsLevelZeroEverywhere()
    {
        var club = NewClub();
        Assert.Equal(0, club.PitchLevel);
        Assert.Equal(0, club.StadiumLevel);
        Assert.Equal(0, club.TrainingGroundLevel);
        Assert.Equal(0, club.ClubhouseLevel);
        Assert.Empty(club.FacilityProjects);

        // Level 0 baselines must reproduce the pre-facilities defaults, so existing tuning is untouched.
        Assert.Equal(1.0, FacilityService.PitchInfo(club).InjuryMultiplier);
        Assert.Equal(1.0, FacilityService.TrainingGroundInfo(club).DevelopmentMultiplier);
        Assert.Equal(1.0, FacilityService.TrainingGroundInfo(club).InjuryRecoveryMultiplier);
        Assert.Equal(8_000, FacilityService.ClubhouseInfo(club).SponsorshipPerWeek);
    }

    [Fact]
    public void TryStartUpgrade_DeductsMoneyAndStartsAProject()
    {
        var club = NewClub(money: 10_000);
        var ok = FacilityService.TryStartUpgrade(club, FacilityArea.Pitch, tier: 4, out var message);

        Assert.True(ok);
        Assert.Equal(10_000 - FacilityCatalog.Pitch[1].Cost, club.Money);
        var project = FacilityService.ProjectFor(club, FacilityArea.Pitch);
        Assert.NotNull(project);
        Assert.Equal(1, project!.TargetLevel);
        Assert.Contains("Marked Home Pitch", message);
    }

    [Fact]
    public void TryStartUpgrade_FailsWithoutEnoughMoney()
    {
        var club = NewClub(money: 100);
        var ok = FacilityService.TryStartUpgrade(club, FacilityArea.Pitch, tier: 4, out var message);

        Assert.False(ok);
        Assert.Equal("Not enough funds.", message);
        Assert.Equal(100, club.Money);
        Assert.Null(FacilityService.ProjectFor(club, FacilityArea.Pitch));
    }

    [Fact]
    public void TryStartUpgrade_FailsWhileAlreadyUnderConstruction()
    {
        var club = NewClub(money: 100_000);
        Assert.True(FacilityService.TryStartUpgrade(club, FacilityArea.Pitch, tier: 4, out _));

        var ok = FacilityService.TryStartUpgrade(club, FacilityArea.Pitch, tier: 4, out var message);

        Assert.False(ok);
        Assert.Equal("Already under construction.", message);
    }

    [Fact]
    public void TryStartUpgrade_GatesBigStadiumsByLeagueTier()
    {
        var club = NewClub(money: 1_000_000);
        for (int i = 0; i < 5; i++)
        {
            Assert.True(FacilityService.TryStartUpgrade(club, FacilityArea.Stadium, tier: 0, out _));
            while (FacilityService.TickWeek(club).Count == 0) { }
        }
        Assert.Equal(5, club.StadiumLevel); // Regional Stadium — MaxTier 1

        var ok = FacilityService.TryStartUpgrade(club, FacilityArea.Stadium, tier: 4, out var message);
        Assert.False(ok);
        Assert.Contains("Needs promotion", message);
    }

    [Fact]
    public void TickWeek_CountsDownAndAppliesOnCompletion()
    {
        var club = NewClub(money: 100_000);
        FacilityService.TryStartUpgrade(club, FacilityArea.Clubhouse, tier: 4, out _); // 1 week to build

        var completed = FacilityService.TickWeek(club);

        Assert.Single(completed);
        Assert.Equal(1, club.ClubhouseLevel);
        Assert.Null(FacilityService.ProjectFor(club, FacilityArea.Clubhouse));
    }

    [Fact]
    public void TickWeek_DoesNotCompleteEarly()
    {
        var club = NewClub(money: 100_000);
        FacilityService.TryStartUpgrade(club, FacilityArea.TrainingGround, tier: 4, out _); // 2 weeks

        var afterOneWeek = FacilityService.TickWeek(club);
        Assert.Empty(afterOneWeek);
        Assert.Equal(0, club.TrainingGroundLevel);
        Assert.Equal(1, FacilityService.ProjectFor(club, FacilityArea.TrainingGround)!.WeeksRemaining);

        var afterTwoWeeks = FacilityService.TickWeek(club);
        Assert.Single(afterTwoWeeks);
        Assert.Equal(1, club.TrainingGroundLevel);
    }
}
