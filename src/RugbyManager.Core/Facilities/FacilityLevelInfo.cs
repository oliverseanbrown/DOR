namespace RugbyManager.Core.Facilities;

public interface IFacilityLevelInfo
{
    int Level { get; }
    string Name { get; }
    string Description { get; }
    int Cost { get; }
    int BuildWeeks { get; }
}

/// <summary>Lower is worse: below 1.0 means fewer injuries than the tuned baseline (1.0, level 0).</summary>
public sealed record PitchLevelInfo(
    int Level, string Name, string Description, int Cost, int BuildWeeks,
    double InjuryMultiplier) : IFacilityLevelInfo;

/// <summary>
/// <see cref="GateCap"/> is the most a home matchday can earn regardless of league-position
/// demand — the real bottleneck of a small ground. <see cref="MaxTier"/> is the highest
/// (numerically worst, i.e. lowest-division) tier still allowed to build this level.
/// </summary>
public sealed record StadiumLevelInfo(
    int Level, string Name, string Description, int Cost, int BuildWeeks,
    int Capacity, int GateCap, int MaxTier) : IFacilityLevelInfo;

public sealed record TrainingGroundLevelInfo(
    int Level, string Name, string Description, int Cost, int BuildWeeks,
    double DevelopmentMultiplier, double InjuryRecoveryMultiplier) : IFacilityLevelInfo;

public sealed record ClubhouseLevelInfo(
    int Level, string Name, string Description, int Cost, int BuildWeeks,
    int SponsorshipPerWeek, int Reputation) : IFacilityLevelInfo;
