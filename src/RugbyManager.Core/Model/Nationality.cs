namespace RugbyManager.Core.Model;

/// <summary>
/// A player's nation. Flavour today (shown on squad/player screens); the fixed set here is
/// deliberately the real-world Rugby Union heartland so it can later back an international
/// eligibility feature (e.g. a national-team pool keyed by this value) without a data migration.
/// </summary>
public enum Nationality
{
    Unspecified,
    NewZealand,
    SouthAfrica,
    England,
    Wales,
    Ireland,
    Scotland,
    France,
    Australia,
    Argentina,
    Italy,
    Fiji,
    Samoa,
    Tonga,
    Georgia,
    Japan,
}

public static class NationalityExtensions
{
    /// <summary>Short 3-letter code for compact table display, e.g. squad/transfer lists.</summary>
    public static string Code(this Nationality n) => n switch
    {
        Nationality.NewZealand => "NZL",
        Nationality.SouthAfrica => "RSA",
        Nationality.England => "ENG",
        Nationality.Wales => "WAL",
        Nationality.Ireland => "IRE",
        Nationality.Scotland => "SCO",
        Nationality.France => "FRA",
        Nationality.Australia => "AUS",
        Nationality.Argentina => "ARG",
        Nationality.Italy => "ITA",
        Nationality.Fiji => "FIJ",
        Nationality.Samoa => "SAM",
        Nationality.Tonga => "TON",
        Nationality.Georgia => "GEO",
        Nationality.Japan => "JPN",
        _ => "—",
    };

    /// <summary>Display name, e.g. "New Zealand".</summary>
    public static string DisplayName(this Nationality n) => n switch
    {
        Nationality.NewZealand => "New Zealand",
        Nationality.SouthAfrica => "South Africa",
        _ => n.ToString(),
    };
}
