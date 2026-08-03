using RugbyManager.ConsoleApp;
using RugbyManager.Core.Competition;
using RugbyManager.Core.Generation;
using RugbyManager.Core.Match;
using RugbyManager.Core.Model;
using RugbyManager.Core.Persistence;

// ---------------------------------------------------------------------------
// RugbyManager — Phase 1.
//
//   dotnet run --project src/RugbyManager.Console                 -> INTERACTIVE career (default)
//   dotnet run --project src/RugbyManager.Console <seed> <teams>  -> interactive with a fixed seed / size
//   dotnet run --project src/RugbyManager.Console load [name]     -> continue a saved career
//   dotnet run --project src/RugbyManager.Console auto <seed> <n> -> auto-play a whole season & print it
//   dotnet run --project src/RugbyManager.Console match [seed]    -> single-match commentary
// ---------------------------------------------------------------------------

string mode = args.Length > 0 ? args[0].ToLowerInvariant() : "";

switch (mode)
{
    case "match":
        RunSingleMatch(ParseSeed(args, 1));
        break;
    case "auto":
        RunAutoSeason(ParseSeed(args, 1), ParseTeams(args, 2));
        break;
    case "load":
        LoadAndRun(args.Length > 1 ? args[1] : "career");
        break;
    default:
        RunInteractive(ParseSeed(args, 0), ParseTeams(args, 1));
        break;
}

// ---------------------------------------------------------------------------

static int ParseSeed(string[] args, int i)
    => args.Length > i && int.TryParse(args[i], out var s) ? s : Random.Shared.Next();

static int ParseTeams(string[] args, int i)
    => args.Length > i && int.TryParse(args[i], out var t) ? Math.Clamp(t, 4, 16) : 10;

static void RunInteractive(int seed, int teamCount)
{
    // Your club starts fair-to-middling (quality 64) in a mid-pyramid tier — room to climb.
    var league = LeagueGenerator.Generate(
        Pyramid.Name(Pyramid.StartingTier), teamCount, seed,
        firstClubQuality: 64, baseQuality: Pyramid.BaseQuality(Pyramid.StartingTier));
    league.Teams[0].Money = 150_000; // starting transfer budget
    var market = MarketGenerator.Generate(count: 40, seed: seed + 5000);
    var coachMarket = CoachGenerator.Generate(count: 20, seed: seed + 7000);
    var career = new Career(league.CreateSeason(seed), myTeamIndex: 0, market, coachMarket);
    new GameLoop(career).Run();
}

static void LoadAndRun(string name)
{
    string path = GameLoop.PathFor(name);
    if (!File.Exists(path))
    {
        Console.WriteLine($"No save found at {path}. Start a new career with: dotnet run --project src/RugbyManager.Console");
        return;
    }
    new GameLoop(CareerStore.Load(path)).Run();
}

static void RunAutoSeason(int seed, int teamCount)
{
    var league = LeagueGenerator.Generate("Regional Championship", teamCount, seed, firstClubQuality: 64);
    var myClub = league.Teams[0];
    var season = league.CreateSeason(seed);

    Console.WriteLine();
    Console.WriteLine($"  {league.Name} — {teamCount} clubs, {season.Rounds} rounds (seed {seed})");
    Console.WriteLine($"  Auto-playing the season for {myClub.Name}...");

    season.PlayAll();

    Display.Table(season.BuildTable(), myClub);
    Display.MyFixtures(season, myClub);
    Display.SeasonSummary(season, myClub);
}

static void RunSingleMatch(int seed)
{
    var home = SquadGenerator.Generate("Ashcombe RFC", "ASH", 68, new Tactics
    {
        PlayStyle = PlayStyle.ForwardsOriented,
        BreakdownFocus = BreakdownFocus.Aggressive,
        PenaltyPhilosophy = PenaltyPhilosophy.Pragmatic,
        KickingTendency = 55,
    }, seed + 1);

    var away = SquadGenerator.Generate("Riverside Rangers", "RIV", 66, new Tactics
    {
        PlayStyle = PlayStyle.Expansive,
        DefensiveLine = DefensiveLine.Rush,
        PenaltyPhilosophy = PenaltyPhilosophy.Ambitious,
        KickingTendency = 30,
    }, seed + 2);

    var result = new MatchEngine(home, away, seed).Simulate();
    Display.Commentary(result);
}
