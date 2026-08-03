namespace RugbyManager.Core.Training;

/// <summary>One attribute ticking up a point for one player during a training week.</summary>
public sealed record AttributeGain(string PlayerName, string Attribute, int NewValue);
