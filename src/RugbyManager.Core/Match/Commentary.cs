using RugbyManager.Core.Util;

namespace RugbyManager.Core.Match;

/// <summary>
/// Picks flavourful commentary lines. Templates use {0}=player, {1}=team where relevant.
/// All variety is drawn from the match Dice, so commentary is deterministic per seed too.
/// </summary>
public sealed class Commentary
{
    private readonly Dice _d;
    public Commentary(Dice d) => _d = d;

    private string Pick(string[] a) => _d.Pick(a);

    public string TryScored(string player, string team) => string.Format(Pick(new[]
    {
        "TRY! {0} crashes over for {1}!",
        "TRY! {0} dots it down in the corner — what a finish for {1}!",
        "TRY! {0} slices through and goes under the posts! {1} are over!",
        "TRY! Great hands from {1} and {0} is in at the corner!",
        "TRY! {0} bulldozes over from close range for {1}!",
    }), player, team);

    public string MaulTry(string team) => string.Format(Pick(new[]
    {
        "TRY! {0}'s driving maul rumbles over — unstoppable up front!",
        "TRY! The {0} pack mauls it down over the line! Textbook forward power.",
        "TRY! Catch and drive from the lineout and {0} bundle it over!",
    }), team);

    public string Conversion(string player) => string.Format(Pick(new[]
    {
        "{0} adds the extras. Two more.",
        "{0} slots the conversion, right between the sticks.",
        "The conversion is good from {0}.",
    }), player);

    public string ConversionMissed(string player) => string.Format(Pick(new[]
    {
        "{0} pushes the conversion wide. No extras.",
        "{0} can't add the two — drifts past the upright.",
    }), player);

    public string PenaltyGoal(string player) => string.Format(Pick(new[]
    {
        "{0} makes no mistake with the penalty. Three points.",
        "Straight through the middle — {0} kicks the penalty goal.",
        "{0} knocks over the three from the tee.",
    }), player);

    public string PenaltyMissed(string player) => string.Format(Pick(new[]
    {
        "{0} drags the penalty wide! Points left out there.",
        "Just short and wide from {0}. No score.",
    }), player);

    public string DropGoal(string player) => string.Format(Pick(new[]
    {
        "DROP GOAL! {0} drops back into the pocket and nails it!",
        "{0} guides over a cheeky drop goal! Three points.",
    }), player);

    public string DropGoalMissed(string player) => string.Format(Pick(new[]
    {
        "{0} snatches at the drop goal attempt — wide.",
    }), player);

    public string PenaltyConceded(string team) => string.Format(Pick(new[]
    {
        "Penalty against {0} at the breakdown.",
        "{0} stray offside — the referee blows up.",
        "{0} penalised for not releasing. Chance for the opposition.",
        "Hands in the ruck from {0} — penalty.",
    }), team);

    public string LineBreak(string player, string team) => string.Format(Pick(new[]
    {
        "{0} slices through the line! {1} are away!",
        "Half-gap and {0} is through it — big metres for {1}!",
        "{0} steps the defender and breaks clear for {1}!",
    }), player, team);

    public string Turnover(string team) => string.Format(Pick(new[]
    {
        "Turnover! {0} steal it at the breakdown.",
        "Jackal! {0} win the ball back on the deck.",
        "{0} counter-ruck and it's their ball now.",
    }), team);

    public string KnockOn(string team) => string.Format(Pick(new[]
    {
        "Knock-on by {0}. Scrum the other way.",
        "Spilled it! {0} put it down in the tackle.",
        "Handling error from {0} — knocked on.",
    }), team);

    public string Scrum(string team) => string.Format(Pick(new[]
    {
        "Scrum ball won cleanly by {0}.",
        "{0} get a solid platform at the scrum.",
    }), team);

    public string ScrumPenalty(string team) => string.Format(Pick(new[]
    {
        "Dominant scrum! {0} earn the penalty up front.",
        "{0}'s front row splinters the opposition — penalty!",
        "The {0} scrum marches them backwards. Penalty.",
    }), team);

    public string AgainstHead(string team) => string.Format(Pick(new[]
    {
        "Against the head! {0} nick the scrum ball!",
    }), team);

    public string Lineout(string team) => string.Format(Pick(new[]
    {
        "Lineout secured by {0}.",
        "{0} take it cleanly at the front.",
        "Good throw, {0} claim the lineout.",
    }), team);

    public string LineoutSteal(string team) => string.Format(Pick(new[]
    {
        "Stolen! {0} pinch the lineout!",
        "Overthrown — {0} pounce on the loose ball.",
    }), team);

