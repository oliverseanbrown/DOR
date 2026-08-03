using Microsoft.JSInterop;
using RugbyManager.Core.Competition;
using RugbyManager.Core.Finance;
using RugbyManager.Core.Generation;
using RugbyManager.Core.Injuries;
using RugbyManager.Core.Match;
using RugbyManager.Core.Model;
using RugbyManager.Core.Persistence;
using RugbyManager.Core.Training;
using RugbyManager.Core.Transfers;
using RugbyManager.Core.Util;

namespace RugbyManager.Web.Services;

/// <summary>
/// Holds the current career and wraps <c>RugbyManager.Core</c> for the Blazor pages. This is
/// the web equivalent of the console's GameLoop — a thin coordinator, no simulation logic of
/// its own. Registered scoped, which in a WASM app means one instance per browser tab.
/// </summary>
public sealed class GameService
{
    private const string StorageKey = "rugbymanager.save";

    private readonly IJSRuntime _js;

    public Career? Career { get; private set; }
    public RoundReport? LastRound { get; private set; }
    public List<string> RecentNews { get; } = new();

    // --- Live Match Centre state ---
    public MatchEngine? LiveMatch { get; private set; }
    public Fixture? LiveFixture { get; private set; }
    private TrainingReport? _pendingTraining;
    private List<Player>? _injuredBeforeSnapshot;

    /// <summary>0 if the manager's club is home in the live fixture, 1 if away.</summary>
    public int MySide => LiveFixture is not null && ReferenceEquals(LiveFixture.Away, MyClub) ? 1 : 0;

    public event Action? Changed;

    public GameService(IJSRuntime js) => _js = js;

    public Team? MyClub => Career?.MyClub;
    public Season? Season => Career?.Season;

    private void NotifyChanged() => Changed?.Invoke();

    // ------------------------------------------------------------------
    //  Career lifecycle
    // ------------------------------------------------------------------

    public void StartNewCareer(int seed, int teamCount)
    {
        var league = LeagueGenerator.Generate(
            Pyramid.Name(Pyramid.StartingTier), teamCount, seed,
            firstClubQuality: 64, baseQuality: Pyramid.BaseQuality(Pyramid.StartingTier));
        league.Teams[0].Money = 150_000;

        var market = MarketGenerator.Generate(count: 40, seed: seed + 5000);
        var coachMarket = CoachGenerator.Generate(count: 20, seed: seed + 7000);
        Career = new Career(league.CreateSeason(seed), myTeamIndex: 0, market, coachMarket);
        LastRound = null;
        RecentNews.Clear();
        NotifyChanged();
    }

    public async Task<bool> TryLoadAsync()
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (string.IsNullOrEmpty(json)) return false;
            Career = CareerStore.Deserialize(json);
            LastRound = null;
            NotifyChanged();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task SaveAsync()
    {
        if (Career is null) return;
        var json = CareerStore.Serialize(Career);
        await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
    }

    public async Task<bool> HasSaveAsync()
    {
        var json = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        return !string.IsNullOrEmpty(json);
    }

