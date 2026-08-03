using RugbyManager.Core.Model;

namespace RugbyManager.Core.Competition;

/// <summary>A division: a fixed set of clubs that play each other over a season.</summary>
public sealed class League
{
    public required string Name { get; init; }
    public required IReadOnlyList<Team> Teams { get; init; }

    public int TeamCount => Teams.Count;

    /// <summary>Create a fresh, unplayed season (double round-robin fixtures) for this league.</summary>
    public Season CreateSeason(int seed) => new(this, seed);
}
