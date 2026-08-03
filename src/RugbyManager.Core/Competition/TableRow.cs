using RugbyManager.Core.Model;

namespace RugbyManager.Core.Competition;

/// <summary>
/// One club's line in the league table. Uses the standard Rugby Union points system:
/// win = 4, draw = 2, loss = 0, plus bonus points (see <see cref="LeaguePoints"/>).
/// </summary>
public sealed class TableRow
{
    public required Team Team { get; init; }

    /// <summary>Final ladder position (1 = top), assigned when the table is built.</summary>
    public int Position { get; set; }

    public int Played { get; set; }
    public int Won { get; set; }
    public int Drawn { get; set; }
    public int Lost { get; set; }

    public int PointsFor { get; set; }
    public int PointsAgainst { get; set; }
    public int TriesFor { get; set; }

    /// <summary>Bonus point for scoring 4+ tries in a match.</summary>
    public int TryBonuses { get; set; }

    /// <summary>Bonus point for losing by 7 points or fewer.</summary>
    public int LosingBonuses { get; set; }

    public int PointsDiff => PointsFor - PointsAgainst;
    public int BonusPoints => TryBonuses + LosingBonuses;

    /// <summary>4 per win, 2 per draw, plus all bonus points.</summary>
    public int LeaguePoints => Won * 4 + Drawn * 2 + BonusPoints;
}
