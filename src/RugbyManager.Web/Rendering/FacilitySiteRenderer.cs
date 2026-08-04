using System.Text;
using RugbyManager.Core.Facilities;
using RugbyManager.Core.Model;
using static RugbyManager.Web.Rendering.IsoRenderer;

namespace RugbyManager.Web.Rendering;

/// <summary>
/// Composes the whole club-grounds isometric scene from a team's facility levels: the pitch at
/// the centre, stands wrapping it as the stadium grows, a training complex to one side, and the
/// clubhouse out front. Each zone is wrapped in an SVG &lt;a&gt; linking to its upgrade card
/// (id="facility-{area}") — plain anchor navigation, no JS interop needed.
/// </summary>
public static class FacilitySiteRenderer
{
    // Pitch occupies the centre of the grid; every other zone is positioned relative to it.
    private const double PitchX = 0, PitchY = 0, PitchW = 8, PitchD = 5;
    private const double TrainX = -6.4, TrainY = 1, TrainW = 4.6, TrainD = 4;
    private const double ClubX = 2, ClubY = 6.8, ClubW = 4.2, ClubD = 3;

    // The ground plate's own footprint — sized to hug the zones above with a little breathing
    // room, not the whole notional world. Used both to draw the plate and to size the viewBox.
    private const double GroundX = -7.6, GroundY = -2.6, GroundW = 17.8, GroundD = 13.6;

    /// <summary>A viewBox string that exactly frames the scene's ground plate, with extra
    /// headroom above for the floating zone labels, construction cranes and badges.</summary>
    public static string ViewBox()
    {
        var corners = new[]
        {
            Project(GroundX, GroundY), Project(GroundX + GroundW, GroundY),
            Project(GroundX, GroundY + GroundD), Project(GroundX + GroundW, GroundY + GroundD),
        };
        double minX = corners.Min(c => c.X) - 20;
        double maxX = corners.Max(c => c.X) + 20;
        double minY = corners.Min(c => c.Y) - 55; // headroom for labels/cranes/badges/floodlights
        double maxY = corners.Max(c => c.Y) + 15;
        return $"{minX:0} {minY:0} {maxX - minX:0} {maxY - minY:0}";
    }

    public static string BuildScene(Team club)
    {
        var sb = new StringBuilder();
        sb.Append(DrawGround());
        sb.Append(DrawClubhouse(club));
        sb.Append(DrawStadiumAndPitch(club));
        // Drawn last so a grassroots (level-0) training ground's cones — which sit on the
        // pitch's own corner — paint on top of the pitch grass instead of being hidden under it.
        sb.Append(DrawTrainingGround(club));
        return sb.ToString();
    }

    /// <summary>
    /// Wraps a zone's markup in a clickable group that scrolls to and briefly highlights its
    /// upgrade card. Deliberately plain inline JS via onclick rather than an &lt;a href="#..."&gt;
    /// — Blazor's client-side router globally intercepts anchor clicks for SPA navigation and
    /// silently swallows fragment-only hrefs that don't match a registered route, so a real
    /// &lt;a&gt; here never actually jumps anywhere.
    /// </summary>
    private static string Anchor(string area, string inner)
    {
        string id = $"facility-{area}";
        string js = $"var t=document.getElementById('{id}');if(t){{t.scrollIntoView({{behavior:'smooth',block:'center'}});t.classList.add('iso-jump');setTimeout(function(){{t.classList.remove('iso-jump');}},1600);}}";
        return $"<g class=\"iso-zone-link\" onclick=\"{js}\">{inner}</g>";
    }

    /// <summary>An invisible-but-painted tile so a whole zone footprint is clickable even where
    /// the art at low levels (e.g. an empty rec ground) barely paints any real geometry — SVG
    /// only hit-tests painted areas by default, not empty space inside a bounding box.</summary>
    private static string HitArea(double gx, double gy, double w, double d) => Tile(gx, gy, w, d, "rgba(255,255,255,0.02)");

    // ------------------------------------------------------------------
    //  Shared ground plate + a few decorative trees for atmosphere
    // ------------------------------------------------------------------

