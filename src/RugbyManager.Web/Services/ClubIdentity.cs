namespace RugbyManager.Web.Services;

/// <summary>The manager's chosen club branding, captured on the New Career screen.</summary>
public sealed record ClubIdentity
{
    public required string Name { get; init; }
    public required string ShortName { get; init; }
    public string HomeGround { get; init; } = "";
    public string PrimaryColour { get; init; } = "#7c3aed";
    public string SecondaryColour { get; init; } = "#a855f7";
    public string? CrestImageDataUrl { get; init; }
}
