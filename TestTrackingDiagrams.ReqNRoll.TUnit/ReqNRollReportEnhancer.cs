using System.Text.Json;

namespace TestTrackingDiagrams.ReqNRoll.TUnit;

public static class ReqNRollReportEnhancer
{
    private static DiagramsFetcherOptions? _fetcherOptions;
    private static bool _registered;

    // ReqNRoll's HTML formatter writes its report file AFTER [AfterTestRun] hooks complete,
    // so we use ProcessExit to post-process the report once the formatter has finished writing it.
    internal static void RegisterForEnhancement(DiagramsFetcherOptions? fetcherOptions = null)
    {
        if (_registered) return;
        _registered = true;
        _fetcherOptions = fetcherOptions;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    private static void OnProcessExit(object? sender, EventArgs e)
    {
        try
        {
            EnhanceReport(_fetcherOptions);
        }
        catch
        {
            // Swallow exceptions during process exit to avoid crashing the test runner
        }
    }

    public static void EnhanceReport(DiagramsFetcherOptions? fetcherOptions = null, string? reportPath = null)
    {
        var path = reportPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "reqnroll_report.html");
        if (!File.Exists(path)) return;

        var diagrams = DefaultDiagramsFetcher.GetDiagramsFetcher(fetcherOptions)();
        if (diagrams.Length == 0) return;

        var scenarios = ReqNRollScenarioCollector.GetAll();
        if (scenarios.Length == 0) return;

        var html = File.ReadAllText(path);

        const string prefix = "window.CUCUMBER_MESSAGES = ";
        var prefixIdx = html.IndexOf(prefix, StringComparison.Ordinal);
        if (prefixIdx == -1) return;

        var arrayStartIdx = prefixIdx + prefix.Length;
        var arraySemicolonIdx = html.IndexOf("];", arrayStartIdx, StringComparison.Ordinal);
        if (arraySemicolonIdx == -1) return;

        var arrayEndIdx = arraySemicolonIdx; // position of the ']'
        var messagesJson = html.Substring(arrayStartIdx, arrayEndIdx - arrayStartIdx + 1);

        using var document = JsonDocument.Parse(messagesJson);

        // Cucumber Messages uses a chain of IDs to link scenarios to test executions:
        //   pickle (scenario definition) → testCase (test plan) → testCaseStarted (test execution)
        // Attachments must reference both testCaseStartedId AND testStepId because
        // Cucumber React's findAttachmentsBy filters by testStepId within the testCaseStartedId bucket.
        var pickleNameToId = new Dictionary<string, List<string>>();
        var pickleIdToTestCaseId = new Dictionary<string, string>();
        var testCaseIdToStartedId = new Dictionary<string, string>();
        // For each testCase, find the last scenario step's testStepId (the step with a pickleStepId,
        // as opposed to hook steps which have hookId). We attach diagrams to this step.
        var testCaseIdToLastStepId = new Dictionary<string, string>();
        // Track each testCaseFinished's array index and its testCaseStartedId, so we can insert
        // attachments just before it. Cucumber React processes messages sequentially — attachments
        // must appear between testCaseStarted and testCaseFinished to be rendered.
        var testCaseFinishedPositions = new Dictionary<int, string>();

        var idx = 0;
        foreach (var element in document.RootElement.EnumerateArray())
        {
            if (element.TryGetProperty("pickle", out var pickle))
            {
                var name = pickle.GetProperty("name").GetString()!;
                var id = pickle.GetProperty("id").GetString()!;
                if (!pickleNameToId.TryGetValue(name, out var ids))
                {
                    ids = [];
                    pickleNameToId[name] = ids;
                }
                ids.Add(id);
            }
            else if (element.TryGetProperty("testCase", out var testCase))
            {
                var pickleId = testCase.GetProperty("pickleId").GetString()!;
                var tcId = testCase.GetProperty("id").GetString()!;
                pickleIdToTestCaseId[pickleId] = tcId;

                // Find the last test step that represents an actual scenario step (has pickleStepId)
                foreach (var step in testCase.GetProperty("testSteps").EnumerateArray())
                {
                    if (step.TryGetProperty("pickleStepId", out _))
                        testCaseIdToLastStepId[tcId] = step.GetProperty("id").GetString()!;
                }
            }
            else if (element.TryGetProperty("testCaseStarted", out var started))
            {
                var testCaseId = started.GetProperty("testCaseId").GetString()!;
                var id = started.GetProperty("id").GetString()!;
                testCaseIdToStartedId[testCaseId] = id;
            }
            else if (element.TryGetProperty("testCaseFinished", out var finished))
            {
                var startedId = finished.GetProperty("testCaseStartedId").GetString()!;
                testCaseFinishedPositions[idx] = startedId;
            }
            idx++;
        }

        // Build a map of testCaseStartedId → attachment JSON strings for that scenario
        var attachmentsPerStartedId = new Dictionary<string, List<string>>();

        foreach (var scenario in scenarios)
        {
            var scenarioDiagrams = diagrams.Where(d => d.TestRuntimeId == scenario.ScenarioId).ToArray();
            if (scenarioDiagrams.Length == 0) continue;

            if (!pickleNameToId.TryGetValue(scenario.ScenarioTitle, out var pickleIds)) continue;

            foreach (var pickleId in pickleIds)
            {
                if (!pickleIdToTestCaseId.TryGetValue(pickleId, out var testCaseId)) continue;
                if (!testCaseIdToStartedId.TryGetValue(testCaseId, out var testCaseStartedId)) continue;
                testCaseIdToLastStepId.TryGetValue(testCaseId, out var lastStepId);

                var attachments = new List<string>();
                foreach (var diagram in scenarioDiagrams)
                {
                    // Cucumber React Components render image/* attachments with a url property as <img src="url">,
                    // which is how we get the PlantUML diagram image to display in the report.
                    attachments.Add(JsonSerializer.Serialize(new
                    {
                        attachment = new
                        {
                            testCaseStartedId,
                            testStepId = lastStepId,
                            body = "",
                            contentEncoding = "IDENTITY", // IDENTITY = no encoding (plain text); the alternative is BASE64
                            mediaType = "image/png",
                            url = diagram.ImgSrc,
                            fileName = "Sequence Diagram"
                        }
                    }));

                    // We use text/plain rather than text/html because Cucumber React Components
                    // renders text/html as escaped text inside <pre>, not as actual HTML.
                    attachments.Add(JsonSerializer.Serialize(new
                    {
                        attachment = new
                        {
                            testCaseStartedId,
                            testStepId = lastStepId,
                            body = diagram.CodeBehind,
                            contentEncoding = "IDENTITY",
                            mediaType = "text/plain",
                            fileName = "PlantUML Code"
                        }
                    }));
                }

                attachmentsPerStartedId[testCaseStartedId] = attachments;
                break; // Only need to match the first pickle ID for this scenario
            }
        }

        if (attachmentsPerStartedId.Count == 0) return;

        // Rebuild the messages array, inserting attachments just before each testCaseFinished.
        var newElements = new List<string>();
        idx = 0;
        foreach (var element in document.RootElement.EnumerateArray())
        {
            if (testCaseFinishedPositions.TryGetValue(idx, out var startedId) &&
                attachmentsPerStartedId.TryGetValue(startedId, out var pending))
            {
                newElements.AddRange(pending);
            }
            newElements.Add(element.GetRawText());
            idx++;
        }

        var newArrayJson = "[" + string.Join(",", newElements) + "]";
        html = html.Substring(0, arrayStartIdx) + newArrayJson + html.Substring(arrayEndIdx + 1);
        File.WriteAllText(path, html);
    }
}

