using Reqnroll;

namespace TestTrackingDiagrams.ReqNRoll.xUnit2;

public record ReqNRollStepInfo(string Keyword, string Text, ScenarioExecutionStatus Status = ScenarioExecutionStatus.OK);
