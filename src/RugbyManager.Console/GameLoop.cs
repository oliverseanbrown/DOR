using RugbyManager.Core.Competition;
using RugbyManager.Core.Finance;
using RugbyManager.Core.Match;
using RugbyManager.Core.Model;
using RugbyManager.Core.Persistence;
using RugbyManager.Core.Training;
using RugbyManager.Core.Transfers;
using RugbyManager.Core.Util;

namespace RugbyManager.ConsoleApp;

/// <summary>
/// The interactive career REPL: you manage one club through a season, setting tactics and
/// advancing round by round. Reads commands from stdin (so it also works with piped input).
/// </summary>
public sealed class GameLoop
{
    private Career _career;
    private MatchResult? _lastMyMatch;
    private WeeklyLedger? _lastLedger;

    public GameLoop(Career career) => _career = career;

    private Season Season => _career.Season;
    private Team My => _career.MyClub;

    public static string PathFor(string name)
        => Path.Combine("saves", (string.IsNullOrWhiteSpace(name) ? "career" : name) + ".json");

    public void Run()
    {
        Console.WriteLine();
        Console.WriteLine("======================================================================");
        Console.WriteLine($"  Season {_career.SeasonNumber} — {Season.League.Name}");
        Console.WriteLine($"  You are the manager of {My.Name}.");
        Console.WriteLine("======================================================================");
        Help();
        ShowNextFixture();

        while (true)
        {
            Console.Write("\n> ");
            string? line = Console.ReadLine();
            if (line is null) break; // EOF (e.g. piped input finished)

            line = line.Trim();
            if (line.Length == 0) continue;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string cmd = parts[0].ToLowerInvariant();
            string arg = parts.Length > 1 ? parts[1] : "";

            if (cmd is "quit" or "exit" or "q") break;
            Dispatch(cmd, arg);
        }

        Console.WriteLine("\nThanks for playing. Full-time.");
    }

    private void Dispatch(string cmd, string arg)
    {
        switch (cmd)
        {
            case "help" or "?": Help(); break;
            case "squad": Display.Squad(My); break;
            case "player": ShowPlayer(arg); break;
            case "table": Display.Table(Season.BuildTable(), My); break;
            case "fixtures": Display.MyFixtures(Season, My); break;
            case "tactics": Display.TacticsView(My); break;

            case "style": SetStyle(arg); break;
            case "breakdown": SetBreakdown(arg); break;
            case "defence" or "defense": SetDefence(arg); break;
            case "penalty": SetPenalty(arg); break;
            case "kick": SetKick(arg); break;

            case "next" or "advance" or "n": PlayRounds(1); break;
            case "sim": PlayRounds(int.TryParse(arg, out var k) ? k : 1); break;
            case "season" or "finish": PlayRounds(int.MaxValue); break;
            case "newseason" or "nextseason": NewSeason(); break;
            case "news": ShowNews(); break;
            case "commentary" or "match": ShowCommentary(); break;

            case "market": Display.Market(_career.Market, My, arg.Length > 0 ? arg : null); break;
            case "sign": SignPlayer(arg); break;
            case "sell": SellPlayer(arg); break;
            case "scout": ScoutPlayer(arg); break;
            case "budget": Console.WriteLine($"  Budget: {TransferService.Money(My.Money)}"); break;
            case "finances" or "money": ShowFinances(); break;
            case "coaches" or "staff": Display.Coaches(My); break;
            case "coachmarket": Display.CoachMarket(_career.CoachMarket); break;
            case "hire": HireCoach(arg); break;
            case "fire": FireCoach(arg); break;
            case "plays" or "playbook": Display.Plays(My); break;
            case "learn": LearnPlay(arg); break;
            case "unlearn": UnlearnPlay(arg); break;
            case "training" or "train": SetTraining(arg); break;

            case "save": SaveGame(arg); break;
            case "load": LoadGame(arg); break;

            default:
                Console.WriteLine($"  Unknown command '{cmd}'. Type 'help' for the list.");
                break;
        }
    }

