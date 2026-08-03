using RugbyManager.Core.Model;
using RugbyManager.Core.Util;
using static RugbyManager.Core.Model.Position;

namespace RugbyManager.Core.Match;

/// <summary>
/// A phase-based Rugby Union match simulator. One engine instance simulates one match.
///
/// The pitch is modelled as a single axis, <c>_ballX</c> in [0,100]: 0 is the home try
/// line, 100 the away try line. Home attacks toward 100, away toward 0. Play advances as a
/// sequence of "situations" (open play, scrum, lineout) whose contests are resolved from
/// the two sides' fatigue-aware ratings plus the seeded <see cref="Dice"/>. Every match is
/// therefore fully deterministic and replayable from its seed.
/// </summary>
public sealed class MatchEngine
{
    private enum Situation { OpenPlay, Scrum, Lineout }

    /// <summary>How the current spell of possession began — used only to flavour the very
    /// first carry of a fresh possession (e.g. "picks from the back of the scrum").</summary>
    private enum PossessionSource { Other, Scrum, Lineout }

    /// <summary>Where the match's <see cref="Step"/>-driven state machine currently is.</summary>
    private enum MatchStage { NotStarted, FirstHalf, SecondHalf, FullTime }

    /// <summary>Backs the ball can be moved on to beyond the scrum-half/fly-half axis.</summary>
    private static readonly Position[] WideBackPositions = { InsideCentre, OutsideCentre, LeftWing, RightWing };

    /// <summary>Forwards who recycle the ball with a short pop pass close to the breakdown.</summary>
    private static readonly Position[] ForwardPassPositions = { Hooker, Lock4, Lock5, BlindsideFlanker, OpensideFlanker, Number8 };

    private readonly MatchTeam[] _teams;
    private readonly Dice _dice;
    private readonly Commentary _c;
    private readonly List<MatchEvent> _events = new();
    private readonly Dictionary<Player, PlayerMatchStats> _playerStats = new();

    private double _clock;      // minutes elapsed
    private int _half = 1;      // 1 or 2
    private int _possession;    // 0 = home, 1 = away
    private double _ballX = 50; // 0..100
    private int _phase;         // rucks since possession started
    private Situation _situation = Situation.OpenPlay;
    private int _setOwner;      // team throwing the lineout / feeding the scrum
    private PossessionSource _source = PossessionSource.Other;
    private MatchStage _stage = MatchStage.NotStarted;
    private int _firstKicker;

    private MatchTeam Home => _teams[0];
    private MatchTeam Away => _teams[1];

    /// <summary>The commentary feed so far — grows with every <see cref="Step"/> call, so a UI
    /// can read it live rather than waiting for the match to finish.</summary>
    public IReadOnlyList<MatchEvent> Events => _events;

    /// <summary>True once full-time has been reached.</summary>
    public bool IsComplete => _stage == MatchStage.FullTime;

    public int HomeScore => Home.Score;
    public int AwayScore => Away.Score;

    /// <summary>Live box-score tallies for each side — updates as the match is stepped through.
    /// <see cref="MatchStats.PossessionPct"/>/<see cref="MatchStats.TerritoryPct"/> are only
    /// finalised at full time; read <see cref="MatchStats.PossessionMinutes"/>/
    /// <see cref="MatchStats.TerritoryMinutes"/> directly for a live percentage.</summary>
    public MatchStats HomeStats => Home.Stats;
    public MatchStats AwayStats => Away.Stats;

    /// <summary>Live, per-player contribution so far — updates as the match is stepped through.</summary>
    public IReadOnlyDictionary<Player, PlayerMatchStats> PlayerStats => _playerStats;

    public MatchEngine(Team home, Team away, int seed)
    {
        _teams = new[] { new MatchTeam(home), new MatchTeam(away) };
        _dice = new Dice(seed);
        _c = new Commentary(_dice);

        // Seed a zero-stat line for every starter so the full XV (both teams) shows up in
        // live/final player ratings even if a player never touches the ball or makes a tackle.
        foreach (var team in _teams)
            foreach (var p in team.Team.Players)
                StatsFor(p);
    }

    // --- Pitch geometry helpers ---
    private double DistToScore(int team) => team == 0 ? 100 - _ballX : _ballX;
    private double DistFromOwnLine(int team) => 100 - DistToScore(team);

    private void MoveBall(int team, double metres)
    {
        _ballX += team == 0 ? metres : -metres;
        _ballX = Math.Clamp(_ballX, 0, 100);
    }

    private void AdvanceClock(double minutes) => _clock += minutes;

    private void Log(MatchEventType type, string text, int? team = null, Drama drama = Drama.Routine, string? scorer = null)
        => _events.Add(new MatchEvent((int)Math.Round(_clock), type, text, team, Home.Score, Away.Score, drama, scorer));

    private string Name(int team) => _teams[team].Team.Name;
    private string Short(int team) => _teams[team].Team.ShortName;

    /// <summary>The live stat line for a player, created on first touch.</summary>
    private PlayerMatchStats StatsFor(Player p)
        => _playerStats.TryGetValue(p, out var s) ? s : _playerStats[p] = new PlayerMatchStats();

    /// <summary>Run the whole match in one call — the classic entry point, and exactly
    /// equivalent to calling <see cref="Step"/> in a loop until it returns true.</summary>
    public MatchResult Simulate()
    {
        while (!Step()) { }
        return BuildResult();
    }

