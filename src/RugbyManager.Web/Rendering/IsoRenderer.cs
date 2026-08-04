using System.Globalization;
using System.Text;

namespace RugbyManager.Web.Rendering;

/// <summary>
/// Minimal 2:1 isometric SVG drawing primitives. Everything works in a "grid" coordinate space
/// (gx, gy = ground position in tiles, gz = height in pixels) and emits raw SVG fragment
/// strings, painter's-algorithm style — callers are responsible for drawing back-to-front.
/// </summary>
public static class IsoRenderer
{
    public const double TileW = 30; // half-width of a tile footprint, px
    public const double TileH = 15; // half-height (2:1 ratio)

    public static (double X, double Y) Project(double gx, double gy, double gz = 0)
        => ((gx - gy) * TileW, (gx + gy) * TileH - gz);

    private static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);
    private static string Pt((double X, double Y) p) => $"{F(p.X)},{F(p.Y)}";

    /// <summary>An axis-aligned iso box footprint (gx,gy)-(gx+w,gy+d), sitting on top of
    /// <paramref name="baseZ"/> and rising h px further, with pseudo-lit top/left/right faces
    /// shaded from one base colour. Stack boxes by chaining baseZ = previous box's top.</summary>
    public static string Box(double gx, double gy, double w, double d, double h, string baseColor, string stroke = "#1c1430", double strokeWidth = 1.4, double baseZ = 0)
    {
        var a = Project(gx, gy, baseZ);
        var b = Project(gx + w, gy, baseZ);
        var c = Project(gx + w, gy + d, baseZ);
        var e = Project(gx, gy + d, baseZ);
        var a2 = Project(gx, gy, baseZ + h);
        var b2 = Project(gx + w, gy, baseZ + h);
        var c2 = Project(gx + w, gy + d, baseZ + h);
        var e2 = Project(gx, gy + d, baseZ + h);

        string top = Shade(baseColor, 0.32);
        string left = Shade(baseColor, -0.10);
        string right = Shade(baseColor, -0.32);

        var sb = new StringBuilder();
        if (h > 0.01)
        {
            sb.Append($"<polygon points=\"{Pt(e)} {Pt(c)} {Pt(c2)} {Pt(e2)}\" fill=\"{left}\" stroke=\"{stroke}\" stroke-width=\"{F(strokeWidth)}\" stroke-linejoin=\"round\"/>");
            sb.Append($"<polygon points=\"{Pt(c)} {Pt(b)} {Pt(b2)} {Pt(c2)}\" fill=\"{right}\" stroke=\"{stroke}\" stroke-width=\"{F(strokeWidth)}\" stroke-linejoin=\"round\"/>");
        }
        sb.Append($"<polygon points=\"{Pt(a2)} {Pt(b2)} {Pt(c2)} {Pt(e2)}\" fill=\"{top}\" stroke=\"{stroke}\" stroke-width=\"{F(strokeWidth)}\" stroke-linejoin=\"round\"/>");
        return sb.ToString();
    }

    /// <summary>A flat ground-level diamond — pitches, paths, forecourts.</summary>
    public static string Tile(double gx, double gy, double w, double d, string color, string stroke = "none", double strokeWidth = 0)
    {
        var a = Project(gx, gy); var b = Project(gx + w, gy); var c = Project(gx + w, gy + d); var e = Project(gx, gy + d);
        var strokeAttr = stroke == "none" ? "" : $" stroke=\"{stroke}\" stroke-width=\"{F(strokeWidth)}\"";
        return $"<polygon points=\"{Pt(a)} {Pt(b)} {Pt(c)} {Pt(e)}\" fill=\"{color}\"{strokeAttr}/>";
    }

    public static string Line(double gx1, double gy1, double gz1, double gx2, double gy2, double gz2, string color, double width = 2, string dash = "")
    {
        var p1 = Project(gx1, gy1, gz1); var p2 = Project(gx2, gy2, gz2);
        var dashAttr = dash.Length == 0 ? "" : $" stroke-dasharray=\"{dash}\"";
        return $"<line x1=\"{F(p1.X)}\" y1=\"{F(p1.Y)}\" x2=\"{F(p2.X)}\" y2=\"{F(p2.Y)}\" stroke=\"{color}\" stroke-width=\"{F(width)}\"{dashAttr} stroke-linecap=\"round\"/>";
    }

    public static string Circle(double gx, double gy, double gz, double r, string fill, string stroke = "none", double strokeWidth = 0)
    {
        var p = Project(gx, gy, gz);
        var strokeAttr = stroke == "none" ? "" : $" stroke=\"{stroke}\" stroke-width=\"{F(strokeWidth)}\"";
        return $"<circle cx=\"{F(p.X)}\" cy=\"{F(p.Y)}\" r=\"{F(r)}\" fill=\"{fill}\"{strokeAttr}/>";
    }

    public static string Text(double gx, double gy, double gz, string content, string cls = "", string fill = "#f2eefc", double size = 12, string anchor = "middle", string weight = "600")
    {
        var p = Project(gx, gy, gz);
        var classAttr = cls.Length == 0 ? "" : $" class=\"{cls}\"";
        return $"<text x=\"{F(p.X)}\" y=\"{F(p.Y)}\" text-anchor=\"{anchor}\" font-size=\"{F(size)}\" font-weight=\"{weight}\" fill=\"{fill}\"{classAttr}>{System.Net.WebUtility.HtmlEncode(content)}</text>";
    }

    /// <summary>Shifts a hex colour toward white (amount > 0) or black (amount < 0), amount in [-1,1].</summary>
    public static string Shade(string hex, double amount)
    {
        hex = hex.TrimStart('#');
        if (hex.Length != 6) return "#" + hex;
        int r = Convert.ToInt32(hex[..2], 16), g = Convert.ToInt32(hex.Substring(2, 2), 16), b = Convert.ToInt32(hex.Substring(4, 2), 16);
        return $"#{Adjust(r, amount):x2}{Adjust(g, amount):x2}{Adjust(b, amount):x2}";
    }

    /// <summary>Linear interpolation between two hex colours, t in [0,1].</summary>
    public static string Lerp(string fromHex, string toHex, double t)
    {
        t = Math.Clamp(t, 0, 1);
        (int r1, int g1, int b1) = Rgb(fromHex);
        (int r2, int g2, int b2) = Rgb(toHex);
        int r = (int)(r1 + (r2 - r1) * t), g = (int)(g1 + (g2 - g1) * t), b = (int)(b1 + (b2 - b1) * t);
        return $"#{r:x2}{g:x2}{b:x2}";
    }

    private static (int, int, int) Rgb(string hex)
    {
        hex = hex.TrimStart('#');
        return (Convert.ToInt32(hex[..2], 16), Convert.ToInt32(hex.Substring(2, 2), 16), Convert.ToInt32(hex.Substring(4, 2), 16));
    }

    private static int Adjust(int channel, double amount)
        => amount >= 0
            ? Math.Clamp((int)(channel + (255 - channel) * amount), 0, 255)
            : Math.Clamp((int)(channel * (1 + amount)), 0, 255);
}