    private void Help()
    {
        Console.WriteLine();
        Console.WriteLine("  Commands:");
        Console.WriteLine("    squad                 view your starting XV and ratings");
        Console.WriteLine("    player <#>            view a squad player's full stat block (see 'squad' for #)");
        Console.WriteLine("    tactics               view your current game plan");
        Console.WriteLine("    style/breakdown/defence/penalty/kick <value>   change a tactic");
        Console.WriteLine("    table                 current league table");
        Console.WriteLine("    fixtures              your fixtures & results");
        Console.WriteLine("    next                  play the next round");
        Console.WriteLine("    sim <n>               play the next n rounds");
        Console.WriteLine("    season                play the rest of the season");
        Console.WriteLine("    newseason             start next season (promotion/relegation, pre-season)");
        Console.WriteLine("    news                  recent headlines");
        Console.WriteLine("    commentary            replay your last match's commentary");
        Console.WriteLine("    market [pos]          view players you can sign (optionally by position)");
        Console.WriteLine("    sign <#>              sign a player from the market");
        Console.WriteLine("    scout <#>             commission a scouting report to reveal ability");
        Console.WriteLine("    sell <#>              sell a player from your squad (see 'squad')");
        Console.WriteLine("    budget                show your cash balance");
        Console.WriteLine("    finances              wage bill, income and last week's ledger");
        Console.WriteLine("    coaches               view your coaching staff");
        Console.WriteLine("    coachmarket           view coaches to hire; hire <#> / fire <#>");
        Console.WriteLine("    plays                 view the set-play library; learn <#> / unlearn <#>");
        Console.WriteLine("    training [focus]      set weekly training (rest|fitness|handling|setpiece|defence|kicking)");
        Console.WriteLine("    save [name]           save your career");
        Console.WriteLine("    load [name]           load a saved career");
        Console.WriteLine("    quit                  leave");
    }

    private Fixture? NextMyFixture()
        => Season.Fixtures
            .Where(f => !f.IsPlayed && (ReferenceEquals(f.Home, My) || ReferenceEquals(f.Away, My)))
            .OrderBy(f => f.Round)
            .FirstOrDefault();

    private void ShowNextFixture()
    {
        var fx = NextMyFixture();
        if (fx is null)
        {
            Console.WriteLine("\n  The season is complete. Type 'table' for the final standings.");
            return;
        }
        bool home = ReferenceEquals(fx.Home, My);
        var opp = home ? fx.Away : fx.Home;
        Console.WriteLine($"\n  Next up (Round {fx.Round + 1}): {(home ? "HOME" : "AWAY")} v {opp.Name}");
    }

    private void PlayRounds(int count)
    {
        if (Season.IsComplete)
        {
            Console.WriteLine("\n  The season is already over. Type 'table' for the final standings.");
            return;
        }

        for (int i = 0; i < count && !Season.IsComplete; i++)
        {
            int round = Season.NextRound;

            // Training week (deterministic per season+round), then the match, then fatigue.
            var trainingDice = new Dice(unchecked(Season.Seed * 31 + round * 17 + 101));
            var report = TrainingService.ApplyWeek(My, _career.Training, trainingDice);

            var injuredBefore = My.Squad.Where(p => !p.IsFit).ToHashSet();
            var played = Season.PlayNextRound();
            var newInjuries = My.Squad.Where(p => !p.IsFit && !injuredBefore.Contains(p)).ToList();

            var myFx = played.FirstOrDefault(f =>
                ReferenceEquals(f.Home, My) || ReferenceEquals(f.Away, My));
            bool homeGame = myFx is not null && ReferenceEquals(myFx.Home, My);
            if (myFx?.Result is { } r)
            {
                _lastMyMatch = r;
                TrainingService.DepleteAfterMatch(My);
                ReportMyMatch(myFx, r);
            }

            // Weekly finances (position drives the gate, so success pays).
            var position = Season.BuildTable().RowFor(My).Position;
            _lastLedger = FinanceService.ProcessWeek(My, homeGame, position, Season.League.TeamCount);

            if (count == 1)
            {
                string dev = report.Improvements > 0 ? $"{report.Improvements} attribute gains" : "no gains this week";
                Console.WriteLine($"  Training ({report.Focus}): {dev}. Squad condition {AverageCondition():0}%.");
                foreach (var g in report.Gains)
                    Console.WriteLine($"    {g.PlayerName}: {Display.Spaced(g.Attribute)} -> {g.NewValue}");
                foreach (var p in newInjuries)
                    Console.WriteLine($"  INJURY: {p.FullName} ({p.NaturalPosition.ShortName()}) out for {p.InjuredWeeksRemaining} week(s).");
                var l = _lastLedger;
                string sign = l.Net >= 0 ? "+" : "-";
                Console.WriteLine($"  Finances: {sign}{TransferService.Money(Math.Abs(l.Net))} this week (gate {TransferService.Money(l.Gate)}, wages {TransferService.Money(l.Wages)}). Balance {TransferService.Money(My.Money)}.");
                Display.RoundResults(round, played, My);
                var row = Season.BuildTable().RowFor(My);
                Console.WriteLine($"\n  {My.ShortName} are {Display.Ordinal(row.Position)} on {row.LeaguePoints} points.");
            }
        }

        if (Season.IsComplete)
        {
            Display.SeasonSummary(Season, My);
            Console.WriteLine("\n  Type 'newseason' to begin the next campaign.");
        }
        else if (count != 1)
            ShowNextFixture();
    }

