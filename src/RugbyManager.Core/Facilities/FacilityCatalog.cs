using RugbyManager.Core.Competition;

namespace RugbyManager.Core.Facilities;

/// <summary>
/// The fixed level tables for every facility area. Every level-0 entry is a literal grassroots
/// starting point — a shared council pitch, a rope for a stand, a training session on the match
/// pitch, no clubhouse at all — and every effect at level 0 reproduces the game's original,
/// pre-facilities baseline so a brand-new club's numbers don't move until the manager invests.
/// </summary>
public static class FacilityCatalog
{
    public static readonly IReadOnlyList<PitchLevelInfo> Pitch = new[]
    {
        new PitchLevelInfo(0, "Public Rec Ground", "A shared council pitch — rutted, unmarked, and shared with the local under-12s on a Sunday.", 0, 0, 1.00),
        new PitchLevelInfo(1, "Marked Home Pitch", "Your own patch of grass, mown and lined every week.", 1_500, 1, 0.92),
        new PitchLevelInfo(2, "Drained & Rolled", "Proper drainage means far fewer ankle-deep afternoons in November.", 6_000, 2, 0.82),
        new PitchLevelInfo(3, "Maintained Turf", "A part-time groundsman keeps this surface true all season.", 18_000, 3, 0.72),
        new PitchLevelInfo(4, "Semi-Pro Surface", "Reinforced turf that shrugs off a wet winter.", 45_000, 4, 0.62),
        new PitchLevelInfo(5, "Elite Hybrid Turf", "Stitched hybrid grass — the surface top-flight sides train and play on.", 120_000, 6, 0.50),
    };

    public static readonly IReadOnlyList<StadiumLevelInfo> Stadium = new[]
    {
        new StadiumLevelInfo(0, "Roped-Off Field", "Spectators stand behind a rope. Bring your own deckchair.", 0, 0, 300, 9_000, Pyramid.BottomTier),
        new StadiumLevelInfo(1, "Portable Stand", "A single bank of scaffold seating, wheeled in on matchday.", 4_000, 2, 1_200, 14_000, Pyramid.BottomTier),
        new StadiumLevelInfo(2, "Covered Stand", "One proper stand, roofed against the rain, with a tea hatch underneath.", 15_000, 3, 3_000, 22_000, 5),
        new StadiumLevelInfo(3, "Two-Stand Ground", "Home and away covered — a real little ground now.", 40_000, 4, 6_000, 34_000, 3),
        new StadiumLevelInfo(4, "All-Seater Ground", "Four sides, all seated, and a scoreboard that actually works.", 90_000, 5, 12_000, 55_000, 2),
        new StadiumLevelInfo(5, "Regional Stadium", "The kind of ground away fans still talk about years later.", 220_000, 7, 25_000, 90_000, 1),
        new StadiumLevelInfo(6, "National Arena", "Floodlit and all-seater, built to host a final.", 500_000, 10, 45_000, 150_000, 0),
    };

    public static readonly IReadOnlyList<TrainingGroundLevelInfo> TrainingGround = new[]
    {
        new TrainingGroundLevelInfo(0, "The Match Pitch", "You train where you play — hard on the surface, harder on development.", 0, 0, 1.00, 1.00),
        new TrainingGroundLevelInfo(1, "Basic Training Field", "A second pitch, set aside just for the week's sessions.", 2_500, 2, 1.15, 1.10),
        new TrainingGroundLevelInfo(2, "Dedicated Facility", "Tackle bags, cones, a proper kit shed.", 12_000, 3, 1.30, 1.20),
        new TrainingGroundLevelInfo(3, "Gym & Analysis Suite", "A weights room and a laptop for match footage.", 35_000, 4, 1.45, 1.35),
        new TrainingGroundLevelInfo(4, "Medical & Conditioning Centre", "A physio on-site changes how fast the walking wounded come back.", 90_000, 6, 1.60, 1.55),
        new TrainingGroundLevelInfo(5, "High-Performance Centre", "Full-time medical, strength and conditioning staff.", 220_000, 8, 1.80, 1.80),
    };

    public static readonly IReadOnlyList<ClubhouseLevelInfo> Clubhouse = new[]
    {
        new ClubhouseLevelInfo(0, "No Clubhouse", "Players change in the car park. Just enough of a shirt-sponsor deal to keep the lights on.", 0, 0, 8_000, 5),
        new ClubhouseLevelInfo(1, "Portacabin Clubhouse", "Changing rooms and a tea urn — somewhere to actually sign a sponsor.", 2_000, 1, 12_000, 15),
        new ClubhouseLevelInfo(2, "Community Clubhouse", "A proper bar, a function room, and a noticeboard full of local sponsors.", 9_000, 3, 18_000, 30),
        new ClubhouseLevelInfo(3, "Members' Clubhouse", "The kind of place the whole town drinks in after a win.", 25_000, 4, 28_000, 45),
        new ClubhouseLevelInfo(4, "Commercial Suite", "Hospitality boxes and a sponsorship wall that actually earns its keep.", 70_000, 6, 45_000, 65),
        new ClubhouseLevelInfo(5, "Performance Academy", "Boardroom, media suite, and a name recognised across the pyramid.", 180_000, 8, 70_000, 90),
    };
}
