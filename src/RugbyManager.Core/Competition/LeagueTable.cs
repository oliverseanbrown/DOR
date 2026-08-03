using RugbyManager.Core.Model;

namespace RugbyManager.Core.Competition;

/// <summary>
/// A league standings snapshot computed from whatever fixtures have been played so far.
/// Rows are ordered by league points, then points difference, then tries, then points for.
/// </summary>
public sealed class LeagueTable
{
    public IReadOnlyList<TableRow> Rows { get; }

    private LeagueTable(IReadOnlyList<TableRow> rows) => Rows = rows;

    public TableRow RowFor(Team team) => Rows.First(r => ReferenceEquals(r.Team, team));

    public static LeagueTable Build(League league, IEnumerable<Fixture> fixtures)
    {
        var rows = new Dictionary<Team, TableRow>(ReferenceEqualityComparer.Instance);
        foreach (var team in league.Teams)
            rows[team] = new TableRow { Team = team };

        foreach (var fx in fixtures)
        {
            if (fx.Result is not { } r) continue;

            var home = rows[fx.Home];
            var away = rows[fx.Away];

            home.Played++; away.Played++;
            home.PointsFor += r.HomeScore; home.PointsAgainst += r.AwayScore;
            away.PointsFor += r.AwayScore; away.PointsAgainst += r.HomeScore;
            home.TriesFor += r.HomeStats.Tries;
            away.TriesFor += r.AwayStats.Tries;

            if (r.HomeScore > r.AwayScore) { home.Won++; away.Lost++; }
            else if (r.AwayScore > r.HomeScore) { away.Won++; home.Lost++; }
            else { home.Drawn++; away.Drawn++; }

            // Try bonus: 4+ tries in the match.
            if (r.HomeStats.Tries >= 4) home.TryBonuses++;
            if (r.AwayStats.Tries >= 4) away.TryBonuses++;

            // Losing bonus: lost by 7 or fewer (never on a draw).
            int margin = Math.Abs(r.HomeScore - r.AwayScore);
            if (margin is > 0 and <= 7)
            {
                if (r.HomeScore < r.AwayScore) home.LosingBonuses++;
                else away.LosingBonuses++;
            }
        }

        var ordered = rows.Values
            .OrderByDescending(x => x.LeaguePoints)
            .ThenByDescending(x => x.PointsDiff)
            .ThenByDescending(x => x.TriesFor)
            .ThenByDescending(x => x.PointsFor)
            .ToList();

        for (int i = 0; i < ordered.Count; i++)
            ordered[i].Position = i + 1;

        return new LeagueTable(ordered);
    }
}
