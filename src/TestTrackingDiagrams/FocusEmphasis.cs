namespace TestTrackingDiagrams;

/// <summary>
/// Flags controlling how focused fields are emphasised in diagram notes.
/// </summary>
[Flags]
public enum FocusEmphasis
{
    /// <summary>No emphasis applied to focused fields.</summary>
    None = 0,

    /// <summary>Focused fields are rendered in bold.</summary>
    Bold = 1,

    /// <summary>Focused fields are rendered with a highlight colour.</summary>
    Colored = 2
}