    private static string DrawGround()
    {
        var sb = new StringBuilder();
        sb.Append(Tile(GroundX, GroundY, GroundW, GroundD, "#22351f"));
        foreach (var (gx, gy) in new (double, double)[] { (-6.8, -1.8), (9.2, -1.6), (-6.6, 10.2), (9, 8.6), (-0.5, -2.2) })
        {
            sb.Append(Circle(gx, gy, 10, 5, "#2f4a2b"));
            sb.Append(Circle(gx - 0.4, gy + 0.3, 16, 4, "#3a5a34"));
            sb.Append(Circle(gx + 0.4, gy - 0.2, 20, 3.4, "#3a5a34"));
        }
        return sb.ToString();
    }

    // ------------------------------------------------------------------
    //  Pitch + stadium — drawn as one zone since the stands wrap the pitch
    // ------------------------------------------------------------------

    private static string DrawStadiumAndPitch(Team club)
    {
        int pitchLevel = club.PitchLevel, pitchMax = FacilityCatalog.Pitch.Count - 1;
        int stadiumLevel = club.StadiumLevel;
        double t = pitchLevel / (double)pitchMax;

        // Behind group: north/west stands, the level-0 rope, and a generous invisible hit area
        // covering the whole ring around the pitch — needed because at low stadium levels there's
        // barely any painted geometry out there for a click to land on.
        var behind = new StringBuilder();
        behind.Append(HitArea(PitchX - 1.5, PitchY - 1.5, PitchW + 3, PitchD + 3));
        behind.Append(DrawStand("north", PitchX, PitchY - 1.1, PitchW, 1.0, stadiumLevel, club.PrimaryColour));
        behind.Append(DrawStand("west", PitchX - 1.1, PitchY, 1.0, PitchD, stadiumLevel, club.PrimaryColour));
        if (stadiumLevel == 0)
        {
            string rope = "#c9a227";
            behind.Append(Line(PitchX - 0.3, PitchY - 0.3, 3, PitchX + PitchW + 0.3, PitchY - 0.3, 3, rope, 1.5, "3,3"));
            behind.Append(Line(PitchX + PitchW + 0.3, PitchY - 0.3, 3, PitchX + PitchW + 0.3, PitchY + PitchD + 0.3, 3, rope, 1.5, "3,3"));
            behind.Append(Line(PitchX + PitchW + 0.3, PitchY + PitchD + 0.3, 3, PitchX - 0.3, PitchY + PitchD + 0.3, 3, rope, 1.5, "3,3"));
            behind.Append(Line(PitchX - 0.3, PitchY + PitchD + 0.3, 3, PitchX - 0.3, PitchY - 0.3, 3, rope, 1.5, "3,3"));
            for (int i = 0; i < 6; i++)
                behind.Append(Circle(PitchX + 0.6 + i * 1.2, PitchY - 0.6, 6, 1.6, "#2a2140"));
        }
        behind.Append(ZoneLabel(4, -2.4, FacilityService.StadiumInfo(club).Name, club.StadiumLevel, club));
        behind.Append(ZoneConstruction(club, FacilityArea.Stadium, 4, -2.4));

        // Pitch: its own click target, on top of the "behind" stand hit area within its footprint.
        var pitch = new StringBuilder();
        string grass = Lerp("#7a8f5a", "#159e3f", t);
        pitch.Append(Tile(PitchX, PitchY, PitchW, PitchD, grass));
        pitch.Append(PitchMarkings(pitchLevel, grass, club.SecondaryColour));
        pitch.Append(ZoneLabel(4, PitchD + 1.9, FacilityService.PitchInfo(club).Name, club.PitchLevel, club, small: true));
        pitch.Append(ZoneConstruction(club, FacilityArea.Pitch, 4, PitchD + 2.3, small: true));

        // Front group: south/east stands + floodlights, painted after the pitch so they occlude
        // it correctly at the shared edge.
        var front = new StringBuilder();
        front.Append(DrawStand("south", PitchX, PitchY + PitchD + 0.1, PitchW, 1.0, stadiumLevel, club.PrimaryColour));
        front.Append(DrawStand("east", PitchX + PitchW + 0.1, PitchY, 1.0, PitchD, stadiumLevel, club.PrimaryColour));
        if (stadiumLevel >= 5)
        {
            foreach (var (fx, fy) in new (double, double)[] { (PitchX - 1.3, PitchY - 1.3), (PitchX + PitchW + 1.3, PitchY - 1.3), (PitchX - 1.3, PitchY + PitchD + 1.3), (PitchX + PitchW + 1.3, PitchY + PitchD + 1.3) })
            {
                front.Append(Line(fx, fy, 0, fx, fy, 46, "#3a3450", 2));
                front.Append(Circle(fx, fy, 48, 3.2, "#fff6c9", "#c9a227", 1));
            }
        }

        return Anchor("stadium", behind.ToString()) + Anchor("pitch", pitch.ToString()) + Anchor("stadium", front.ToString());
    }

