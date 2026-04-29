namespace TestTrackingDiagrams.Reports;

/// <summary>
/// A file attachment associated with a test step (e.g. screenshot, log file).
/// </summary>
public record FileAttachment(string Name, string RelativePath);
