using RugbyManager.Core.Competition;
using RugbyManager.Core.Match;
using RugbyManager.Core.Model;
using RugbyManager.Core.Transfers;

namespace RugbyManager.ConsoleApp;

/// <summary>Console rendering for tables, squads, fixtures and match feeds.</summary>
public static class Display
{
    public static void Table(LeagueTable table, Team myClub)
    {
        Console.WriteLine();
        Console.WriteLine("  LEAGUE TABLE");
        Console.WriteLine("  ------------------------------------------------------------------------------");
        Console.WriteLine($"     {"Club",-22}{"P",3}{"W",3}{"D",3}{"L",3}{"PF",5}{"PA",5}{"PD",5}{"TF",4}{"BP",4}{"Pts",5}");
        Console.WriteLine("  ------------------------------------------------------------------------------");
        foreach (var r in table.Rows)
        {
            char me = ReferenceEquals(r.Team, myClub) ? '>' : ' ';
            string pd = (r.PointsDiff >= 0 ? "+" : "") + r.PointsDiff;
            Console.WriteLine(
                $"  {me}{r.Position,2} {r.Team.Name,-22}{r.Played,3}{r.Won,3}{r.Drawn,3}{r.Lost,3}" +
                $"{r.PointsFor,5}{r.PointsAgainst,5}{pd,5}{r.TriesFor,4}{r.BonusPoints,4}{r.LeaguePoints,5}");
        }
        Console.WriteLine("  ------------------------------------------------------------------------------");
        Console.WriteLine("  PF/PA=points for/against  PD=diff  TF=tries  BP=bonus  Pts=league points");
    }

    public static void PlayerDetail(Team team, Player p)
    {
        var a = p.Attributes;
        bool inXv = team.Players.Contains(p);
        string status = !p.IsFit ? $"INJURED, {p.InjuredWeeksRemaining}w" : inXv ? "Starting XV" : "Bench";

        Console.WriteLine();
        Console.WriteLine($"  {p.FullName}   ({p.NaturalPosition.ShortName()} — {p.NaturalPosition})");
        Console.WriteLine("  ------------------------------------------------------------------------------");
        Console.WriteLine($"  Age {p.Age}   OVR {PlayerRating.Overall(p)}   Condition {p.Condition}%   {status}");
        Console.WriteLine($"  Potential: {PotentialBand(p)}   Injury risk: {InjuryRiskBand(p)}");
        Console.WriteLine();

        PrintGroup("Physical", p, new (string, int)[]
        {
            ("Strength", a.Strength), ("Pace", a.Pace), ("Acceleration", a.Acceleration),
            ("Stamina", a.Stamina), ("Agility", a.Agility),
        });
        PrintGroup("Technical", p, new (string, int)[]
        {
            ("Handling", a.Handling), ("Passing", a.Passing), ("Tackling", a.Tackling),
            ("Kicking", a.Kicking), ("GoalKicking", a.GoalKicking), ("Lineout", a.Lineout),
            ("Scrummaging", a.Scrummaging), ("Breakdown", a.Breakdown),
        });
        PrintGroup("Mental", p, new (string, int)[]
        {
            ("DecisionMaking", a.DecisionMaking), ("Composure", a.Composure), ("Discipline", a.Discipline),
            ("WorkRate", a.WorkRate), ("Leadership", a.Leadership), ("Positioning", a.Positioning),
            ("Vision", a.Vision), ("Aggression", a.Aggression),
        });

        if (p.TrainingGains.Count > 0)
        {
            Console.WriteLine("  ------------------------------------------------------------------------------");
            string gains = string.Join(", ", p.TrainingGains.OrderByDescending(g => g.Value).Select(g => $"{Spaced(g.Key)} +{g.Value}"));
            Console.WriteLine($"  Career training gains: {gains}");
        }
    }

    private static void PrintGroup(string title, Player p, (string Name, int Value)[] attrs)
    {
        Console.WriteLine($"  {title}:");
        foreach (var (name, value) in attrs)
        {
            int gain = p.TrainingGains.GetValueOrDefault(name);
            string gainTag = gain > 0 ? $" (+{gain})" : "";
            Console.WriteLine($"    {Spaced(name),-16}{value,3}{gainTag}");
        }
        Console.WriteLine();
    }

    public static string Spaced(string name)
        => System.Text.RegularExpressions.Regex.Replace(name, "(?<!^)([A-Z])", " $1");

    private static string PotentialBand(Player p)
    {
        int gap = p.Attributes.Potential - PlayerRating.Overall(p);
        return gap switch { >= 15 => "High", >= 5 => "Some room", _ => "Fully developed" };
    }

