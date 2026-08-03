namespace RugbyManager.Core.Training;

/// <summary>What the squad works on during a training week. Each focus develops a set of
/// attributes and restores a different amount of condition (rest restores the most).</summary>
public enum TrainingFocus
{
    /// <summary>No development, but the best recovery of condition.</summary>
    Rest,
    /// <summary>Stamina, strength, work rate. Intense — poor condition recovery.</summary>
    Fitness,
    /// <summary>Handling, passing, vision.</summary>
    Handling,
    /// <summary>Scrummaging & lineout.</summary>
    SetPiece,
    /// <summary>Tackling, positioning, breakdown.</summary>
    Defence,
    /// <summary>Kicking out of hand & goal kicking.</summary>
    Kicking,
}