    /// <summary>
    /// Advance the match by one unit of play — a single phase, scrum, or lineout resolution, or
    /// a half/full-time transition — and return whether the match is now complete. Because all
    /// match state lives on this instance, the caller is free to inspect <see cref="Events"/>
    /// and <see cref="PlayerStats"/> between calls, and to mutate the teams (tactics via
    /// <c>Team.Tactics</c>, personnel via <see cref="Substitute"/>) — changes take effect on the
    /// very next step. This is what powers a live, pausable Match Centre.
    /// </summary>
    public bool Step()
    {
        switch (_stage)
        {
            case MatchStage.NotStarted:
                Log(MatchEventType.Info, $"Welcome to {Name(0)} v {Name(1)}. Conditions are good and we're ready for kick-off.");
                _firstKicker = _dice.Chance(0.5) ? 0 : 1;
                Log(MatchEventType.Kickoff, $"{Name(_firstKicker)} get the match under way.");
                Kickoff(_firstKicker);
                _stage = MatchStage.FirstHalf;
                return false;

            case MatchStage.FirstHalf:
                if (HalfBoundaryReached(1))
                {
                    Log(MatchEventType.HalfTime, $"HALF-TIME: {Home.Team.ShortName} {Home.Score} - {Away.Score} {Away.Team.ShortName}.", drama: Drama.Highlight);
                    _half = 2;
                    Log(MatchEventType.Kickoff, $"{Name(1 - _firstKicker)} restart the second half.");
                    Kickoff(1 - _firstKicker);
                    _stage = MatchStage.SecondHalf;
                }
                else
                {
                    RunOnePhase();
                }
                return false;

            case MatchStage.SecondHalf:
                if (HalfBoundaryReached(2))
                {
                    Log(MatchEventType.FullTime, $"FULL-TIME: {Home.Team.ShortName} {Home.Score} - {Away.Score} {Away.Team.ShortName}.", drama: Drama.Highlight);
                    _stage = MatchStage.FullTime;
                    return true;
                }
                RunOnePhase();
                return false;

            default:
                return true;
        }
    }

    /// <summary>A half can only end at a natural stoppage: a set piece, or the start of a fresh
    /// possession — never mid-phase.</summary>
    private bool HalfBoundaryReached(int half)
    {
        double endAt = 40.0 * half;
        bool atStoppage = _situation != Situation.OpenPlay || _phase == 0;
        if (atStoppage && _clock >= endAt) return true;
        if (_clock >= endAt + 8) return true; // hard safety cap on injury time
        return false;
    }

    private void RunOnePhase()
    {
        switch (_situation)
        {
            case Situation.OpenPlay: OpenPlayPhase(); break;
            case Situation.Scrum: ResolveScrum(); break;
            case Situation.Lineout: ResolveLineout(); break;
        }
    }

    /// <summary>
    /// Bring a substitute on at a position, mid-match. Resets that position's fatigue to the
    /// incoming player's own condition (they're fresh, not inheriting whoever they replaced),
    /// and the change is live from the very next phase — <see cref="MatchTeam"/> always reads
    /// the current <c>Team.At(pos)</c>, never a cached reference.
    /// </summary>
    public void Substitute(int team, Position pos, Player incoming)
    {
        var outgoing = _teams[team].Team.At(pos);
        _teams[team].Team.SetStarter(pos, incoming);
        _teams[team].ResetPositionEnergy(pos);
        StatsFor(incoming);
        Log(MatchEventType.Substitution, _c.Substitution(outgoing.ShortName, incoming.ShortName, Name(team)), team);
    }

    /// <summary>
    /// Logs a tactical change for the commentary feed. The tactics themselves are just set
    /// directly on <c>Team.Tactics</c> by the caller — <see cref="MatchTeam"/> reads that
    /// property live every phase, so no engine-side plumbing is needed for it to take effect.
    /// </summary>
    public void LogTacticsChange(int team) => Log(MatchEventType.TacticsChange, _c.TacticsChange(Name(team)), team);

    private void Kickoff(int kicker)
    {
        int receiver = 1 - kicker;
        _possession = receiver;
        _ballX = receiver == 0 ? 40 : 60; // receiver gathers around their own 40m
        _phase = 0;
        _situation = Situation.OpenPlay;
        _source = PossessionSource.Other;
    }