    private static string DrawStand(string edge, double gx, double gy, double w, double d, int level, string primary)
    {
        if (level == 0) return "";
        double height = level switch { 1 => 6, 2 or 3 => 9, 4 => 10, 5 => 14, _ => 18 };
        bool present = edge switch
        {
            "north" => level >= 1,
            "south" => level >= 3,
            "west" or "east" => level >= 4,
            _ => false,
        };
        if (!present) return "";

        string seat = level >= 2 ? primary : "#8a8a8a";
        var sb = new StringBuilder();
        sb.Append(Box(gx, gy, w, d, height, seat));
        if (level >= 2)
            sb.Append(Box(gx, gy, w, d, 1.6, "#2a2438", baseZ: height));
        return sb.ToString();
    }

    private static string PitchMarkings(int level, string grass, string secondary)
    {
        var sb = new StringBuilder();
        if (level >= 1)
        {
            sb.Append(Line(PitchX + PitchW / 2, PitchY, 1, PitchX + PitchW / 2, PitchY + PitchD, 1, "#f5f5f5", 1.3));
            sb.Append(Line(PitchX + 0.7, PitchY, 1, PitchX + 0.7, PitchY + PitchD, 1, "#f5f5f5", 1));
            sb.Append(Line(PitchX + PitchW - 0.7, PitchY, 1, PitchX + PitchW - 0.7, PitchY + PitchD, 1, "#f5f5f5", 1));
        }
        if (level >= 2)
        {
            sb.Append(Line(PitchX + 2.2, PitchY, 1, PitchX + 2.2, PitchY + PitchD, 1, "#f5f5f5", 0.8));
            sb.Append(Line(PitchX + PitchW - 2.2, PitchY, 1, PitchX + PitchW - 2.2, PitchY + PitchD, 1, "#f5f5f5", 0.8));
        }
        if (level >= 3)
        {
            string stripe = Shade(grass, -0.10);
            for (double i = 0.2; i < PitchW; i += 2)
                sb.Append(Tile(PitchX + i, PitchY, 1, PitchD, stripe));
        }
        if (level >= 4)
        {
            for (int i = 0; i < 4; i++)
                sb.Append(Box(PitchX + 1 + i * 2, PitchY - 0.28, 0.9, 0.14, 1.6, secondary));
        }
        if (level >= 5)
        {
            foreach (var (cx, cy) in new (double, double)[] { (PitchX + 0.1, PitchY + 0.1), (PitchX + PitchW - 0.1, PitchY + 0.1), (PitchX + 0.1, PitchY + PitchD - 0.1), (PitchX + PitchW - 0.1, PitchY + PitchD - 0.1) })
            {
                sb.Append(Line(cx, cy, 0, cx, cy, 6, "#e8e8e8", 1));
                sb.Append(Box(cx - 0.15, cy - 0.02, 0.3, 0.15, 1.6, secondary, baseZ: 5.2));
            }
        }
        // Posts at both ends, every level.
        sb.Append(GoalPosts(PitchX + PitchW / 2, PitchY - 0.05));
        sb.Append(GoalPosts(PitchX + PitchW / 2, PitchY + PitchD + 0.05));
        return sb.ToString();
    }