    private static string InjuryRiskBand(Player p) => p.Attributes.InjuryProneness switch
    {
        >= 66 => "High", >= 33 => "Medium", _ => "Low",
    };

    public static void Squad(Team team)
    {
        var xv = new HashSet<Player>(team.Players);
        Console.WriteLine();
        Console.WriteLine($"  SQUAD — {team.Name}   ({team.Squad.Count} players, tactics: {team.Tactics.PlayStyle})");
        Console.WriteLine("  ------------------------------------------------------------------------------");
        Console.WriteLine($"    #  {"Pos",-4}{"Name",-22}{"Age",4}{"OVR",5}{"Cond",7}   Status");
        Console.WriteLine("  ------------------------------------------------------------------------------");

        var rows = team.Squad
            .Select((p, idx) => (p, idx))
            .OrderBy(x => x.p.NaturalPosition)
            .ThenByDescending(x => PlayerRating.Overall(x.p));

        foreach (var (p, idx) in rows)
        {
            string status = !p.IsFit ? $"INJ {p.InjuredWeeksRemaining}w" : xv.Contains(p) ? "XV" : "bench";
            Console.WriteLine(
                $"  {idx,3}  {p.NaturalPosition.ShortName(),-4}{p.FullName,-22}{p.Age,4}" +
                $"{PlayerRating.Overall(p),5}{p.Condition + "%",7}   {status}");
        }
        Console.WriteLine("  ------------------------------------------------------------------------------");
        double xvOvr = team.Players.Average(PlayerRating.Overall);
        Console.WriteLine($"  Starting XV avg OVR: {xvOvr:0.0}   XV avg condition: {team.Players.Average(p => p.Condition):0}%");
        Console.WriteLine("  Sell a squad player with:  sell <#>");
    }

    public static void TacticsView(Team team)
    {
        var t = team.Tactics;
        Console.WriteLine();
        Console.WriteLine($"  TACTICS — {team.Name}");
        Console.WriteLine("  ------------------------------------------------------------------------------");
        Console.WriteLine($"    style     {t.PlayStyle,-16}  (forwards | balanced | expansive | kicking)");
        Console.WriteLine($"    breakdown {t.BreakdownFocus,-16}  (conservative | balanced | aggressive)");
        Console.WriteLine($"    defence   {t.DefensiveLine,-16}  (drift | standard | rush)");
        Console.WriteLine($"    penalty   {t.PenaltyPhilosophy,-16}  (pragmatic | balanced | ambitious)");
        Console.WriteLine($"    kick      {t.KickingTendency,-16}  (0-100 tendency to kick from hand)");
        Console.WriteLine("  Change with e.g.:  style expansive   defence rush   kick 30");
    }

    public static void MyFixtures(Season season, Team myClub)
    {
        Console.WriteLine();
        Console.WriteLine($"  FIXTURES — {myClub.Name}");
        Console.WriteLine("  ------------------------------------------------------------------------------");
        var mine = season.Fixtures
            .Where(f => ReferenceEquals(f.Home, myClub) || ReferenceEquals(f.Away, myClub))
            .OrderBy(f => f.Round);

        foreach (var fx in mine)
        {
            bool home = ReferenceEquals(fx.Home, myClub);
            var opp = home ? fx.Away : fx.Home;
            string venue = home ? "(H)" : "(A)";
            if (fx.Result is { } r)
            {
                int my = home ? r.HomeScore : r.AwayScore;
                int th = home ? r.AwayScore : r.HomeScore;
                char o = my > th ? 'W' : my < th ? 'L' : 'D';
                Console.WriteLine($"  R{fx.Round + 1,-2} {venue} v {opp.Name,-22} {my,3} - {th,-3}  {o}");
            }
            else
            {
                Console.WriteLine($"  R{fx.Round + 1,-2} {venue} v {opp.Name,-22}   (to play)");
            }
        }
    }

    public static void RoundResults(int round, IReadOnlyList<Fixture> fixtures, Team myClub)
    {
        Console.WriteLine();
        Console.WriteLine($"  ROUND {round + 1} RESULTS");
        Console.WriteLine("  ------------------------------------------------------------------------------");
        foreach (var fx in fixtures)
        {
            if (fx.Result is not { } r) continue;
            bool involvesMe = ReferenceEquals(fx.Home, myClub) || ReferenceEquals(fx.Away, myClub);
            char me = involvesMe ? '>' : ' ';
            Console.WriteLine($"  {me} {fx.Home.ShortName,-4} {r.HomeScore,3} - {r.AwayScore,-3} {fx.Away.ShortName}");
        }
    }