    // ------------------------------------------------------------------
    //  Open play
    // ------------------------------------------------------------------
    private void OpenPlayPhase()
    {
        int att = _possession, def = 1 - att;
        MatchTeam A = _teams[att], D = _teams[def];

        double dt = Math.Max(0.2, _dice.Gaussian(0.55, 0.15));
        AdvanceClock(dt);
        A.Tick(dt);
        D.Tick(dt);
        TrackPossessionAndTerritory(att, dt);
        _phase++;

        // --- Decision: kick out of hand? ---
        double kickProb = A.Team.Tactics.KickingTendency / 100.0 * 0.35;
        double fromOwnLine = DistFromOwnLine(att);
        if (fromOwnLine < 30) kickProb += 0.30;       // pinned deep, clear the lines
        else if (fromOwnLine < 45) kickProb += 0.10;
        if (DistToScore(att) < 30) kickProb -= 0.25;  // in striking range, keep ball in hand
        if (_phase > 6) kickProb += 0.05;
        kickProb += A.Team.Tactics.PlayStyle switch
        {
            PlayStyle.KickingGame => 0.12,
            PlayStyle.Expansive => -0.10,
            _ => 0,
        };
        if (_dice.Chance(kickProb)) { ResolveKickFromHand(att); return; }

        // --- Drop goal opportunity (tight, late, in range) ---
        if (DistToScore(att) <= 30 && _phase >= 4 && _half == 2 && _clock >= 60)
        {
            int diff = A.Score - D.Score;
            if (diff <= 0 && diff >= -3 && _dice.Chance(0.06))
            {
                double boot = 0.7 * A.Effective(FlyHalf, a => a.Kicking) + 0.3 * A.Composure;
                var fh = A.Team.At(FlyHalf);
                StatsFor(fh).Kicks++;
                if (_dice.Chance(GoalProb(boot, DistToScore(att) + 8)))
                {
                    A.Stats.DropGoals++;
                    Log(MatchEventType.DropGoal, _c.DropGoal(fh.ShortName), att, Drama.Highlight, fh.ShortName);
                    Kickoff(def);
                }
                else
                {
                    Log(MatchEventType.DropGoalMissed, _c.DropGoalMissed(fh.ShortName), att, Drama.Highlight);
                    RestartFromMissedKick(def);
                }
                return;
            }
        }

        double atkR = A.AttackRating, defR = D.DefenceRating;
        double contestP = _dice.ContestProb(atkR, defR, 16);

        // --- Penalty conceded by the defence at the breakdown ---
        double pPenDef = 0.050 + Math.Clamp((58 - D.DisciplineRating) / 100.0 * 0.07, 0, 0.06);
        if (D.Team.Tactics.BreakdownFocus == BreakdownFocus.Aggressive) pPenDef += 0.020;
        pPenDef += _phase * 0.002;
        if (_dice.Chance(pPenDef))
        {
            D.Stats.PenaltiesConceded++;
            Log(MatchEventType.Penalty, _c.PenaltyConceded(Name(def)), att);
            ResolvePenalty(att);
            return;
        }

        // --- Penalty conceded by the attack (holding on / not releasing) ---
        double pPenAtt = 0.020 + Math.Clamp((58 - A.DisciplineRating) / 100.0 * 0.04, 0, 0.03);
        if (_dice.Chance(pPenAtt))
        {
            A.Stats.PenaltiesConceded++;
            Log(MatchEventType.Penalty, _c.PenaltyConceded(Name(att)), def);
            ResolvePenalty(def);
            return;
        }

        // --- Turnover won by the defence (jackal / counter-ruck) ---
        double pTO = 0.030 * (D.BreakdownRating / Math.Max(1.0, A.BreakdownRating));
        if (D.Team.Tactics.BreakdownFocus == BreakdownFocus.Aggressive) pTO += 0.010;
        pTO = Math.Clamp(pTO + _phase * 0.002, 0, 0.14);
        if (_dice.Chance(pTO))
        {
            D.Stats.TurnoversWon++;
            var winner = PickDefender(def);
            StatsFor(winner).TurnoversWon++;
            Log(MatchEventType.Turnover, _c.Turnover(Name(def)), def);
            _possession = def; _situation = Situation.OpenPlay; _phase = 0; _source = PossessionSource.Other;
            return;
        }

        // --- Knock-on / handling error ---
        double pKnock = 0.030 * (60.0 / Math.Max(1.0, A.HandlingRating));
        if (A.Team.Tactics.PlayStyle == PlayStyle.Expansive) pKnock += 0.015;
        pKnock += _phase * 0.0015;
        if (_dice.Chance(pKnock))
        {
            A.Stats.KnockOns++;
            var fumbler = PickRunner(att);
            StatsFor(fumbler).Errors++;
            Log(MatchEventType.KnockOn, _c.KnockOn(Name(att)), def);
            _setOwner = def; _situation = Situation.Scrum;
            return;
        }

        // --- Carry: gain (or lose) metres ---
        double lineBreakP = 0.035 + Math.Max(0, contestP - 0.5) * 0.10;
        if (D.Team.Tactics.DefensiveLine == DefensiveLine.Rush) lineBreakP += 0.02; // rush beaten = clean break

        // A team doesn't call a scripted shape on literally every phase near the line — only
        // some of them. This also keeps in-match "they've seen it too many times" predictability
        // rising at a realistic pace rather than maxing out inside a single attacking spell.
        var backlinePlay = DistToScore(att) <= 22 && _dice.Chance(0.3)
            ? TryUseSetPlay(att, def, SetPlayArea.BacklineMove)
            : default;
        if (backlinePlay.Outcome == SetPlayOutcome.Backfired)
        {
            HandleSetPlayBackfire(att, def, backlinePlay.Play!);
            return;
        }
        if (backlinePlay.Outcome == SetPlayOutcome.Attempted) lineBreakP += backlinePlay.Bonus;

        bool lineBreak = _dice.Chance(lineBreakP);
        if (backlinePlay.Outcome == SetPlayOutcome.Attempted)
            RecordPlayFeedback(att, backlinePlay.Play!.Name, success: lineBreak);

        // Every passage of open play touches the base (scrum-half) and the orchestrator
        // (fly-half); wider phases carry the ball on down the backline to a centre or wing,
        // and tighter phases see the forwards recycle it among themselves close to the ruck.
        StatsFor(A.Team.At(ScrumHalf)).Passes++;
        StatsFor(A.Team.At(FlyHalf)).Passes++;
        double widePassP = 0.40 + (A.Team.Tactics.PlayStyle == PlayStyle.Expansive ? 0.15 : 0);
        if (_dice.Chance(widePassP))
            StatsFor(A.Team.At(_dice.Pick(WideBackPositions))).Passes++;
        double forwardPassP = 0.30 + (A.Team.Tactics.PlayStyle == PlayStyle.ForwardsOriented ? 0.15 : 0);
        if (_dice.Chance(forwardPassP))
            StatsFor(A.Team.At(_dice.Pick(ForwardPassPositions))).Passes++;

        Player? runner = null;
        double gain;
        if (lineBreak)
        {
            A.Stats.LineBreaks++;
            runner = PickRunner(att);
            gain = _dice.Gaussian(28, 10);
        }
        else
        {
            gain = _dice.Gaussian(3.0 + (atkR - defR) / 12.0, 3.0);
            if (A.Team.Tactics.PlayStyle == PlayStyle.Expansive) gain += _dice.Gaussian(0.5, 1.5);
        }
        gain = Math.Max(-3, gain);

        double distAtBreak = DistToScore(att);
        if (lineBreak && distAtBreak - gain <= 0)
        {
            // The break threatens to go the distance. Whether it actually does — and how —
            // is resolved as its own cinematic sequence; it fully owns ball placement from here.
            ResolveLineBreakToTry(att, def, runner!, distAtBreak);
            return;
        }

        MoveBall(att, gain);

        if (lineBreak)
        {
            StatsFor(runner!).Carries++;
            StatsFor(runner!).MetresGained += gain;
            Log(MatchEventType.LineBreak, _c.LineBreak(runner!.ShortName, Name(att)), att, Drama.Highlight);
        }
        else
        {
            // A routine carry: credit the ball-carrier and whoever made the tackle, so stats
            // accumulate realistically across the whole XV, not just the highlight moments.
            var carrier = PickRunner(att);
            StatsFor(carrier).Carries++;
            StatsFor(carrier).MetresGained += Math.Max(0, gain);
            var tackler = PickDefender(def);
            StatsFor(tackler).Tackles++;

            if (DistToScore(att) <= 0) ScoreTry(att, carrier, wide: false);
        }
        // else: possession & situation unchanged — another phase follows.
    }