    public async Task DeleteSaveAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
    }

    public void AbandonCareer()
    {
        Career = null;
        LastRound = null;
        RecentNews.Clear();
        NotifyChanged();
    }

    // ------------------------------------------------------------------
    //  Tactics
    // ------------------------------------------------------------------

    public void UpdateTactics(Tactics tactics)
    {
        if (MyClub is null) return;
        MyClub.Tactics = tactics;
        NotifyChanged();
    }

    // ------------------------------------------------------------------
    //  Transfers & scouting
    // ------------------------------------------------------------------

    public SignResult Sign(Player target)
    {
        var result = TransferService.Sign(MyClub!, Career!.Market, target);
        if (result.Success) NotifyChanged();
        return result;
    }

    public SignResult Sell(Player player)
    {
        var result = TransferService.Sell(MyClub!, Career!.Market, player);
        if (result.Success) NotifyChanged();
        return result;
    }

    public bool Scout(Player player, out string message)
    {
        if (Scouting.FullyKnown(player)) { message = $"{player.FullName} is already fully scouted."; return false; }
        if (MyClub!.Money < Scouting.ScoutCost) { message = "Not enough funds to scout."; return false; }
        MyClub.Money -= Scouting.ScoutCost;
        Scouting.Scout(player);
        var (lo, hi) = Scouting.OverallRange(player);
        message = Scouting.FullyKnown(player) ? $"OVR confirmed: {hi}." : $"OVR now estimated {lo}-{hi}.";
        NotifyChanged();
        return true;
    }

    // ------------------------------------------------------------------
    //  Coaches
    // ------------------------------------------------------------------

    public const int MaxCoaches = 4;
    public const int MaxPlaybook = 6;

    public bool HireCoach(Coach coach, out string message)
    {
        if (MyClub!.Coaches.Count >= MaxCoaches) { message = $"Staff is full (max {MaxCoaches})."; return false; }
        Career!.CoachMarket.Remove(coach);
        MyClub.Coaches.Add(coach);
        message = $"Hired {coach.Name} ({coach.Specialty}).";
        NotifyChanged();
        return true;
    }

    public void FireCoach(Coach coach)
    {
        MyClub!.Coaches.Remove(coach);
        Career!.CoachMarket.Add(coach);
        NotifyChanged();
    }

    // ------------------------------------------------------------------
    //  Set plays
    // ------------------------------------------------------------------

    public bool LearnPlay(SetPlay play, out string message)
    {
        if (MyClub!.Playbook.Any(p => p.Name == play.Name)) { message = "Already in the playbook."; return false; }
        if (MyClub.Playbook.Count >= MaxPlaybook) { message = $"Playbook is full (max {MaxPlaybook})."; return false; }
        MyClub.Playbook.Add(play);
        message = $"Added {play.Name}.";
        NotifyChanged();
        return true;
    }

    public void UnlearnPlay(SetPlay play)
    {
        var owned = MyClub!.Playbook.FirstOrDefault(p => p.Name == play.Name);
        if (owned is not null) MyClub.Playbook.Remove(owned);
        NotifyChanged();
    }

    // ------------------------------------------------------------------
    //  Training
    // ------------------------------------------------------------------

    public void SetTraining(TrainingFocus focus)
    {
        Career!.Training = focus;
        NotifyChanged();
    }

    // ------------------------------------------------------------------
    //  Playing rounds
    // ------------------------------------------------------------------

    public RoundReport? PlayNextRound()
    {
        var season = Season!;
        if (season.IsComplete) return null;

        int round = season.NextRound;
        var trainingDice = new Dice(unchecked(season.Seed * 31 + round * 17 + 101));
        var trainingReport = TrainingService.ApplyWeek(MyClub!, Career!.Training, trainingDice);

        var injuredBefore = MyClub!.Squad.Where(p => !p.IsFit).ToHashSet();
        var played = season.PlayNextRound();
        var newInjuries = MyClub.Squad.Where(p => !p.IsFit && !injuredBefore.Contains(p)).ToList();

        var myFx = played.FirstOrDefault(f => ReferenceEquals(f.Home, MyClub) || ReferenceEquals(f.Away, MyClub));
        bool home = myFx is not null && ReferenceEquals(myFx.Home, MyClub);
        if (myFx?.Result is not null)
            TrainingService.DepleteAfterMatch(MyClub);

        int position = season.BuildTable().RowFor(MyClub).Position;
        var ledger = FinanceService.ProcessWeek(MyClub, home, position, season.League.TeamCount);

        var report = new RoundReport
        {
            Round = round,
            Training = trainingReport,
            NewInjuries = newInjuries,
            MyFixture = myFx,
            MyResult = myFx?.Result,
            Home = home,
            Ledger = ledger,
            AllFixtures = played.ToList(),
            TablePosition = position,
            LeaguePoints = season.BuildTable().RowFor(MyClub).LeaguePoints,
        };
        LastRound = report;

        foreach (var p in newInjuries)
            RecentNews.Add($"INJURY: {p.FullName} ({p.NaturalPosition.ShortName()}) out for {p.InjuredWeeksRemaining} week(s).");

        NotifyChanged();
        return report;
    }

    public void PlayToSeasonEnd()
    {
        while (Season is { IsComplete: false }) PlayNextRound();
    }

    // ------------------------------------------------------------------
    //  Match Centre — a live, stepped, interactively-pausable version of PlayNextRound()
    //  scoped to just the manager's own fixture. Every other fixture in the round is still
    //  simulated instantly, exactly as PlayNextRound() would.
    // ------------------------------------------------------------------

    /// <summary>Starts the matchday: applies the week's training, fields the manual lineup (if
    /// one was set on the Team Sheet) or auto-picks, simulates the rest of the round instantly,
    /// and constructs a live, steppable engine for the manager's own fixture (null on a bye).</summary>
    public void StartMatchCentre()
    {
        var season = Season!;
        int round = season.NextRound;

        var trainingDice = new Dice(unchecked(season.Seed * 31 + round * 17 + 101));
        _pendingTraining = TrainingService.ApplyWeek(MyClub!, Career!.Training, trainingDice);
        _injuredBeforeSnapshot = MyClub!.Squad.Where(p => !p.IsFit).ToList();

        if (MyClub.HasManualLineup) MyClub.ValidateLineupFitness();
        else MyClub.SelectBestXV();
        MyClub.HasManualLineup = false; // consumed — next week defaults back to auto unless reset

        var myFx = season.FindFixtureFor(MyClub, round);
        season.BeginRound();
        season.PlayRoundExcept(myFx);

        LiveFixture = myFx;
        LiveMatch = myFx is not null ? new MatchEngine(myFx.Home, myFx.Away, season.MatchSeedFor(myFx)) : null;
        NotifyChanged();
    }

    /// <summary>Advance the live match by one unit of play. Returns true once it's complete
    /// (or there was no fixture to play — a bye).</summary>
    public bool StepLiveMatch()
    {
        if (LiveMatch is null) return true;
        bool done = LiveMatch.Step();
        NotifyChanged();
        return done;
    }

    /// <summary>Brings a substitute on for the manager's own club, mid-match.</summary>
    public void Substitute(Position pos, Player incoming)
    {
        if (LiveMatch is null) return;
        LiveMatch.Substitute(MySide, pos, incoming);
        NotifyChanged();
    }

    /// <summary>Logs a tactics change in the live commentary feed. The tactics themselves are
    /// applied directly to <c>MyClub.Tactics</c> by the caller (e.g. the Tactics selectors);
    /// this just tells the story.</summary>
    public void LogLiveTacticsChange()
    {
        LiveMatch?.LogTacticsChange(MySide);
        NotifyChanged();
    }

    /// <summary>Records the finished live match into the season, runs the same post-match
    /// bookkeeping PlayNextRound() would (injuries, depletion, finances, news), and returns the
    /// round report.</summary>
    public RoundReport FinishMatchCentre()
    {
        var season = Season!;
        int round = season.NextRound;
        MatchResult? result = null;
        bool homeGame = false;

        if (LiveMatch is not null && LiveFixture is not null)
        {
            result = LiveMatch.BuildResult();
            season.RecordFixtureResult(LiveFixture, result);
            homeGame = ReferenceEquals(LiveFixture.Home, MyClub);
            TrainingService.DepleteAfterMatch(MyClub!);
        }
        season.CompleteRound();

        var newInjuries = MyClub!.Squad
            .Where(p => !p.IsFit && !(_injuredBeforeSnapshot ?? new List<Player>()).Contains(p))
            .ToList();
        int position = season.BuildTable().RowFor(MyClub).Position;
        var ledger = FinanceService.ProcessWeek(MyClub, homeGame, position, season.League.TeamCount);

        var report = new RoundReport
        {
            Round = round,
            Training = _pendingTraining!,
            NewInjuries = newInjuries,
            MyFixture = LiveFixture,
            MyResult = result,
            Home = homeGame,
            Ledger = ledger,
            AllFixtures = season.FixturesInRound(round).ToList(),
            TablePosition = position,
            LeaguePoints = season.BuildTable().RowFor(MyClub).LeaguePoints,
        };
        LastRound = report;

        foreach (var p in newInjuries)
            RecentNews.Add($"INJURY: {p.FullName} ({p.NaturalPosition.ShortName()}) out for {p.InjuredWeeksRemaining} week(s).");

        LiveMatch = null;
        LiveFixture = null;
        _pendingTraining = null;
        _injuredBeforeSnapshot = null;
        NotifyChanged();
        return report;
    }

    public SeasonResult AdvanceSeason()
    {
        var (next, result) = SeasonTransition.Advance(Career!, Season!.League.TeamCount);
        Career = next;
        LastRound = null;
        RecentNews.Clear();
        RecentNews.AddRange(next.News);
        NotifyChanged();
        return result;
    }
}