    public string KickToTouch(string team) => string.Format(Pick(new[]
    {
        "{0} kick long into touch — territory gained.",
        "Good tactical kick to the corner from {0}.",
    }), team);

    public string KickTerritory(string team) => string.Format(Pick(new[]
    {
        "{0} put boot to ball, kicking for field position.",
        "Up-and-under from {0}, contesting in the air.",
    }), team);

    public string KickReclaim(string team) => string.Format(Pick(new[]
    {
        "Brilliant chase from {0} — they regather their own kick!",
    }), team);

    // --- Breakaway try narrative: a short cinematic build-up for the biggest tries,
    //     told beat by beat (origin -> missed tackle -> beating the last man -> try). ---

    public string OriginScrumPick(string player, string team) => string.Format(Pick(new[]
    {
        "{0} picks from the back of the scrum for {1}.",
        "{0} takes it from the base of the scrum for {1}.",
    }), player, team);

    public string OriginLineoutCarry(string player, string team) => string.Format(Pick(new[]
    {
        "{0} takes it off the top and carries for {1}.",
        "{0} peels off the lineout and sets off for {1}.",
    }), player, team);

    public string OriginGeneralCarry(string player, string team) => string.Format(Pick(new[]
    {
        "{0} takes it to the line for {1}.",
        "{0} gets on the ball and looks to make something happen for {1}.",
        "{0} receives it with a bit of space for {1}.",
    }), player, team);

    public string MissedTackleBreak(string position, string defender, string defTeam, string carrier) => string.Format(Pick(new[]
    {
        "A missed tackle from the {1} {0}, {2}, allows {3} to break the gain line!",
        "{2} of {1} flies past the tackle — {3} is through!",
        "{3} steps inside a poor tackle attempt from {1}'s {0}, {2}!",
    }), position, defTeam, defender, carrier);

    public string BreakawayTry(string player, string team) => string.Format(Pick(new[]
    {
        "TRY! What a score — {0} completes a brilliant length-of-field try for {1}!",
        "TRY! {0} finishes it off! Sensational stuff from {1}!",
        "TRY! {0} goes the length of the field to touch down for {1}!",
    }), player, team);

    public string NoCoverFinish(string carrier, string team) => string.Format(Pick(new[]
    {
        "There's no one home! {0} strolls over unopposed for {1}!",
        "{0} has the line at his mercy — no cover in sight — and goes over for {1}!",
        "Nobody left to stop him — {0} walks it in for {1}!",
    }), carrier, team);

    /// <summary>The moment the carrier attempts to beat the last defender, and succeeds.</summary>
    public string BeatMoveSuccess(BeatMove move, string carrier, string defTeamShort) => move switch
    {
        BeatMove.RunThrough => string.Format(Pick(new[]
        {
            "{0} puts the head down and bursts straight through the tackle attempt!",
            "{0} shrugs the {1} defender off like he wasn't there!",
        }), carrier, defTeamShort),
        BeatMove.HandOff => string.Format(Pick(new[]
        {
            "{0} plants a hand off in the defender's face and powers clear!",
            "A stiff-arm fend from {0} leaves the {1} defender on the floor!",
        }), carrier, defTeamShort),
        BeatMove.Sidestep => string.Format(Pick(new[]
        {
            "{0} sidesteps beautifully, leaving the {1} cover grasping at thin air!",
            "A gorgeous step from {0} and the defender is left for dead!",
        }), carrier, defTeamShort),
        BeatMove.Goosestep => string.Format(Pick(new[]
        {
            "{0} goosesteps clear — what magnificent footwork!",
            "{0} shifts through the gears and simply leaves the {1} cover behind!",
        }), carrier, defTeamShort),
        BeatMove.Dummy => string.Format(Pick(new[]
        {
            "{0} sells the outrageous dummy and the {1} defence completely bites!",
            "{0} shows the ball, sends the defender the wrong way, and is gone!",
        }), carrier, defTeamShort),
        BeatMove.ChipAndChase => string.Format(Pick(new[]
        {
            "{0} chips it delicately over the top and wins the race to gather!",
            "A perfectly weighted chip from {0}, who regathers to finish it off!",
        }), carrier, defTeamShort),
        _ => $"{carrier} gets past the last defender!",
    };