    /// <summary>Inside this range of the line, a break leaves no time for anyone to cover it —
    /// the carrier just scores. Beyond it, a last defender is realistically still in the picture.</summary>
    private const double NoCoverDistance = 15.0;

    /// <summary>
    /// Tells a length-of-field try as a short beat-by-beat story: how the carry started, the
    /// tackle that was missed, then — if there's a last defender to beat — a real contest over
    /// how the carrier tries to beat them (power, footwork, guile, boot, or a pass), which can
    /// fail. Every beat uses the same ball-carrier so the story stays coherent.
    /// </summary>
    private void ResolveLineBreakToTry(int att, int def, Player carrier, double distAtBreak)
    {
        string origin = _source switch
        {
            PossessionSource.Scrum => _c.OriginScrumPick(carrier.ShortName, Name(att)),
            PossessionSource.Lineout => _c.OriginLineoutCarry(carrier.ShortName, Name(att)),
            _ => _c.OriginGeneralCarry(carrier.ShortName, Name(att)),
        };
        Log(MatchEventType.Carry, origin, att, Drama.Highlight);
        StatsFor(carrier).Carries++;
        StatsFor(carrier).MetresGained += distAtBreak; // covers the ground from the break to the line

        var missedTackler = PickDefender(def);
        StatsFor(missedTackler).Errors++;
        Log(MatchEventType.MissedTackle,
            _c.MissedTackleBreak(missedTackler.NaturalPosition.ShortName(), missedTackler.ShortName, Name(def), carrier.ShortName),
            def, Drama.Highlight);

        if (distAtBreak <= NoCoverDistance)
        {
            // Broke right on the line — nobody has time to get across and cover it.
            SetBallByDistToScore(att, 0);
            ScoreTry(att, carrier, wide: false, breakaway: true, line: _c.NoCoverFinish(carrier.ShortName, Name(att)));
            return;
        }

        // A last defender is still in this. The carrier reaches for whatever suits their game;
        // the defender gets a real chance to stop it, based on both players' attributes.
        var lastDefender = PickDefender(def);
        var move = PickMove(carrier);
        double attackScore = AttackMoveScore(carrier.Attributes, move);
        double defendScore = DefendMoveScore(lastDefender.Attributes, move);
        // +8 reflects the carrier's momentum and space advantage from already being clean through —
        // favoured, but a genuinely good last-ditch defender can still make the covering play.
        bool beatsDefender = _dice.Chance(_dice.ContestProb(attackScore + 8, defendScore, 14));

        if (beatsDefender)
        {
            SetBallByDistToScore(att, 0);
            if (move == BeatMove.OffloadPass)
            {
                var support = PickSupportRunner(att, carrier);
                StatsFor(carrier).Passes++;
                StatsFor(support).DefendersBeaten++;
                Log(MatchEventType.LineBreak, _c.OffloadBeat(carrier.ShortName, support.ShortName), att, Drama.Highlight);
                ScoreTry(att, support, wide: false, breakaway: true, line: _c.BreakawayTryVia(move, support.ShortName, Name(att)));
            }
            else
            {
                StatsFor(carrier).DefendersBeaten++;
                Log(MatchEventType.LineBreak, _c.BeatMoveSuccess(move, carrier.ShortName, Short(def)), att, Drama.Highlight);
                ScoreTry(att, carrier, wide: false, breakaway: true, line: _c.BreakawayTryVia(move, carrier.ShortName, Name(att)));
            }
            return;
        }

        // The defender wins the contest. What that means depends on what was attempted.
        switch (move)
        {
            case BeatMove.ChipAndChase:
                // A poor chip is just a gift — the defence gathers it and it's their ball now.
                StatsFor(lastDefender).TurnoversWon++;
                Log(MatchEventType.Turnover, _c.ChipAndChaseFail(lastDefender.ShortName, Name(def)), def, Drama.Highlight);
                _teams[def].Stats.TurnoversWon++;
                _possession = def; _situation = Situation.OpenPlay; _phase = 0; _source = PossessionSource.Other;
                break;

            case BeatMove.OffloadPass:
                // A mishandled offload in the tackle is a knock-on against the attacking side.
                StatsFor(carrier).Errors++;
                Log(MatchEventType.KnockOn, _c.OffloadFail(lastDefender.ShortName, Name(att)), att, Drama.Highlight);
                _teams[att].Stats.KnockOns++;
                _setOwner = def; _situation = Situation.Scrum;
                break;

            default:
                // A try-saving tackle — held up just short. Same team, right on the line, another phase follows.
                StatsFor(lastDefender).Tackles++;
                Log(MatchEventType.LineBreak, _c.BeatMoveFail(move, lastDefender.ShortName, Name(def)), def, Drama.Highlight);
                SetBallByDistToScore(att, Math.Clamp(_dice.Gaussian(3, 1.5), 0.5, 8));
                break;
        }
    }

