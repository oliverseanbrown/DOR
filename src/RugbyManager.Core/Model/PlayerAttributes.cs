namespace RugbyManager.Core.Model;

/// <summary>
/// A player's core attributes on a 1-100 scale (CM/FM style).
/// The match engine reads these to resolve every contest. Keep this as pure data —
/// derived, position-weighted ratings are computed in the match layer.
/// </summary>
public sealed class PlayerAttributes
{
    // --- Physical ---
    public int Strength { get; set; }      // scrummaging, contact, carries
    public int Pace { get; set; }          // top speed - line breaks & finishing
    public int Acceleration { get; set; }  // first few steps, beating a defender
    public int Stamina { get; set; }       // how long attributes hold up over 80 mins
    public int Agility { get; set; }       // footwork, sidesteps, evasion

    // --- Technical ---
    public int Handling { get; set; }       // catch/pass under pressure (fewer knock-ons)
    public int Passing { get; set; }        // accuracy & range of distribution
    public int Tackling { get; set; }       // completing tackles, dominant hits
    public int Kicking { get; set; }        // out of hand: territory, tactical, box kicks
    public int GoalKicking { get; set; }    // penalties, conversions
    public int Lineout { get; set; }        // throwing (hooker) / jumping (locks & back row)
    public int Scrummaging { get; set; }    // set-piece power & technique (front row)
    public int Breakdown { get; set; }      // ruck contest, jackaling, clearing out

    // --- Mental ---
    public int DecisionMaking { get; set; } // shape choices, when to pass/kick/run
    public int Composure { get; set; }      // performing under pressure & fatigue
    public int Discipline { get; set; }     // LOW discipline => concedes more penalties
    public int WorkRate { get; set; }       // involvement, ruck arrivals, drains stamina
    public int Leadership { get; set; }     // steadies team morale
    public int Positioning { get; set; }    // defensive read, covering space
    public int Vision { get; set; }         // spotting & creating space
    public int Aggression { get; set; }     // physical edge (and penalty risk)

    // --- Hidden (not shown to the player directly) ---
    public int Potential { get; set; }       // ceiling for development
    public int Consistency { get; set; }     // how reliably true ability shows up
    public int InjuryProneness { get; set; } // higher => gets injured more

    /// <summary>Every attribute at a single value — handy for tests and placeholders.</summary>
    public static PlayerAttributes Uniform(int value) => new()
    {
        Strength = value, Pace = value, Acceleration = value, Stamina = value, Agility = value,
        Handling = value, Passing = value, Tackling = value, Kicking = value, GoalKicking = value,
        Lineout = value, Scrummaging = value, Breakdown = value,
        DecisionMaking = value, Composure = value, Discipline = value, WorkRate = value,
        Leadership = value, Positioning = value, Vision = value, Aggression = value,
        Potential = value, Consistency = value, InjuryProneness = value,
    };
}