    public static void Commentary(MatchResult result)
    {
        Console.WriteLine();
        Console.WriteLine($"  {result.HomeName}  v  {result.AwayName}   (seed {result.Seed})");
        Console.WriteLine("  ------------------------------------------------------------------------------");
        foreach (var e in result.Events)
        {
            bool scoring = e.Type is MatchEventType.Try or MatchEventType.Conversion
                or MatchEventType.PenaltyGoal or MatchEventType.DropGoal
                or MatchEventType.HalfTime or MatchEventType.FullTime;
            string score = scoring ? $"   [{e.HomeScore}-{e.AwayScore}]" : "";
            Console.WriteLine($"  {e.Minute,3}'  {e.Text}{score}");
        }
        Console.WriteLine();
        Console.WriteLine($"  FULL-TIME: {result.ScoreLine}  (winner: {result.Winner})");
        BoxScore(result);
    }

    public static void BoxScore(MatchResult r)
    {
        Console.WriteLine("  ------------------------------------------------------------------------------");
        Console.WriteLine($"  {"",-22}{r.HomeShort,10}{r.AwayShort,10}");
        Row("Tries", r.HomeStats.Tries, r.AwayStats.Tries);
        Row("Conversions", r.HomeStats.Conversions, r.AwayStats.Conversions);
        Row("Penalty goals", r.HomeStats.PenaltyGoals, r.AwayStats.PenaltyGoals);
        Row("Drop goals", r.HomeStats.DropGoals, r.AwayStats.DropGoals);
        Row("Line breaks", r.HomeStats.LineBreaks, r.AwayStats.LineBreaks);
        Row("Turnovers won", r.HomeStats.TurnoversWon, r.AwayStats.TurnoversWon);
        Row("Penalties conceded", r.HomeStats.PenaltiesConceded, r.AwayStats.PenaltiesConceded);
        RowPct("Possession %", r.HomeStats.PossessionPct, r.AwayStats.PossessionPct);
        RowPct("Territory %", r.HomeStats.TerritoryPct, r.AwayStats.TerritoryPct);

        static void Row(string label, int h, int a) => Console.WriteLine($"  {label,-22}{h,10}{a,10}");
        static void RowPct(string label, double h, double a) => Console.WriteLine($"  {label,-22}{h,9:0}%{a,9:0}%");
    }

