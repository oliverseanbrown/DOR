using RugbyManager.Core.Match;
using RugbyManager.Core.Model;

namespace RugbyManager.Core.Competition;

/// <summary>
/// One scheduled match in a season. <see cref="Result"/> is null until the fixture is played.
/// <see cref="Index"/> is a stable sequence number used to derive a deterministic match seed.
/// </summary>
public sealed class Fixture
{
    public required int Index { get; init; }
    public required int Round { get; init; } // 0-based internally; displayed as Round+1
    public required Team Home { get; init; }
    public required Team Away { get; init; }

    public MatchResult? Result { get; set; }

    public bool IsPlayed => Result is not null;
}