    private static readonly BeatMove[] AllBeatMoves = Enum.GetValues<BeatMove>();

    /// <summary>How well a carry-move suits this player, from their attributes.</summary>
    private static double AttackMoveScore(PlayerAttributes a, BeatMove move) => move switch
    {
        BeatMove.RunThrough => a.Strength * 0.7 + a.Aggression * 0.3,
        BeatMove.HandOff => a.Strength * 0.6 + a.Agility * 0.4,
        BeatMove.Sidestep => a.Agility * 0.7 + a.Acceleration * 0.3,
        BeatMove.Goosestep => a.Acceleration * 0.6 + a.Pace * 0.4,
        BeatMove.Dummy => a.DecisionMaking * 0.6 + a.Composure * 0.4,
        BeatMove.ChipAndChase => a.Kicking * 0.5 + a.Pace * 0.5,
        BeatMove.OffloadPass => a.Passing * 0.6 + a.Vision * 0.4,
        _ => 50,
    };

    /// <summary>How well this defender counters a given move.</summary>
    private static double DefendMoveScore(PlayerAttributes a, BeatMove move) => move switch
    {
        BeatMove.RunThrough => a.Strength * 0.6 + a.Tackling * 0.4,
        BeatMove.HandOff => a.Tackling * 0.5 + a.Positioning * 0.5,
        BeatMove.Sidestep => a.Positioning * 0.5 + a.Tackling * 0.5,
        BeatMove.Goosestep => a.Pace * 0.6 + a.Positioning * 0.4,
        BeatMove.Dummy => a.DecisionMaking * 0.6 + a.Positioning * 0.4,
        BeatMove.ChipAndChase => a.Pace * 0.6 + a.Positioning * 0.4,
        BeatMove.OffloadPass => a.Positioning * 0.6 + a.Tackling * 0.4,
        _ => 50,
    };

    /// <summary>
    /// The carrier's go-to technique — whichever move best fits their attributes, with a touch
    /// of randomness so the same player doesn't attempt the identical thing every single time.
    /// </summary>
    private BeatMove PickMove(Player carrier)
    {
        var best = AllBeatMoves[0];
        double bestScore = double.MinValue;
        foreach (var move in AllBeatMoves)
        {
            double score = AttackMoveScore(carrier.Attributes, move) + _dice.Gaussian(0, 4);
            if (score > bestScore) { bestScore = score; best = move; }
        }
        return best;
    }

    /// <summary>A teammate to offload to — anyone but the ball-carrier themselves.</summary>
    private Player PickSupportRunner(int team, Player exclude)
    {
        var t = _teams[team].Team;
        var candidates = new[]
        {
            t.At(LeftWing), t.At(RightWing), t.At(Fullback),
            t.At(OutsideCentre), t.At(InsideCentre), t.At(FlyHalf), t.At(Number8),
        }.Where(p => p != exclude).ToArray();
        return candidates.Length > 0 ? _dice.Pick(candidates) : exclude;
    }

    private void ResolveKickFromHand(int att)
    {
        int def = 1 - att;
        MoveBall(att, _dice.Gaussian(34, 7));

        double r = _dice.NextDouble();
        if (r < 0.08)
        {
            Log(MatchEventType.KickTerritory, _c.KickReclaim(Name(att)), att);
            _possession = att; _situation = Situation.OpenPlay; _phase = 0; _source = PossessionSource.Other;
        }
        else if (r < 0.62)
        {
            Log(MatchEventType.KickToTouch, _c.KickToTouch(Name(att)), att);
            _setOwner = def; _situation = Situation.Lineout; // non-kicking side throws in
        }
        else
        {
            Log(MatchEventType.KickTerritory, _c.KickTerritory(Name(att)), att);
            _possession = def; _situation = Situation.OpenPlay; _phase = 0; _source = PossessionSource.Other; // contestable, opp counters
        }
    }

    // ------------------------------------------------------------------
    //  Set pieces
    // ------------------------------------------------------------------
    private void ResolveScrum()
    {
        int owner = _setOwner, opp = 1 - owner;
        AdvanceClock(Math.Max(0.4, _dice.Gaussian(1.1, 0.3)));
        double p = _dice.ContestProb(_teams[owner].ScrumRating, _teams[opp].ScrumRating, 12);

        if (_dice.Chance(0.14 * Math.Max(0, 2 * p - 1) + 0.02))
        {
            _teams[opp].Stats.PenaltiesConceded++;
            Log(MatchEventType.Penalty, _c.ScrumPenalty(Name(owner)), owner);
            ResolvePenalty(owner);
            return;
        }
        if (_dice.Chance(0.05 * (1 - p)))
        {
            var hooker = _teams[opp].Team.At(Hooker);
            StatsFor(hooker).TurnoversWon++;
            Log(MatchEventType.Turnover, _c.AgainstHead(Name(opp)), opp);
            _possession = opp; _situation = Situation.OpenPlay; _phase = 0; _source = PossessionSource.Scrum;
            return;
        }
        Log(MatchEventType.Scrum, _c.Scrum(Name(owner)), owner);
        _possession = owner; _situation = Situation.OpenPlay; _phase = 0; _source = PossessionSource.Scrum;

        // A rehearsed scrum move close to the line can score straight off the set piece.
        // Only roll when a scrum play actually exists, so teams without one don't perturb the RNG.
        if (DistToScore(owner) > 22) return;

        var scrumPlay = TryUseSetPlay(owner, opp, SetPlayArea.ScrumPlay);
        if (scrumPlay.Outcome == SetPlayOutcome.Backfired)
        {
            HandleSetPlayBackfire(owner, opp, scrumPlay.Play!);
            return;
        }
        if (scrumPlay.Outcome != SetPlayOutcome.Attempted) return;

        bool tries = _dice.Chance(scrumPlay.Bonus);
        RecordPlayFeedback(owner, scrumPlay.Play!.Name, success: tries);
        if (tries)
        {
            _ballX = owner == 0 ? 100 : 0;
            ScoreTry(owner, PickForward(owner), wide: false);
        }
    }

