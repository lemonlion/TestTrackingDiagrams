namespace TestTrackingDiagrams;

/// <summary>
/// Flags controlling how non-focused fields are de-emphasised in diagram notes.
/// </summary>
[Flags]
public enum FocusDeEmphasis
{
    /// <summary>Non-focused fields are displayed normally.</summary>
    None = 0,

    /// <summary>Non-focused fields are rendered in light gray.</summary>
    LightGray = 1,

    /// <summary>Non-focused fields are rendered with a smaller font size.</summary>
    SmallerText = 2,

    /// <summary>Non-focused fields are hidden entirely.</summary>
    Hidden = 4
}
