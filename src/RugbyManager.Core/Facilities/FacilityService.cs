using RugbyManager.Core.Model;

namespace RugbyManager.Core.Facilities;

/// <summary>
/// Reads and drives a club's facility levels: current-level lookups for the other systems
/// (finance, training, injuries) to apply, and the upgrade/construction workflow the Facilities
/// screen calls into.
/// </summary>
public static class FacilityService
{
    public static PitchLevelInfo PitchInfo(Team club) => FacilityCatalog.Pitch[club.PitchLevel];
    public static StadiumLevelInfo StadiumInfo(Team club) => FacilityCatalog.Stadium[club.StadiumLevel];
    public static TrainingGroundLevelInfo TrainingGroundInfo(Team club) => FacilityCatalog.TrainingGround[club.TrainingGroundLevel];
    public static ClubhouseLevelInfo ClubhouseInfo(Team club) => FacilityCatalog.Clubhouse[club.ClubhouseLevel];

    private static int CurrentLevel(Team club, FacilityArea area) => area switch
    {
        FacilityArea.Pitch => club.PitchLevel,
        FacilityArea.Stadium => club.StadiumLevel,
        FacilityArea.TrainingGround => club.TrainingGroundLevel,
        FacilityArea.Clubhouse => club.ClubhouseLevel,
        _ => throw new ArgumentOutOfRangeException(nameof(area)),
    };

    private static int MaxLevel(FacilityArea area) => area switch
    {
        FacilityArea.Pitch => FacilityCatalog.Pitch.Count - 1,
        FacilityArea.Stadium => FacilityCatalog.Stadium.Count - 1,
        FacilityArea.TrainingGround => FacilityCatalog.TrainingGround.Count - 1,
        FacilityArea.Clubhouse => FacilityCatalog.Clubhouse.Count - 1,
        _ => throw new ArgumentOutOfRangeException(nameof(area)),
    };

    private static IFacilityLevelInfo InfoAt(FacilityArea area, int level) => area switch
    {
        FacilityArea.Pitch => FacilityCatalog.Pitch[level],
        FacilityArea.Stadium => FacilityCatalog.Stadium[level],
        FacilityArea.TrainingGround => FacilityCatalog.TrainingGround[level],
        FacilityArea.Clubhouse => FacilityCatalog.Clubhouse[level],
        _ => throw new ArgumentOutOfRangeException(nameof(area)),
    };

    /// <summary>The current level's info for an area, regardless of type.</summary>
    public static IFacilityLevelInfo CurrentInfo(Team club, FacilityArea area) => InfoAt(area, CurrentLevel(club, area));

    /// <summary>The next level available for an area, or null if already at the top.</summary>
    public static IFacilityLevelInfo? NextLevel(Team club, FacilityArea area)
    {
        int current = CurrentLevel(club, area);
        return current >= MaxLevel(area) ? null : InfoAt(area, current + 1);
    }

    public static FacilityProject? ProjectFor(Team club, FacilityArea area)
        => club.FacilityProjects.FirstOrDefault(p => p.Area == area);

    /// <summary>
    /// Whether an upgrade to the next level can be started right now: not already maxed, no
    /// project already running for this area, the club's league tier (0 = top flight) is high
    /// enough to justify a stadium of that size, and there's enough money in the bank.
    /// </summary>
    public static bool CanUpgrade(Team club, FacilityArea area, int tier, out string blockedReason)
    {
        blockedReason = "";
        if (ProjectFor(club, area) is not null) { blockedReason = "Already under construction."; return false; }

        var next = NextLevel(club, area);
        if (next is null) { blockedReason = "Already at maximum level."; return false; }

        if (area == FacilityArea.Stadium && next is StadiumLevelInfo stadium && tier > stadium.MaxTier)
        {
            blockedReason = $"Needs promotion to {Competition.Pyramid.Name(stadium.MaxTier)} or higher first.";
            return false;
        }

        if (club.Money < next.Cost) { blockedReason = "Not enough funds."; return false; }
        return true;
    }

    /// <summary>Spend the money and begin construction. Returns false (with a reason) if
    /// <see cref="CanUpgrade"/> would have failed — callers don't need to check both.</summary>
    public static bool TryStartUpgrade(Team club, FacilityArea area, int tier, out string message)
    {
        if (!CanUpgrade(club, area, tier, out message)) return false;

        var next = NextLevel(club, area)!;
        club.Money -= next.Cost;
        club.FacilityProjects.Add(new FacilityProject
        {
            Area = area,
            TargetLevel = next.Level,
            TotalWeeks = Math.Max(1, next.BuildWeeks),
            WeeksRemaining = Math.Max(1, next.BuildWeeks),
        });
        message = $"Work has started on {next.Name}.";
        return true;
    }

    /// <summary>Advance every in-progress project by one week, completing (and applying) any
    /// that finish. Returns a headline per completion, for the news feed. Call once per
    /// matchday round, alongside training and finance.</summary>
    public static List<string> TickWeek(Team club)
    {
        var completed = new List<string>();
        foreach (var project in club.FacilityProjects.ToList())
        {
            project.WeeksRemaining--;
            if (project.WeeksRemaining > 0) continue;

            switch (project.Area)
            {
                case FacilityArea.Pitch: club.PitchLevel = project.TargetLevel; break;
                case FacilityArea.Stadium: club.StadiumLevel = project.TargetLevel; break;
                case FacilityArea.TrainingGround: club.TrainingGroundLevel = project.TargetLevel; break;
                case FacilityArea.Clubhouse: club.ClubhouseLevel = project.TargetLevel; break;
            }

            var info = InfoAt(project.Area, project.TargetLevel);
            completed.Add($"{AreaLabel(project.Area)} upgrade complete: {info.Name}.");
            club.FacilityProjects.Remove(project);
        }
        return completed;
    }

    public static string AreaLabel(FacilityArea area) => area switch
    {
        FacilityArea.Pitch => "Pitch",
        FacilityArea.Stadium => "Stadium",
        FacilityArea.TrainingGround => "Training Ground",
        FacilityArea.Clubhouse => "Clubhouse",
        _ => area.ToString(),
    };
}
