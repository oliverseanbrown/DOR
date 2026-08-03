namespace RugbyManager.Core.Model;

/// <summary>
/// A persistent player entity. Match-time state (fatigue, minutes, event tallies)
/// lives on a separate wrapper in the match layer so this stays a clean domain object.
/// </summary>
public sealed class Player
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }

    /// <summary>Settable so players age one year between seasons.</summary>
    public int Age { get; set; }

    /// <summary>The position the player is naturally best in.</summary>
    public Position NaturalPosition { get; init; }

    public required PlayerAttributes Attributes { get; init; }

    /// <summary>
    /// Match sharpness / freshness, 0-100. Depleted by playing, restored by rest and light
    /// training. Feeds the player's starting energy in a match. Defaults to fully fresh.
    /// </summary>
    public int Condition { get; set; } = 100;

    /// <summary>Weeks (rounds) until the player is fit again. 0 = available.</summary>
    public int InjuredWeeksRemaining { get; set; }

    public bool IsFit => InjuredWeeksRemaining == 0;

    /// <summary>
    /// Cumulative attribute points gained from training since this player was generated, by
    /// attribute name. Purely a display record — the actual growth already lives in
    /// <see cref="Attributes"/>; this just lets the UI show "how much of that came from training".
    /// </summary>
    public Dictionary<string, int> TrainingGains { get; init; } = new();

    /// <summary>
    /// How well this player is known to the manager, 0-100. Your own players are fully known
    /// (100); market players start largely unknown and are revealed by scouting.
    /// </summary>
    public int Scouted { get; set; } = 100;

    public string FullName => $"{FirstName} {LastName}";

    /// <summary>Surname with initial, e.g. "J. Lomu" — the default commentary style.</summary>
    public string ShortName => $"{FirstName[..1]}. {LastName}";
}
