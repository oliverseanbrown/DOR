namespace RugbyManager.Core.Model;

/// <summary>
/// The 15 starting positions in Rugby Union, numbered by their traditional shirt number.
/// The enum's integer value IS the shirt number (1-15).
/// </summary>
public enum Position
{
    LooseheadProp = 1,
    Hooker = 2,
    TightheadProp = 3,
    Lock4 = 4,
    Lock5 = 5,
    BlindsideFlanker = 6,
    OpensideFlanker = 7,
    Number8 = 8,
    ScrumHalf = 9,
    FlyHalf = 10,
    LeftWing = 11,
    InsideCentre = 12,
    OutsideCentre = 13,
    RightWing = 14,
    Fullback = 15,
}

/// <summary>Broad tactical groupings that positions belong to.</summary>
public enum Unit
{
    FrontRow,   // 1, 2, 3   - scrummaging & the "dark arts"
    SecondRow,  // 4, 5      - lineout engine, physicality
    BackRow,    // 6, 7, 8   - breakdown, tackling, carrying
    HalfBacks,  // 9, 10     - game management, the "brains"
    Centres,    // 12, 13    - midfield distribution & defence
    BackThree,  // 11, 14, 15 - pace, finishing, aerial ability
}

public static class PositionExtensions
{
    /// <summary>Traditional shirt number (1-15).</summary>
    public static int ShirtNumber(this Position p) => (int)p;

    /// <summary>Forwards wear 1-8, backs wear 9-15.</summary>
    public static bool IsForward(this Position p) => (int)p <= 8;

    public static bool IsBack(this Position p) => !p.IsForward();

    public static Unit Unit(this Position p) => p switch
    {
        Position.LooseheadProp or Position.Hooker or Position.TightheadProp => Model.Unit.FrontRow,
        Position.Lock4 or Position.Lock5 => Model.Unit.SecondRow,
        Position.BlindsideFlanker or Position.OpensideFlanker or Position.Number8 => Model.Unit.BackRow,
        Position.ScrumHalf or Position.FlyHalf => Model.Unit.HalfBacks,
        Position.InsideCentre or Position.OutsideCentre => Model.Unit.Centres,
        _ => Model.Unit.BackThree,
    };

    /// <summary>Short label used in commentary and team sheets (e.g. "TH", "FH").</summary>
    public static string ShortName(this Position p) => p switch
    {
        Position.LooseheadProp => "LH",
        Position.Hooker => "HK",
        Position.TightheadProp => "TH",
        Position.Lock4 => "L4",
        Position.Lock5 => "L5",
        Position.BlindsideFlanker => "BF",
        Position.OpensideFlanker => "OF",
        Position.Number8 => "N8",
        Position.ScrumHalf => "SH",
        Position.FlyHalf => "FH",
        Position.LeftWing => "LW",
        Position.InsideCentre => "IC",
        Position.OutsideCentre => "OC",
        Position.RightWing => "RW",
        Position.Fullback => "FB",
        _ => "??",
    };
}