    private static string GoalPosts(double gx, double gy)
    {
        var sb = new StringBuilder();
        sb.Append(Line(gx - 0.9, gy, 0, gx - 0.9, gy, 7.5, "#f0f0f0", 1.3));
        sb.Append(Line(gx + 0.9, gy, 0, gx + 0.9, gy, 7.5, "#f0f0f0", 1.3));
        sb.Append(Line(gx - 0.9, gy, 5.5, gx + 0.9, gy, 5.5, "#f0f0f0", 1.3));
        return sb.ToString();
    }

    // ------------------------------------------------------------------
    //  Training ground
    // ------------------------------------------------------------------

    private static string DrawTrainingGround(Team club)
    {
        int level = club.TrainingGroundLevel;
        var sb = new StringBuilder();

        if (level == 0)
        {
            sb.Append(Tile(PitchX + 0.2, PitchY + PitchD - 1.2, 1.8, 1.2, "rgba(255,255,255,0.02)"));
            for (int i = 0; i < 3; i++)
                sb.Append(Circle(PitchX + 0.8 + i * 0.7, PitchY + PitchD - 0.6, 2, 1, "#e2711d"));
        }
        else
        {
            sb.Append(Tile(TrainX, TrainY, TrainW, TrainD, "#2f7a35"));
            sb.Append(Line(TrainX + TrainW / 2, TrainY, 1, TrainX + TrainW / 2, TrainY + TrainD, 1, "#eef", 1));
            for (int i = 0; i < 3; i++)
                sb.Append(Circle(TrainX + 0.5 + i * 0.5, TrainY + 0.5, 2, 0.8, "#e2711d"));

            if (level >= 2)
                sb.Append(Box(TrainX + 0.4, TrainY + 1.6, 1.6, 1.3, 5, "#8f7256"));
            if (level >= 3)
                sb.Append(Box(TrainX + 2.3, TrainY + 1.7, 1.2, 1.1, 4.2, club.PrimaryColour));
            if (level >= 4)
            {
                sb.Append(Box(TrainX + 0.5, TrainY + 3.1, 1.4, 1.1, 5, "#e7e2f0"));
                sb.Append(Line(TrainX + 1.1, TrainY + 3.65, 5.2, TrainX + 1.3, TrainY + 3.65, 5.2, "#d1354a", 2.2));
                sb.Append(Line(TrainX + 1.2, TrainY + 3.55, 5.2, TrainX + 1.2, TrainY + 3.75, 5.2, "#d1354a", 2.2));
            }
            if (level >= 5)
            {
                sb.Append(Box(TrainX + 2.2, TrainY + 3.0, 1.6, 1.2, 6, "#8f7256"));
                sb.Append(Line(TrainX + 3.6, TrainY + 0.4, 0, TrainX + 3.6, TrainY + 0.4, 30, "#3a3450", 1.6));
                sb.Append(Circle(TrainX + 3.6, TrainY + 0.4, 32, 2.4, "#fff6c9", "#c9a227", 1));
            }
        }

        return Anchor("training", sb.ToString()
            + ZoneLabel(TrainX + TrainW / 2, TrainY - 1.1, FacilityService.TrainingGroundInfo(club).Name, level, club)
            + ZoneConstruction(club, FacilityArea.TrainingGround, TrainX + TrainW / 2, TrainY - 1.1));
    }

    // ------------------------------------------------------------------
    //  Clubhouse
    // ------------------------------------------------------------------

