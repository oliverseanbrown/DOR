using RugbyManager.Core.Generation;
using RugbyManager.Core.Match;
using RugbyManager.Core.Model;
using RugbyManager.Core.Util;
using Xunit;

namespace RugbyManager.Tests;

public class SetPlayTests
{
    [Fact]
    public void Library_CoversAllAreas()
    {
        Assert.Contains(SetPlayLibrary.All, p => p.Area == SetPlayArea.BacklineMove);
        Assert.Contains(SetPlayLibrary.All, p => p.Area == SetPlayArea.LineoutPlay);
        Assert.Contains(SetPlayLibrary.All, p => p.Area == SetPlayArea.ScrumPlay);
    }

    [Fact]
    public void WellDrilledFocusedPlaybook_ScoresMoreTriesThanNone()
    {
        // Proficiency has to be earned: a playbook a team has never practiced shouldn't
        // outperform having none (see FreshPlaybook_DoesNotYetOutperformNone below). A team
        // that has genuinely drilled a FOCUSED playbook (one polished play per area, not
        // every play in the library at once) with good coaching should come out ahead.
        //
        // Note this deliberately isn't "load every play and max every familiarity": doing
        // that spreads attempts thin across many plays and — since every one of them is
        // equally at maximum familiarity — makes every single attempt equally scoutable too,
        // which roughly cancels the benefit out. A few well-drilled plays beats a bloated,
        // equally-famous playbook — a real strategic trade-off, not just a test artifact.
        int TriesOver(int matches, bool equipped)
        {
            int tries = 0;
            for (int seed = 0; seed < matches; seed++)
            {
                var home = SquadGenerator.Generate("Home", "HOM", 68, new Tactics(), 100 + seed);
                var away = SquadGenerator.Generate("Away", "AWY", 68, new Tactics(), 500 + seed);
                if (equipped)
                {
                    foreach (var area in Enum.GetValues<SetPlayArea>())
                    {
                        var play = SetPlayLibrary.All.First(p => p.Area == area);
                        home.Playbook.Add(play);
                        home.BumpFamiliarity(play.Name, 100); // fully drilled, proven in anger
                    }
                    home.Coaches.Add(new Coach { Name = "AC", Specialty = CoachSpecialty.Attack, Ability = 90, Wage = 1 });
                    home.Coaches.Add(new Coach { Name = "LC", Specialty = CoachSpecialty.Lineout, Ability = 90, Wage = 1 });
                    home.Coaches.Add(new Coach { Name = "SC", Specialty = CoachSpecialty.Scrum, Ability = 90, Wage = 1 });
                    home.SelectBestXV();
                }
                tries += new MatchEngine(home, away, seed).Simulate().HomeStats.Tries;
            }
            return tries;
        }

        int equippedTries = TriesOver(150, equipped: true);
        int plainTries = TriesOver(150, equipped: false);

        Assert.True(equippedTries > plainTries,
            $"A focused, well-drilled playbook+coaches should lift tries ({equippedTries} vs {plainTries}).");
    }

    [Fact]
    public void FreshPlaybook_DoesNotYetOutperformNone()
    {
        // A playbook loaded up on matchday with zero reps (default familiarity) shouldn't beat
        // having no playbook at all — plays have to be earned through training and match use.
        int TriesOver(int matches, bool equipped)
        {
            int tries = 0;
            for (int seed = 0; seed < matches; seed++)
            {
                var home = SquadGenerator.Generate("Home", "HOM", 68, new Tactics(), 100 + seed);
                var away = SquadGenerator.Generate("Away", "AWY", 68, new Tactics(), 500 + seed);
                if (equipped)
                {
                    foreach (var p in SetPlayLibrary.All) home.Playbook.Add(p);
                    home.SelectBestXV();
                }
                tries += new MatchEngine(home, away, seed).Simulate().HomeStats.Tries;
            }
            return tries;
        }

        int freshTries = TriesOver(150, equipped: true);
        int noneTries = TriesOver(150, equipped: false);

        Assert.True(freshTries <= noneTries + 15,
            $"An undrilled playbook shouldn't meaningfully outperform none ({freshTries} vs {noneTries}).");
    }

    [Fact]
    public void Familiarity_GrowsWithMatchingTraining_AndRustsOtherwise()
    {
        var club = SquadGenerator.Generate("A", "A", 65, new Tactics(), 1);
        var play = SetPlayLibrary.All.First(p => p.Area == SetPlayArea.BacklineMove);
        club.Playbook.Add(play);
        var dice = new Dice(1);

        int start = club.GetFamiliarity(play.Name);
        Core.Training.TrainingService.ApplyWeek(club, Core.Training.TrainingFocus.Handling, dice);
        int afterHandling = club.GetFamiliarity(play.Name);
        Assert.True(afterHandling > start, "Handling training should drill a backline move.");

        club.BumpFamiliarity(play.Name, 100 - club.GetFamiliarity(play.Name)); // push to 100
        Core.Training.TrainingService.ApplyWeek(club, Core.Training.TrainingFocus.Fitness, dice);
        int afterUnrelated = club.GetFamiliarity(play.Name);
        Assert.True(afterUnrelated < 100, "An unpracticed play should go slightly rusty.");
    }

    [Fact]
    public void HeavilyFamiliarPlay_CanBeReadAndTurnedOver()
    {
        // A team that always reaches for the same, very well-known play against a defence
        // with a sharp coach should, across enough matches, occasionally get read and turned
        // over before the play even develops — the "they've seen this before" mechanic.
        var home = SquadGenerator.Generate("Home", "HOM", 65, new Tactics(), 1);
        var away = SquadGenerator.Generate("Away", "AWY", 65, new Tactics(), 2);
        var play = SetPlayLibrary.All.First(p => p.Area == SetPlayArea.BacklineMove);
        home.Playbook.Add(play);
        home.BumpFamiliarity(play.Name, 100);
        away.Coaches.Add(new Coach { Name = "DC", Specialty = CoachSpecialty.Defence, Ability = 95, Wage = 1 });

        bool sawRead = false;
        for (int seed = 0; seed < 200 && !sawRead; seed++)
        {
            var events = new MatchEngine(home, away, seed).Simulate().Events;
            sawRead = events.Any(e =>
                e.Type == MatchEventType.Turnover && e.Drama == Drama.Highlight && e.Text.Contains(play.Name));
        }

        Assert.True(sawRead, "Expected the defence to read and turn over a heavily-familiar, oft-used play at least once in 200 seeds.");
    }
}