    private void ResolveLineout()
    {
        int owner = _setOwner, opp = 1 - owner;
        AdvanceClock(Math.Max(0.3, _dice.Gaussian(0.7, 0.2)));
        double p = _dice.ContestProb(_teams[owner].LineoutRating, _teams[opp].LineoutRating, 12);

        if (_dice.Chance(0.10 * (1 - p) + 0.02))
        {
            _teams[opp].Stats.TurnoversWon++;
            var jumper = new[] { Lock4, Lock5, BlindsideFlanker, Number8 }
                .Select(pos => _teams[opp].Team.At(pos)).MaxBy(x => x.Attributes.Lineout)!;
            StatsFor(jumper).TurnoversWon++;
            Log(MatchEventType.LineoutSteal, _c.LineoutSteal(Name(opp)), opp);
            _possession = opp; _situation = Situation.OpenPlay; _phase = 0; _source = PossessionSource.Other;
            return;
        }

        Log(MatchEventType.Lineout, _c.Lineout(Name(owner)), owner);

        // Attacking lineout close in: back the maul, with a real shot at a pushover try.
        bool forwards = _teams[owner].Team.Tactics.PlayStyle == PlayStyle.ForwardsOriented;
        if (DistToScore(owner) <= 8)
        {
            var lineoutPlay = TryUseSetPlay(owner, opp, SetPlayArea.LineoutPlay);
            if (lineoutPlay.Outcome == SetPlayOutcome.Backfired)
            {
                HandleSetPlayBackfire(owner, opp, lineoutPlay.Play!);
                return;
            }

            double maulTryP = 0.32 + (forwards ? 0.15 : 0) + Math.Max(0, p - 0.5) * 0.3;
            if (lineoutPlay.Outcome == SetPlayOutcome.Attempted) maulTryP += lineoutPlay.Bonus;

            bool scoresMaul = _dice.Chance(maulTryP);
            if (lineoutPlay.Outcome == SetPlayOutcome.Attempted)
                RecordPlayFeedback(owner, lineoutPlay.Play!.Name, success: scoresMaul);

            if (scoresMaul)
            {
                ScoreTry(owner, PickForward(owner), wide: false, viaMaul: true);
                return;
            }
            MoveBall(owner, _dice.Gaussian(3, 1.5)); // maul edges forward
        }
        else if (DistToScore(owner) <= 22 && forwards)
        {
            MoveBall(owner, _dice.Gaussian(4, 2)); // rolling maul gains ground
        }

        _possession = owner; _situation = Situation.OpenPlay; _phase = 0; _source = PossessionSource.Lineout;
    }

    // ------------------------------------------------------------------
    //  Scoring
    // ------------------------------------------------------------------
    private void ScoreTry(int team, Player scorer, bool wide, bool viaMaul = false, bool breakaway = false, string? line = null)
    {
        _teams[team].Stats.Tries++;
        StatsFor(scorer).Tries++;
        string text = line
            ?? (breakaway ? _c.BreakawayTry(scorer.ShortName, Name(team))
            : viaMaul ? _c.MaulTry(Name(team))
            : _c.TryScored(scorer.ShortName, Name(team)));
        Log(MatchEventType.Try, text, team, Drama.Highlight, scorer.ShortName);
        ResolveConversion(team, wide || viaMaul);
        Kickoff(1 - team); // the conceding side restarts
    }

    private void ResolveConversion(int team, bool wide)
    {
        var kicker = _teams[team].Team.GoalKicker;
        StatsFor(kicker).Kicks++;
        double dist = wide ? _dice.Gaussian(34, 4) : _dice.Gaussian(20, 4);
        if (_dice.Chance(GoalProb(_teams[team].GoalKickRating, dist)))
        {
            _teams[team].Stats.Conversions++;
            StatsFor(kicker).Conversions++;
            Log(MatchEventType.Conversion, _c.Conversion(kicker.ShortName), team, Drama.Highlight, kicker.ShortName);
        }
        else
        {
            Log(MatchEventType.ConversionMissed, _c.ConversionMissed(kicker.ShortName), team, Drama.Highlight);
        }
    }

