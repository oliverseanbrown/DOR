using RugbyManager.Core.Finance;
using RugbyManager.Core.Generation;
using RugbyManager.Core.Match;
using RugbyManager.Core.Model;
using Xunit;

namespace RugbyManager.Tests;

public class CoachTests
{
    [Fact]
    public void ScrumCoach_RaisesScrumRating()
    {
        var club = SquadGenerator.Generate("A", "A", 65, new Tactics(), 1);
        double before = new MatchTeam(club).ScrumRating;

        club.Coaches.Add(new Coach { Name = "Guru", Specialty = CoachSpecialty.Scrum, Ability = 90, Wage = 3000 });
        double after = new MatchTeam(club).ScrumRating;

        Assert.True(after > before, $"Scrum rating should rise with a scrum coach ({before:0.0} -> {after:0.0}).");
    }

    [Fact]
    public void CoachRating_ReturnsBestOfSpecialty()
    {
        var club = SquadGenerator.Generate("A", "A", 65, new Tactics(), 1);
        club.Coaches.Add(new Coach { Name = "One", Specialty = CoachSpecialty.Attack, Ability = 60, Wage = 1 });
        club.Coaches.Add(new Coach { Name = "Two", Specialty = CoachSpecialty.Attack, Ability = 85, Wage = 1 });

        Assert.Equal(85, club.CoachRating(CoachSpecialty.Attack));
        Assert.Equal(0, club.CoachRating(CoachSpecialty.Kicking));
    }

    [Fact]
    public void CoachWages_CountTowardTheWageBill()
    {
        var club = SquadGenerator.Generate("A", "A", 65, new Tactics(), 1);
        int before = FinanceService.WeeklyWageBill(club);

        club.Coaches.Add(new Coach { Name = "Guru", Specialty = CoachSpecialty.Fitness, Ability = 80, Wage = 2500 });

        Assert.Equal(before + 2500, FinanceService.WeeklyWageBill(club));
    }
}