    private static string DrawClubhouse(Team club)
    {
        int level = club.ClubhouseLevel;
        var sb = new StringBuilder();

        sb.Append(Tile(ClubX, ClubY, ClubW, ClubD, "#4a4560"));

        double footW = 1.4 + level * 0.18, footD = 1.1 + level * 0.12;
        double height = 3.6 + level * 1.1;
        string wall = Lerp("#9a9a9a", "#e7dcc9", level / 5.0);
        double bx = ClubX + 0.6, by = ClubY + 0.7;

        sb.Append(Box(bx, by, footW, footD, height, wall));
        if (level >= 2)
            sb.Append(Box(bx - 0.08, by - 0.08, footW + 0.16, footD + 0.16, 0.9, "#2e2a3d", baseZ: height));
        if (level >= 3)
            sb.Append(Box(bx + 0.2, by + 0.2, footW - 0.4, footD - 0.4, 1.8, wall, baseZ: height + 0.9));
        if (level >= 1)
        {
            sb.Append(Line(bx + footW + 0.3, by + footD + 0.4, 0, bx + footW + 0.3, by + footD + 0.4, 3.4 + level * 0.3, "#6a6480", 1.4));
            sb.Append(Box(bx + footW + 0.3, by + footD + 0.15, 0.55, 0.05, 0.4, club.SecondaryColour, baseZ: 2.6 + level * 0.3));
        }
        if (level >= 4)
        {
            sb.Append(Box(bx + footW + 0.15, by, 1.1, footD * 0.8, height * 0.7, wall));
            var cars = level >= 5 ? 3 : 2;
            for (int i = 0; i < cars; i++)
                sb.Append(Box(ClubX + 0.3 + i * 0.9, ClubY + ClubD - 0.9, 0.7, 0.4, 0.6, i % 2 == 0 ? "#3b6ea5" : "#a5423b"));
        }
        if (level >= 2)
        {
            for (int i = 0; i < 1 + level / 2; i++)
                sb.Append(Box(bx + 0.25 + i * 0.55, by - 0.03, 0.3, 0.06, 0.5, "#fde68a", baseZ: height * 0.45));
        }

        return Anchor("clubhouse", sb.ToString()
            + ZoneLabel(ClubX + ClubW / 2, ClubY - 0.6, FacilityService.ClubhouseInfo(club).Name, level, club)
            + ZoneConstruction(club, FacilityArea.Clubhouse, ClubX + ClubW / 2, ClubY - 0.6));
    }

    // ------------------------------------------------------------------
    //  Labels + construction markers, shared by every zone
    // ------------------------------------------------------------------

    private static string ZoneLabel(double gx, double gy, string levelName, int level, Team club, bool small = false)
    {
        double gz = small ? 4 : 60;
        var sb = new StringBuilder();
        sb.Append($"<g class=\"iso-label\">");
        sb.Append(Text(gx, gy, gz + 16, $"Lv {level} — {levelName}", "iso-label-text", size: small ? 10 : 11.5));
        sb.Append("</g>");
        return sb.ToString();
    }

    private static string ZoneConstruction(Team club, FacilityArea area, double gx, double gy, bool small = false)
    {
        var project = FacilityService.ProjectFor(club, area);
        if (project is null) return "";

        double gz = (small ? 4 : 60) + 32;
        var sb = new StringBuilder();
        // A little crane: mast + jib + cable.
        sb.Append(Line(gx - 1.6, gy, 0, gx - 1.6, gy, gz + 20, "#d97706", 2));
        sb.Append(Line(gx - 1.6, gy, gz + 20, gx + 0.6, gy, gz + 20, "#d97706", 2));
        sb.Append(Line(gx + 0.5, gy, gz + 20, gx + 0.5, gy, gz + 8, "#d97706", 1.2));

        sb.Append($"<g class=\"iso-badge\">");
        var (bx, by) = Project(gx, gy, gz + 30);
        sb.Append($"<rect x=\"{(bx - 46):0.##}\" y=\"{(by - 12):0.##}\" width=\"92\" height=\"22\" rx=\"11\" fill=\"#d97706\" stroke=\"#1c1430\" stroke-width=\"1.4\"/>");
        sb.Append(Text(gx, gy, gz + 30 - 3, $"{project.WeeksRemaining} wk{(project.WeeksRemaining == 1 ? "" : "s")} left", fill: "#1c1430", size: 10.5, weight: "800"));
        sb.Append("</g>");
        return sb.ToString();
    }
}