    private void ResolvePenalty(int team)
    {
        int opp = 1 - team;
        double dist = DistToScore(team);
        bool kickable = dist <= 38;
        var phil = _teams[team].Team.Tactics.PenaltyPhilosophy;
        int diff = _teams[team].Score - _teams[opp].Score;
        bool chasingTry = _half == 2 && _clock >= 65 && diff < 0 && diff >= -7;

        bool goForCorner =
            (phil == PenaltyPhilosophy.Ambitious && dist <= 25) ||
            (phil == PenaltyPhilosophy.Balanced && dist <= 12) ||
            (chasingTry && dist <= 30);

        if (kickable && !goForCorner)
        {
            var kicker = _teams[team].Team.GoalKicker;
            StatsFor(kicker).Kicks++;
            if (_dice.Chance(GoalProb(_teams[team].GoalKickRating, dist + 7)))
            {
                _teams[team].Stats.PenaltyGoals++;
                StatsFor(kicker).PenaltyGoals++;
                Log(MatchEventType.PenaltyGoal, _c.PenaltyGoal(kicker.ShortName), team, Drama.Highlight, kicker.ShortName);
                Kickoff(opp);
            }
            else
            {
                Log(MatchEventType.PenaltyMissed, _c.PenaltyMissed(kicker.ShortName), team, Drama.Highlight);
                RestartFromMissedKick(opp);
            }
            return;
        }

        // Kick to touch: keep the throw, set up an attacking lineout.
        Log(MatchEventType.KickToTouch, _c.KickToTouch(Name(team)), team);
        double newDist = dist <= 50 ? Math.Clamp(_dice.Gaussian(6, 3), 2, 15) : Math.Max(6, dist - 40);
        SetBallByDistToScore(team, newDist);
        _setOwner = team; _situation = Situation.Lineout;
    }

    /// <summary>After a missed shot at goal the defending side clears from their 22.</summary>
    private void RestartFromMissedKick(int team)
    {
        _possession = team;
        SetBallByDistToScore(team, 78); // roughly their own 22
        _situation = Situation.OpenPlay; _phase = 0; _source = PossessionSource.Other;
    }

    private void SetBallByDistToScore(int team, double dist)
        => _ballX = team == 0 ? 100 - dist : dist;

    // ------------------------------------------------------------------
    //  Set plays: proficiency, predictability, and the read/backfire mechanic
    // ------------------------------------------------------------------

    private enum SetPlayOutcome { NotAttempted, Read, Backfired, Attempted }

    private readonly struct SetPlayAttempt
    {
        public SetPlayOutcome Outcome { get; init; }
        public SetPlay? Play { get; init; }
        public double Bonus { get; init; } // execution*payoff, meaningful only when Attempted
    }

    /// <summary>How many times each (team, play) pairing has been attempted so far this match —
    /// the in-game "they've seen this three times already" half of the read mechanic.</summary>
    private readonly Dictionary<(int Team, string Play), int> _playAttemptsThisMatch = new();

    /// <summary>
    /// Attempts a rehearsed play in the given area, if the team has one. Selection favours
    /// plays the team trusts more (higher familiarity) without being exclusive to it, so a
    /// varied playbook stays less predictable than a one-trick one. The defence gets a real
    /// chance to have scouted it — driven by how polished/well-worn the play is and how many
    /// times they've already seen it tonight — and reading it can outright backfire on the attack.
    /// </summary>
    private SetPlayAttempt TryUseSetPlay(int team, int opp, SetPlayArea area)
    {
        var t = _teams[team].Team;
        var options = t.Playbook.Where(p => p.Area == area).ToList();
        if (options.Count == 0) return new SetPlayAttempt { Outcome = SetPlayOutcome.NotAttempted };

        var play = PickWeightedPlay(t, options);

        var key = (team, play.Name);
        _playAttemptsThisMatch.TryGetValue(key, out int usedSoFar);
        _playAttemptsThisMatch[key] = usedSoFar + 1;

        int familiarity = t.GetFamiliarity(play.Name);
        int coach = t.CoachRating(play.Coaching);
        double playerSkill = play.KeyPositions.Length > 0
            ? play.KeyPositions.Select(pos => KeyPlayerScore(t.At(pos), play.Area)).Average()
            : 50;

        double execution = Math.Clamp(
            0.22 + coach * 0.0035 + familiarity * 0.003 + playerSkill * 0.003 - play.Difficulty * 0.003,
            0.08, 0.95);
        double payoff = play.Difficulty / 100.0 * 0.25;
        double bonus = execution * payoff;

        // Can the defence see it coming? Better-drilled plays leave more tape to study; running
        // it repeatedly tonight makes it obvious; a good defence coach helps read it either way.
        int defCoach = _teams[opp].Team.CoachRating(CoachSpecialty.Defence);
        double readChance = Math.Clamp(
            0.05 + familiarity / 100.0 * 0.15 + Math.Min(usedSoFar, 4) * 0.09 + defCoach / 100.0 * 0.10,
            0.05, 0.70);

        if (_dice.Chance(readChance))
        {
            bool backfires = _dice.Chance(0.45);
            return new SetPlayAttempt { Outcome = backfires ? SetPlayOutcome.Backfired : SetPlayOutcome.Read, Play = play };
        }

        return new SetPlayAttempt { Outcome = SetPlayOutcome.Attempted, Play = play, Bonus = bonus };
    }

    /// <summary>Logs a read-and-shut-down play as a turnover and hands the defence the ball.</summary>
    private void HandleSetPlayBackfire(int team, int opp, SetPlay play)
    {
        var reader = PickDefender(opp);
        StatsFor(reader).TurnoversWon++;
        Log(MatchEventType.Turnover, _c.SetPlayRead(play.Name, reader.ShortName, Name(opp)), opp, Drama.Highlight);
        _teams[opp].Stats.TurnoversWon++;
        RecordPlayFeedback(team, play.Name, success: false);
        _possession = opp; _situation = Situation.OpenPlay; _phase = 0; _source = PossessionSource.Other;
    }

    /// <summary>Match usage feeds back into how well-drilled a play becomes — success reinforces
    /// it more than failure, but even a failed attempt is still a rep.</summary>
    private void RecordPlayFeedback(int team, string playName, bool success)
        => _teams[team].Team.BumpFamiliarity(playName, success ? 2 : 1);

