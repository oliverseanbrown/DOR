namespace RugbyManager.Core.Facilities;

/// <summary>The four independently-upgradeable areas of a club's home ground.</summary>
public enum FacilityArea { Pitch, Stadium, TrainingGround, Clubhouse }

/// <summary>An upgrade in progress: the target level for one area, counting down week by week.</summary>
public sealed class FacilityProject
{
    public required FacilityArea Area { get; init; }
    public required int TargetLevel { get; init; }
    public required int TotalWeeks { get; init; }
    public int WeeksRemaining { get; set; }
}
