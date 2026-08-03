using static RugbyManager.Core.Model.Position;

namespace RugbyManager.Core.Model;

/// <summary>The catalogue of set plays a manager can add to a team's playbook.</summary>
public static class SetPlayLibrary
{
    public static readonly IReadOnlyList<SetPlay> All = new[]
    {
        // Backline moves (Attack coach).
        new SetPlay { Name = "Crash Ball",        Area = SetPlayArea.BacklineMove, Difficulty = 35, Coaching = CoachSpecialty.Attack, KeyPositions = new[] { InsideCentre, Number8 } },
        new SetPlay { Name = "Miss-2 Cut",        Area = SetPlayArea.BacklineMove, Difficulty = 55, Coaching = CoachSpecialty.Attack, KeyPositions = new[] { FlyHalf, OutsideCentre } },
        new SetPlay { Name = "Loop Around",       Area = SetPlayArea.BacklineMove, Difficulty = 60, Coaching = CoachSpecialty.Attack, KeyPositions = new[] { FlyHalf, InsideCentre } },
        new SetPlay { Name = "Double Switch",     Area = SetPlayArea.BacklineMove, Difficulty = 75, Coaching = CoachSpecialty.Attack, KeyPositions = new[] { FlyHalf, InsideCentre, OutsideCentre } },
        new SetPlay { Name = "Wraparound Strike", Area = SetPlayArea.BacklineMove, Difficulty = 85, Coaching = CoachSpecialty.Attack, KeyPositions = new[] { ScrumHalf, FlyHalf } },

        // Lineout plays (Lineout coach).
        new SetPlay { Name = "Catch & Drive",     Area = SetPlayArea.LineoutPlay,  Difficulty = 40, Coaching = CoachSpecialty.Lineout, KeyPositions = new[] { Hooker, Lock5 } },
        new SetPlay { Name = "Off-the-Top Strike",Area = SetPlayArea.LineoutPlay,  Difficulty = 55, Coaching = CoachSpecialty.Lineout, KeyPositions = new[] { Lock4, ScrumHalf } },
        new SetPlay { Name = "Front Peel",        Area = SetPlayArea.LineoutPlay,  Difficulty = 65, Coaching = CoachSpecialty.Lineout, KeyPositions = new[] { Hooker, BlindsideFlanker } },
        new SetPlay { Name = "Throw to the Tail", Area = SetPlayArea.LineoutPlay,  Difficulty = 80, Coaching = CoachSpecialty.Lineout, KeyPositions = new[] { Hooker, Number8 } },

        // Scrum plays (Scrum coach).
        new SetPlay { Name = "8-9 Pick and Go",   Area = SetPlayArea.ScrumPlay,    Difficulty = 40, Coaching = CoachSpecialty.Scrum, KeyPositions = new[] { Number8, ScrumHalf } },
        new SetPlay { Name = "Number 8 Pickup",   Area = SetPlayArea.ScrumPlay,    Difficulty = 55, Coaching = CoachSpecialty.Scrum, KeyPositions = new[] { Number8 } },
        new SetPlay { Name = "Blindside Break",   Area = SetPlayArea.ScrumPlay,    Difficulty = 70, Coaching = CoachSpecialty.Scrum, KeyPositions = new[] { ScrumHalf, BlindsideFlanker } },
    };
}