    private void NewSeason()
    {
        if (!Season.IsComplete)
        {
            Console.WriteLine("  Finish the season first (type 'season').");
            return;
        }
        var (next, _) = SeasonTransition.Advance(_career, Season.League.TeamCount);
        _career = next;
        _lastMyMatch = null;
        _lastLedger = null;

        Console.WriteLine();
        Console.WriteLine("======================================================================");
        Console.WriteLine($"  A NEW SEASON — Season {_career.SeasonNumber}, {Season.League.Name}");
        Console.WriteLine("======================================================================");
        ShowNews();
        ShowNextFixture();
    }

    private void ShowNews()
    {
        Console.WriteLine();
        Console.WriteLine("  NEWS");
        Console.WriteLine("  ------------------------------------------------------------------------------");
        if (_career.News.Count == 0) { Console.WriteLine("  No headlines yet."); return; }
        foreach (var line in _career.News.TakeLast(12))
            Console.WriteLine($"  - {line}");
    }

    private void ReportMyMatch(Fixture fx, MatchResult r)
    {
        bool home = ReferenceEquals(fx.Home, My);
        int my = home ? r.HomeScore : r.AwayScore;
        int th = home ? r.AwayScore : r.HomeScore;
        var opp = home ? fx.Away : fx.Home;
        string outcome = my > th ? "WIN" : my < th ? "LOSS" : "DRAW";
        Console.WriteLine($"\n  >>> Round {fx.Round + 1}: {My.Name} {my}-{th} {opp.Name}  —  {outcome}");
        Console.WriteLine("      (type 'commentary' to replay the match)");
    }

    private void ShowCommentary()
    {
        if (_lastMyMatch is { } r) Display.Commentary(r);
        else Console.WriteLine("  No match played this session. Type 'next' to play a round.");
    }

    private double AverageCondition() => My.Squad.Average(p => p.Condition);

    private void ShowPlayer(string arg)
    {
        if (!int.TryParse(arg, out int i) || i < 0 || i >= My.Squad.Count)
        {
            Console.WriteLine("  Usage: player <#>  (see the numbers in 'squad')");
            return;
        }
        Display.PlayerDetail(My, My.Squad[i]);
    }

    private void ShowFinances()
    {
        Console.WriteLine();
        Console.WriteLine($"  FINANCES — {My.Name}");
        Console.WriteLine("  ------------------------------------------------------------------------------");
        Console.WriteLine($"    Cash balance      {TransferService.Money(My.Money)}");
        Console.WriteLine($"    Weekly wage bill  {TransferService.Money(FinanceService.WeeklyWageBill(My))}  ({My.Squad.Count} players)");
        if (_lastLedger is { } l)
        {
            Console.WriteLine($"    Last week         gate {TransferService.Money(l.Gate)} + sponsorship {TransferService.Money(l.Sponsorship)} - wages {TransferService.Money(l.Wages)}");
            Console.WriteLine($"                      net {(l.Net >= 0 ? "+" : "-")}{TransferService.Money(Math.Abs(l.Net))}");
        }
        if (My.Money < 0)
            Console.WriteLine("    WARNING: you are in the red. The board is watching the accounts.");
    }

    private void HireCoach(string arg)
    {
        if (!int.TryParse(arg, out int i) || i < 0 || i >= _career.CoachMarket.Count)
        {
            Console.WriteLine("  Usage: hire <#>  (see 'coachmarket')");
            return;
        }
        if (My.Coaches.Count >= GameConstants.MaxCoaches)
        {
            Console.WriteLine($"  Your staff is full (max {GameConstants.MaxCoaches}). Fire someone first.");
            return;
        }
        var coach = _career.CoachMarket[i];
        _career.CoachMarket.RemoveAt(i);
        My.Coaches.Add(coach);
        Console.WriteLine($"  Hired {coach.Name} ({coach.Specialty}, ability {coach.Ability}) at {TransferService.Money(coach.Wage)}/wk.");
    }

