namespace TestTrackingDiagrams.Reports;

/// <summary>
/// Represents a single step within a test scenario (e.g. Given, When, Then).
/// </summary>
public record ScenarioStep
{
    public string? Keyword { get; set; }
    public required string Text { get; set; }
    public ExecutionResult? Status { get; set; }
    public TimeSpan? Duration { get; set; }
    public ScenarioStep[]? SubSteps { get; set; }
    public StepParameter[]? Parameters { get; set; }
    public string[]? Comments { get; set; }
    public FileAttachment[]? Attachments { get; set; }
    public string? DocString { get; set; }
    public string? DocStringMediaType { get; set; }

    /// <summary>
    /// Structured representation of the step text with inline parameter values embedded.
    /// When set, the renderer uses this instead of <see cref="Text"/> to produce highlighted
    /// parameter values within the prose (matching LightBDD's native report rendering).
    /// </summary>
    public StepTextSegment[]? TextSegments { get; set; }
}

/// <summary>
/// A segment of step text — either literal prose or an inline parameter value.
/// </summary>
public record StepTextSegment
{
    /// <summary>Literal text content (mutually exclusive with <see cref="Parameter"/> and <see cref="TableReference"/>).</summary>
    public string? Text { get; init; }

    /// <summary>An inline parameter value to render highlighted (mutually exclusive with <see cref="Text"/> and <see cref="TableReference"/>).</summary>
    public InlineParameterValue? Parameter { get; init; }

    /// <summary>The parameter name (for tooltip), only set when <see cref="Parameter"/> is set.</summary>
    public string? ParameterName { get; init; }

    /// <summary>A reference to a tabular/tree parameter rendered below the step. Renders as a clickable toggle. Mutually exclusive with <see cref="Text"/> and <see cref="Parameter"/>.</summary>
    public string? TableReference { get; init; }

    /// <summary>Creates a literal text segment.</summary>
    public static StepTextSegment Literal(string text) => new() { Text = text };

    /// <summary>Creates a parameter segment with value, verification status, and name.</summary>
    public static StepTextSegment Param(string? name, InlineParameterValue value) => new() { Parameter = value, ParameterName = name };

    /// <summary>Creates a table/tree parameter reference segment that toggles visibility of the associated table.</summary>
    public static StepTextSegment TableRef(string paramName) => new() { TableReference = paramName };
}