    public static void Market(TransferMarket market, Team myClub, string? posFilter = null)
    {
        Console.WriteLine();
        Console.WriteLine($"  TRANSFER MARKET     (your budget: {TransferService.Money(myClub.Money)})");
        Console.WriteLine("  ------------------------------------------------------------------------------");
        Console.WriteLine($"    #  {"Pos",-4}{"Name",-22}{"Age",4}{"OVR (est)",10}{"Value",12}   vs your starter");
        Console.WriteLine("  ------------------------------------------------------------------------------");

        var listed = market.Available
            .Select((p, i) => (p, i))
            .Where(x => posFilter is null || x.p.NaturalPosition.ShortName().Equals(posFilter, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.p.NaturalPosition)
            .ThenByDescending(x => PlayerRating.Overall(x.p));

        foreach (var (p, i) in listed)
        {
            int mine = PlayerRating.Overall(myClub.At(p.NaturalPosition));
            var (lo, hi) = Scouting.OverallRange(p);
            string ovrText = Scouting.FullyKnown(p) ? $"{hi}" : $"{lo}-{hi}";
            string delta = !Scouting.FullyKnown(p)
                ? (lo > mine ? "upgrade" : hi < mine ? "weaker" : "unclear")
                : (hi > mine ? $"upgrade (+{hi - mine})" : hi == mine ? "same" : $"weaker ({hi - mine})");
            Console.WriteLine(
                $"  {i,4}  {p.NaturalPosition.ShortName(),-4}{p.FullName,-22}{p.Age,4}{ovrText,10}" +
                $"{TransferService.Money(TransferValue.Estimate(p)),12}   {delta} (you {mine})");
        }
        Console.WriteLine("  ------------------------------------------------------------------------------");
        Console.WriteLine($"  sign <#> to buy · scout <#> to reveal ability ({TransferService.Money(Scouting.ScoutCost)}/report) · market <pos> to filter");
    }

    public static void Coaches(Team team)
    {
        Console.WriteLine();
        Console.WriteLine($"  COACHING STAFF — {team.Name}");
        Console.WriteLine("  ------------------------------------------------------------------------------");
        if (team.Coaches.Count == 0)
        {
            Console.WriteLine("  No coaches hired. See 'coachmarket' to hire one.");
            return;
        }
        Console.WriteLine($"    #  {"Specialty",-12}{"Name",-22}{"Ability",8}{"Wage/wk",12}");
        for (int i = 0; i < team.Coaches.Count; i++)
        {
            var c = team.Coaches[i];
            Console.WriteLine($"  {i,3}  {c.Specialty,-12}{c.Name,-22}{c.Ability,8}{TransferService.Money(c.Wage),12}");
        }
        Console.WriteLine("  Fire a coach with:  fire <#>");
    }

    public static void CoachMarket(IReadOnlyList<Coach> coaches)
    {
        Console.WriteLine();
        Console.WriteLine("  COACH MARKET");
        Console.WriteLine("  ------------------------------------------------------------------------------");
        Console.WriteLine($"    #  {"Specialty",-12}{"Name",-22}{"Ability",8}{"Wage/wk",12}");
        var ordered = coaches.Select((c, i) => (c, i)).OrderBy(x => x.c.Specialty).ThenByDescending(x => x.c.Ability);
        foreach (var (c, i) in ordered)
            Console.WriteLine($"  {i,3}  {c.Specialty,-12}{c.Name,-22}{c.Ability,8}{TransferService.Money(c.Wage),12}");
        Console.WriteLine("  Hire a coach with:  hire <#>   (coaches boost matchday ratings & matching training)");
    }

    public static void Plays(Team team)
    {
        var known = new HashSet<string>(team.Playbook.Select(p => p.Name));
        Console.WriteLine();
        Console.WriteLine($"  SET PLAYS — playbook {team.Playbook.Count} of {GameConstants.MaxPlaybook}");
        Console.WriteLine("  ------------------------------------------------------------------------------");
        Console.WriteLine($"    #  {"Area",-13}{"Name",-20}{"Diff",5}  {"Key",-8}{"Coach",-12}{"Fam",5}  Status");
        var catalog = SetPlayLibrary.All;
        for (int i = 0; i < catalog.Count; i++)
        {
            var p = catalog[i];
            bool inBook = known.Contains(p.Name);
            string status = inBook ? "IN PLAYBOOK" : "";
            int coachRating = team.CoachRating(p.Coaching);
            string coach = coachRating > 0 ? $"{p.Coaching}({coachRating})" : p.Coaching.ToString();
            string key = string.Join("/", p.KeyPositions.Select(pos => pos.ShortName()));
            string fam = inBook ? $"{team.GetFamiliarity(p.Name)}%" : "-";
            Console.WriteLine($"  {i,3}  {p.Area,-13}{p.Name,-20}{p.Difficulty,5}  {key,-8}{coach,-12}{fam,5}{status,14}");
        }
        Console.WriteLine("  ------------------------------------------------------------------------------");
        Console.WriteLine("  learn <#> to add a play, unlearn <#> to drop it. A matching coach runs it better;");
        Console.WriteLine("  familiarity grows with matching training and match reps — but a very well-known,");
        Console.WriteLine("  often-repeated play can get read and turned over by the opposition.");
    }

    public static void SeasonSummary(Season season, Team myClub)
    {
        var row = season.BuildTable().RowFor(myClub);
        Console.WriteLine();
        Console.WriteLine("======================================================================");
        Console.WriteLine($"  SEASON SUMMARY — {myClub.Name}");
        Console.WriteLine($"  Final position: {Ordinal(row.Position)} of {season.League.TeamCount}");
        Console.WriteLine($"  Record: {row.Won}W {row.Drawn}D {row.Lost}L   Points: {row.LeaguePoints}   Point diff: {(row.PointsDiff >= 0 ? "+" : "")}{row.PointsDiff}");
        Console.WriteLine($"  Verdict: {Verdict(row.Position, season.League.TeamCount)}");
        Console.WriteLine("======================================================================");
    }

    public static string Ordinal(int n)
    {
        if (n is >= 11 and <= 13) return $"{n}th";
        return (n % 10) switch { 1 => $"{n}st", 2 => $"{n}nd", 3 => $"{n}rd", _ => $"{n}th" };
    }

    public static string Verdict(int pos, int teams)
    {
        if (pos == 1) return "CHAMPIONS! Promotion secured. What a season.";
        if (pos <= Math.Max(2, teams / 4)) return "A promotion play-off place — a superb campaign.";
        if (pos <= teams / 2) return "A solid mid-table finish. Foundations to build on.";
        if (pos < teams) return "A tough season fighting the drop. Survival is survival.";
        return "Relegation. The board is not happy. Rebuild required.";
    }
}