    private void FireCoach(string arg)
    {
        if (!int.TryParse(arg, out int i) || i < 0 || i >= My.Coaches.Count)
        {
            Console.WriteLine("  Usage: fire <#>  (see 'coaches')");
            return;
        }
        var coach = My.Coaches[i];
        My.Coaches.RemoveAt(i);
        _career.CoachMarket.Add(coach);
        Console.WriteLine($"  Released {coach.Name} ({coach.Specialty}).");
    }

    private void LearnPlay(string arg)
    {
        if (!int.TryParse(arg, out int i) || i < 0 || i >= SetPlayLibrary.All.Count)
        {
            Console.WriteLine("  Usage: learn <#>  (see 'plays')");
            return;
        }
        var play = SetPlayLibrary.All[i];
        if (My.Playbook.Any(p => p.Name == play.Name))
        {
            Console.WriteLine($"  {play.Name} is already in your playbook.");
            return;
        }
        if (My.Playbook.Count >= GameConstants.MaxPlaybook)
        {
            Console.WriteLine($"  Playbook is full (max {GameConstants.MaxPlaybook}). Drop one with 'unlearn'.");
            return;
        }
        My.Playbook.Add(play);
        Console.WriteLine($"  Added {play.Name} ({play.Area}) to the playbook.");
    }

    private void UnlearnPlay(string arg)
    {
        if (!int.TryParse(arg, out int i) || i < 0 || i >= SetPlayLibrary.All.Count)
        {
            Console.WriteLine("  Usage: unlearn <#>  (see 'plays')");
            return;
        }
        var play = SetPlayLibrary.All[i];
        var owned = My.Playbook.FirstOrDefault(p => p.Name == play.Name);
        if (owned is null) { Console.WriteLine($"  {play.Name} isn't in your playbook."); return; }
        My.Playbook.Remove(owned);
        Console.WriteLine($"  Dropped {play.Name} from the playbook.");
    }

    private void SetTraining(string arg)
    {
        if (arg.Length == 0)
        {
            Console.WriteLine($"  Training focus: {_career.Training}  (squad condition {AverageCondition():0}%)");
            Console.WriteLine("  Set with: training <rest|fitness|handling|setpiece|defence|kicking>");
            return;
        }
        var v = arg.ToLowerInvariant() switch
        {
            "rest" => TrainingFocus.Rest,
            "fitness" => TrainingFocus.Fitness,
            "handling" => TrainingFocus.Handling,
            "setpiece" or "set" => TrainingFocus.SetPiece,
            "defence" or "defense" => TrainingFocus.Defence,
            "kicking" => TrainingFocus.Kicking,
            _ => (TrainingFocus?)null,
        };
        if (v is null) { Console.WriteLine("  Usage: training <rest|fitness|handling|setpiece|defence|kicking>"); return; }
        _career.Training = v.Value;
        Console.WriteLine($"  Training focus set to {v.Value}. Applied from your next match week.");
    }

    private void SignPlayer(string arg)
    {
        if (!int.TryParse(arg, out int i) || i < 0 || i >= _career.Market.Available.Count)
        {
            Console.WriteLine("  Usage: sign <#>  (see the numbers in 'market')");
            return;
        }
        var target = _career.Market.Available[i];
        var result = TransferService.Sign(My, _career.Market, target);
        Console.WriteLine(result.Success ? $"  DONE: {result.Message}" : $"  (failed) {result.Message}");
        if (result.Success)
            Console.WriteLine($"  New budget: {TransferService.Money(My.Money)}");
    }

    private void ScoutPlayer(string arg)
    {
        if (!int.TryParse(arg, out int i) || i < 0 || i >= _career.Market.Available.Count)
        {
            Console.WriteLine("  Usage: scout <#>  (see 'market')");
            return;
        }
        if (My.Money < Scouting.ScoutCost)
        {
            Console.WriteLine($"  Not enough funds to scout ({TransferService.Money(Scouting.ScoutCost)}).");
            return;
        }
        var player = _career.Market.Available[i];
        if (Scouting.FullyKnown(player))
        {
            Console.WriteLine($"  {player.FullName} is already fully scouted.");
            return;
        }
        My.Money -= Scouting.ScoutCost;
        Scouting.Scout(player);
        var (lo, hi) = Scouting.OverallRange(player);
        string known = Scouting.FullyKnown(player) ? $"OVR {hi} (confirmed)" : $"OVR now estimated {lo}-{hi}";
        Console.WriteLine($"  Scouted {player.FullName}: {known}. Budget {TransferService.Money(My.Money)}.");
    }

