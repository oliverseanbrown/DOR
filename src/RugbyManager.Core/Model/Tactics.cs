namespace RugbyManager.Core.Model;

/// <summary>Overall approach to how the team wants to play.</summary>
public enum PlayStyle
{
    /// <summary>Keep it tight, pick-and-go, dominate up front. Low risk, slow build.</summary>
    ForwardsOriented,
    Balanced,
    /// <summary>Move the ball wide, offload, back the outside backs. High reward, more errors.</summary>
    Expansive,
    /// <summary>Kick for territory and pressure, chase hard, squeeze the opposition.</summary>
    KickingGame,
}

/// <summary>How hard the team commits to contesting the breakdown.</summary>
public enum BreakdownFocus
{
    /// <summary>Prioritise keeping bodies in the line. Fewer turnovers won, fewer penalties.</summary>
    Conservative,
    Balanced,
    /// <summary>Contest everything. Wins more turnovers but concedes more penalties.</summary>
    Aggressive,
}

/// <summary>Defensive line speed.</summary>
public enum DefensiveLine
{
    /// <summary>Sit back, absorb, cover space. Safe but cedes territory.</summary>
    Drift,
    Standard,
    /// <summary>Fly up to shut down space. Forces errors but leaks big breaks if beaten.</summary>
    Rush,
}

/// <summary>How willing the team is to take the points when awarded a penalty.</summary>
public enum PenaltyPhilosophy
{
    /// <summary>Almost always take the three when in range.</summary>
    Pragmatic,
    Balanced,
    /// <summary>Back the pack — kick to the corner and go for the try.</summary>
    Ambitious,
}

/// <summary>
/// A team's tactical setup for a match. Read by the engine every phase. A record so the
/// manager UI can tweak one setting with a <c>with</c> expression.
/// </summary>
public sealed record Tactics
{
    public PlayStyle PlayStyle { get; init; } = PlayStyle.Balanced;
    public BreakdownFocus BreakdownFocus { get; init; } = BreakdownFocus.Balanced;
    public DefensiveLine DefensiveLine { get; init; } = DefensiveLine.Standard;
    public PenaltyPhilosophy PenaltyPhilosophy { get; init; } = PenaltyPhilosophy.Balanced;

    /// <summary>0-100 tendency to kick out of hand rather than run. Nudged by PlayStyle.</summary>
    public int KickingTendency { get; init; } = 40;
}
