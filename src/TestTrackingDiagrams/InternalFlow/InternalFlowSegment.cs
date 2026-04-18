using System.Diagnostics;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.InternalFlow;

/// <summary>
/// Represents a segment of internal flow between two consecutive HTTP boundaries
/// (e.g. between a request arriving and the SUT calling a dependency).
/// </summary>
public record InternalFlowSegment(
    Guid RequestResponseId,
    RequestResponseType BoundaryType,
    string TestId,
    DateTimeOffset? StartTime,
    DateTimeOffset? EndTime,
    Activity[] Spans);
