namespace RugbyManager.Web.Services;

/// <summary>
/// Supabase project connection details. The anon key is a publishable client key — safe to
/// ship in client-side code — access is actually enforced by the project's Row Level Security
/// policies, not by keeping this key secret.
/// </summary>
public static class SupabaseConfig
{
    public const string Url = "https://mzlqvnyspxbudrhrtstb.supabase.co";
    public const string AnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im16bHF2bnlzcHhidWRyaHJ0c3RiIiwicm9sZSI6ImFub24iLCJpYXQiOjE3ODU3NzUzMzAsImV4cCI6MjEwMTM1MTMzMH0.-nWuGVhdnAgHeUbSahcRUBeY8NPilNEJzoEzjqwsZDs";
}