    private void SellPlayer(string arg)
    {
        if (!int.TryParse(arg, out int i) || i < 0 || i >= My.Squad.Count)
        {
            Console.WriteLine("  Usage: sell <#>  (see the numbers in 'squad')");
            return;
        }
        var player = My.Squad[i];
        var result = TransferService.Sell(My, _career.Market, player);
        Console.WriteLine(result.Success ? $"  DONE: {result.Message}" : $"  (failed) {result.Message}");
        if (result.Success)
            Console.WriteLine($"  New budget: {TransferService.Money(My.Money)}");
    }

    private void SaveGame(string name)
    {
        try
        {
            string path = PathFor(name);
            CareerStore.Save(_career, path);
            Console.WriteLine($"  Career saved to {path}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Could not save: {ex.Message}");
        }
    }

    private void LoadGame(string name)
    {
        string path = PathFor(name);
        if (!File.Exists(path))
        {
            Console.WriteLine($"  No save found at {path}.");
            return;
        }
        try
        {
            _career = CareerStore.Load(path);
            _lastMyMatch = null;
            Console.WriteLine($"  Career loaded from {path}. You are managing {My.Name}.");
            ShowNextFixture();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Could not load: {ex.Message}");
        }
    }

    // --- Tactics setters ---

    private void SetStyle(string arg)
    {
        var v = arg.ToLowerInvariant() switch
        {
            "forwards" => PlayStyle.ForwardsOriented,
            "balanced" => PlayStyle.Balanced,
            "expansive" => PlayStyle.Expansive,
            "kicking" => PlayStyle.KickingGame,
            _ => (PlayStyle?)null,
        };
        if (v is null) { Console.WriteLine("  Usage: style <forwards|balanced|expansive|kicking>"); return; }
        My.Tactics = My.Tactics with { PlayStyle = v.Value };
        Console.WriteLine($"  Play style set to {v.Value}.");
    }

    private void SetBreakdown(string arg)
    {
        var v = arg.ToLowerInvariant() switch
        {
            "conservative" => BreakdownFocus.Conservative,
            "balanced" => BreakdownFocus.Balanced,
            "aggressive" => BreakdownFocus.Aggressive,
            _ => (BreakdownFocus?)null,
        };
        if (v is null) { Console.WriteLine("  Usage: breakdown <conservative|balanced|aggressive>"); return; }
        My.Tactics = My.Tactics with { BreakdownFocus = v.Value };
        Console.WriteLine($"  Breakdown focus set to {v.Value}.");
    }

    private void SetDefence(string arg)
    {
        var v = arg.ToLowerInvariant() switch
        {
            "drift" => DefensiveLine.Drift,
            "standard" => DefensiveLine.Standard,
            "rush" => DefensiveLine.Rush,
            _ => (DefensiveLine?)null,
        };
        if (v is null) { Console.WriteLine("  Usage: defence <drift|standard|rush>"); return; }
        My.Tactics = My.Tactics with { DefensiveLine = v.Value };
        Console.WriteLine($"  Defensive line set to {v.Value}.");
    }

    private void SetPenalty(string arg)
    {
        var v = arg.ToLowerInvariant() switch
        {
            "pragmatic" => PenaltyPhilosophy.Pragmatic,
            "balanced" => PenaltyPhilosophy.Balanced,
            "ambitious" => PenaltyPhilosophy.Ambitious,
            _ => (PenaltyPhilosophy?)null,
        };
        if (v is null) { Console.WriteLine("  Usage: penalty <pragmatic|balanced|ambitious>"); return; }
        My.Tactics = My.Tactics with { PenaltyPhilosophy = v.Value };
        Console.WriteLine($"  Penalty philosophy set to {v.Value}.");
    }

    private void SetKick(string arg)
    {
        if (!int.TryParse(arg, out int v)) { Console.WriteLine("  Usage: kick <0-100>"); return; }
        v = Math.Clamp(v, 0, 100);
        My.Tactics = My.Tactics with { KickingTendency = v };
        Console.WriteLine($"  Kicking tendency set to {v}.");
    }
}