    /// <summary>
    /// Picks among a team's plays in an area, weighted toward the ones it trusts more —
    /// but not exclusively, so a team with a fuller playbook naturally spreads its attempts
    /// across more plays and stays less predictable than one that always reaches for its best.
    /// </summary>
    private SetPlay PickWeightedPlay(Team t, List<SetPlay> options)
    {
        if (options.Count == 1) return options[0];
        double totalWeight = options.Sum(p => t.GetFamiliarity(p.Name) + 10);
        double roll = _dice.NextDouble() * totalWeight;
        double cumulative = 0;
        foreach (var play in options)
        {
            cumulative += t.GetFamiliarity(play.Name) + 10;
            if (roll <= cumulative) return play;
        }
        return options[^1];
    }

    /// <summary>The attribute blend that matters for executing a play in this area — this is
    /// "how good they are" at it, drawn straight from the specific key-position players.</summary>
    private static double KeyPlayerScore(Player p, SetPlayArea area) => area switch
    {
        SetPlayArea.BacklineMove => (p.Attributes.Passing + p.Attributes.DecisionMaking + p.Attributes.Handling) / 3.0,
        SetPlayArea.LineoutPlay => (p.Attributes.Lineout + p.Attributes.Strength) / 2.0,
        SetPlayArea.ScrumPlay => (p.Attributes.Scrummaging + p.Attributes.Strength + p.Attributes.DecisionMaking) / 3.0,
        _ => 50,
    };

    /// <summary>Goal-kicking success from a nominal distance, given the kicker's rating.</summary>
    private static double GoalProb(double rating, double dist)
    {
        double baseP = rating / 100.0;
        double rangePenalty = Math.Clamp((dist - 18) / 40.0, 0, 0.6);
        return Math.Clamp(baseP * 1.05 - rangePenalty + 0.05, 0.03, 0.98);
    }

    // ------------------------------------------------------------------
    //  Commentary flavour helpers
    // ------------------------------------------------------------------
    /// <summary>A plausible ball-carrier for an open-play carry — weighted toward the players
    /// who realistically carry most (backs, back row, hooker), but every position on the pitch
    /// gets the ball sometimes, forwards included.</summary>
    private Player PickRunner(int team)
    {
        var t = _teams[team].Team;
        var candidates = new[]
        {
            // Backs and the back row carry most often — listed more than once to weight the pick.
            t.At(LeftWing), t.At(RightWing), t.At(Fullback),
            t.At(OutsideCentre), t.At(InsideCentre), t.At(FlyHalf), t.At(ScrumHalf),
            t.At(Number8), t.At(Number8), t.At(BlindsideFlanker), t.At(OpensideFlanker),
            // The tight five carry less often, but they do carry — crash balls, pick-and-goes.
            t.At(Hooker), t.At(LooseheadProp), t.At(TightheadProp), t.At(Lock4), t.At(Lock5),
        };
        return _dice.Pick(candidates);
    }

    /// <summary>A plausible last-ditch tackler for commentary — the sort of player who'd be
    /// covering across (back row, centre, or back three), not a positionally exact defender.</summary>
    private Player PickDefender(int team)
    {
        var t = _teams[team].Team;
        var candidates = new[]
        {
            t.At(OpensideFlanker), t.At(BlindsideFlanker), t.At(InsideCentre),
            t.At(OutsideCentre), t.At(Fullback),
        };
        return _dice.Pick(candidates);
    }

    private Player PickForward(int team)
    {
        var t = _teams[team].Team;
        var candidates = new[]
        {
            t.At(Hooker), t.At(Number8), t.At(BlindsideFlanker),
            t.At(OpensideFlanker), t.At(Lock4), t.At(Lock5),
        };
        return _dice.Pick(candidates);
    }

    /// <summary>
    /// Builds the final result snapshot. Safe to call once <see cref="IsComplete"/> — or at any
    /// point via <see cref="Step"/>-driven code that wants to record a match's outcome as soon
    /// as full-time is reached, without needing to call <see cref="Simulate"/> again.
    /// </summary>
    public MatchResult BuildResult()
    {
        FinalisePercentages();
        return new MatchResult
        {
            HomeName = Home.Team.Name,
            AwayName = Away.Team.Name,
            HomeShort = Home.Team.ShortName,
            AwayShort = Away.Team.ShortName,
            HomeScore = Home.Score,
            AwayScore = Away.Score,
            HomeStats = Home.Stats,
            AwayStats = Away.Stats,
            Events = _events,
            Seed = _dice.Seed,
        };
    }

    private void TrackPossessionAndTerritory(int att, double dt)
    {
        _teams[att].Stats.PossessionMinutes += dt;
        // Territory credited to whichever side has play in the opponent's half.
        if (_ballX > 50) Home.Stats.TerritoryMinutes += dt;
        else Away.Stats.TerritoryMinutes += dt;
    }

    private void FinalisePercentages()
    {
        double poss = Home.Stats.PossessionMinutes + Away.Stats.PossessionMinutes;
        double terr = Home.Stats.TerritoryMinutes + Away.Stats.TerritoryMinutes;
        Home.Stats.PossessionPct = poss > 0 ? 100 * Home.Stats.PossessionMinutes / poss : 50;
        Away.Stats.PossessionPct = 100 - Home.Stats.PossessionPct;
        Home.Stats.TerritoryPct = terr > 0 ? 100 * Home.Stats.TerritoryMinutes / terr : 50;
        Away.Stats.TerritoryPct = 100 - Home.Stats.TerritoryPct;
    }
}