    /// <summary>The offload itself, when it comes off — narrated separately since the finisher
    /// is a different player from the one who made the break.</summary>
    public string OffloadBeat(string carrier, string support) => string.Format(Pick(new[]
    {
        "{0} draws the last defender and slips a perfect offload to {1}!",
        "{0} holds the defender just long enough to find {1} with the offload!",
    }), carrier, support);

    /// <summary>The last defender wins the contest — outcome for the "held up short" techniques.</summary>
    public string BeatMoveFail(BeatMove move, string defender, string defTeam) => move switch
    {
        BeatMove.RunThrough => string.Format(Pick(new[]
        {
            "{0} holds firm — try-saving hit from {1} stops the charge just short of the line!",
            "Big hit from {0} of {1}! Held up right on the whitewash!",
        }), defender, defTeam),
        BeatMove.HandOff => string.Format(Pick(new[]
        {
            "The fend doesn't work — {0} clings on and drags him down just short!",
        }), defender, defTeam),
        BeatMove.Sidestep => string.Format(Pick(new[]
        {
            "{0} reads the step perfectly and makes the covering tackle!",
        }), defender, defTeam),
        BeatMove.Goosestep => string.Format(Pick(new[]
        {
            "Not quite enough gas — {0} runs it down from behind, right on the line!",
        }), defender, defTeam),
        BeatMove.Dummy => string.Format(Pick(new[]
        {
            "{0} isn't fooled for a second and makes the try-saving tackle!",
        }), defender, defTeam),
        _ => $"{defender} makes the covering tackle!",
    };

    public string ChipAndChaseFail(string defender, string defTeam) => string.Format(Pick(new[]
    {
        "The chip is a gift — {0} gathers it in for {1}!",
        "{0} reads the chip superbly and claims it back for {1}!",
    }), defender, defTeam);

    public string OffloadFail(string defender, string attTeam) => string.Format(Pick(new[]
    {
        "The offload goes to ground! {0} was waiting for it — knock-on.",
        "{0} gets a hand to the offload and it's knocked on — {1} will rue that one.",
    }), defender, attTeam);

    /// <summary>The try line itself, flavoured by whichever move actually beat the defender.</summary>
    public string BreakawayTryVia(BeatMove move, string player, string team) => move switch
    {
        BeatMove.RunThrough => string.Format(Pick(new[]
        {
            "TRY! {0} wasn't going to be stopped! {1} are over!",
        }), player, team),
        BeatMove.HandOff => string.Format(Pick(new[]
        {
            "TRY! {0} finishes it off in style for {1}!",
        }), player, team),
        BeatMove.Sidestep => string.Format(Pick(new[]
        {
            "TRY! {0} completes a superb solo try for {1}!",
        }), player, team),
        BeatMove.Goosestep => string.Format(Pick(new[]
        {
            "TRY! {0} strolls in untouched after that footwork! {1} are over!",
        }), player, team),
        BeatMove.Dummy => string.Format(Pick(new[]
        {
            "TRY! {0} finishes off a brilliant individual try for {1}!",
        }), player, team),
        BeatMove.ChipAndChase => string.Format(Pick(new[]
        {
            "TRY! {0} completes the chip and chase in style for {1}!",
        }), player, team),
        BeatMove.OffloadPass => string.Format(Pick(new[]
        {
            "TRY! {0} finishes off a wonderful team try for {1}!",
        }), player, team),
        _ => string.Format("TRY! {0} finishes it off for {1}!", player, team),
    };

    /// <summary>A rehearsed play gets read and shut down before it develops — either because
    /// it's well-scouted, or the opposition have simply seen it too many times tonight.</summary>
    public string SetPlayRead(string playName, string defender, string defTeam) => string.Format(Pick(new[]
    {
        "They've seen the {0} before — {1} reads it perfectly and {2} snuff it out!",
        "{2} have done their homework on the {0} — {1} shuts it down before it develops!",
        "Spotted a mile off! {1} sees the {0} coming and turns it over for {2}!",
        "Third time of asking and {2} are wise to it — {1} picks off the {0}!",
    }), playName, defender, defTeam);

    public string Substitution(string outgoing, string incoming, string team) => string.Format(Pick(new[]
    {
        "Substitution for {2}: {1} replaces {0}.",
        "{2} make a change — {1} comes on, {0} makes way.",
        "Fresh legs for {2}: on comes {1} in place of {0}.",
    }), outgoing, incoming, team);

    public string TacticsChange(string team) => string.Format(Pick(new[]
    {
        "{0} make a tactical adjustment from the sideline.",
        "A change of approach from {0}'s coaching box.",
        "{0} tweak things up as the game evolves.",
    }), team);
}
