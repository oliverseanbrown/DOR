namespace RugbyManager.Core.Model;

/// <summary>The area a coach is expert in. Boosts matchday ratings and related training.</summary>
public enum CoachSpecialty
{
    Scrum,
    Lineout,
    Attack,
    Defence,
    Breakdown,
    Kicking,
    Fitness,
}

/// <summary>
/// A member of the coaching staff. Their ability in their specialty lifts the team's matchday
/// rating in that area and accelerates matching training — and (later) helps the squad execute
/// set plays they know.
/// </summary>
public sealed class Coach
{
    public required string Name { get; init; }
    public CoachSpecialty Specialty { get; init; }

    /// <summary>1-100.</summary>
    public int Ability { get; init; }

    /// <summary>Weekly wage, in pounds.</summary>
    public int Wage { get; init; }
}
