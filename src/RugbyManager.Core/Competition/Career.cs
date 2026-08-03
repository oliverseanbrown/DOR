using RugbyManager.Core.Model;
using RugbyManager.Core.Training;
using RugbyManager.Core.Transfers;

namespace RugbyManager.Core.Competition;

/// <summary>
/// A save-able career: the season in progress, which club the player manages, and the
/// transfer market they can sign from. The aggregate root the game loop and persistence
/// both operate on.
/// </summary>
public sealed class Career
{
    public Season Season { get; }
    public int MyTeamIndex { get; }
    public TransferMarket Market { get; }

    /// <summary>Coaches available to hire.</summary>
    public List<Coach> CoachMarket { get; }

    /// <summary>The training focus applied to the player's club each week. Manager-set.</summary>
    public TrainingFocus Training { get; set; } = TrainingFocus.Fitness;

    /// <summary>1-based season counter for this career.</summary>
    public int SeasonNumber { get; set; } = 1;

    /// <summary>Division tier: 0 = top flight, higher = further down the pyramid.</summary>
    public int Tier { get; set; } = Pyramid.StartingTier;

    /// <summary>Recent headlines (newest last). Populated at season transitions and events.</summary>
    public List<string> News { get; init; } = new();

    public Career(Season season, int myTeamIndex, TransferMarket market, List<Coach>? coachMarket = null)
    {
        if (myTeamIndex < 0 || myTeamIndex >= season.League.TeamCount)
            throw new ArgumentOutOfRangeException(nameof(myTeamIndex));
        Season = season;
        MyTeamIndex = myTeamIndex;
        Market = market;
        CoachMarket = coachMarket ?? new List<Coach>();
    }

    public League League => Season.League;
    public Team MyClub => League.Teams[MyTeamIndex];
    public int Seed => Season.Seed;
}
