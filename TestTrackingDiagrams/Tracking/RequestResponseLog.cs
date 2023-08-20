namespace TestTrackingDiagrams.Tracking;

public record RequestResponseLog(
    string TestInfo,
    RequestLog Request,
    ResponseLog Response);