using Humanizer;
using Newtonsoft.Json.Linq;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.PlantUml;

public class PlantUmlCreator
{
    private static readonly string[] ExcludedHeaders = { "Cache-Control", "Pragma" };

    public static Dictionary<string, string[]> GetPlantUmlImageTagsPerTestName(IEnumerable<RequestResponseLog>? requestResponses, string plantUmlServerRendererUrl = "https://www.plantuml.com/plantuml/png", Func<string, string>? processor = null)
    {
        var plantUmlPerTestName = requestResponses?.Select(requestResponseLog =>
            {
                var testName = requestResponseLog.TestInfo.ToString();
                var plantUmlImageTag = CreatePlantUmlImageTag(requestResponseLog, plantUmlServerRendererUrl, processor);
                return (testName, plantUmlImageTag);
            }).GroupBy(x => x.testName)
            .ToDictionary(x => x.Key, x => x.Select(y => y.plantUmlImageTag).Distinct().ToArray());

        return plantUmlPerTestName ?? new Dictionary<string, string[]>();
    }

    private static string CreatePlantUml(RequestResponseLog requestResponse, Func<string, string>? processor = null)
    {
        var (httpMethod, requestContent, uri, valueTuples, serviceFullName) = requestResponse.Request;
        var (httpStatusCode, responseContent, headers) = requestResponse.Response;
        var serviceShortName = serviceFullName.Camelize();
        var requestPlantUmlNoteContent = GetPlantUmlForRequestOrResponseNote
            (valueTuples, requestContent, ExcludedHeaders);
        var responsePlantUmlNoteContent = GetPlantUmlForRequestOrResponseNote
            (headers, responseContent, ExcludedHeaders);

        if (processor is not null)
        {
            requestPlantUmlNoteContent = processor.Invoke(requestPlantUmlNoteContent);
            responsePlantUmlNoteContent = processor.Invoke(responsePlantUmlNoteContent);
        }

        var plantUml =
            $"@startuml{Environment.NewLine}" +
            $"!function $my_code($fgcolor){Environment.NewLine}" +
            $"!return \"<color:\"+$fgcolor+\">\"{Environment.NewLine}" +
            $"!endfunction{Environment.NewLine}" +
            $"actor \"Caller\" as caller{Environment.NewLine}" +
            $"entity \"{serviceFullName}\" as {serviceShortName}{Environment.NewLine}" +
            $"caller -> {serviceShortName}: {httpMethod}: {uri.PathAndQuery}{Environment.NewLine}" +
            $"note left{Environment.NewLine}" +
            $"{requestPlantUmlNoteContent}{Environment.NewLine}" +
            $"end note{Environment.NewLine}" +
            $"{serviceShortName} --> caller: {httpStatusCode.ToString().Titleize()}{Environment.NewLine}" +
            $"note right{Environment.NewLine}" +
            $"{responsePlantUmlNoteContent}{Environment.NewLine}" +
            $"end note{Environment.NewLine}" +
            $"@enduml{Environment.NewLine}";

        return plantUml;
    }

    private static string GetPlantUmlForRequestOrResponseNote(IEnumerable<(string Key, string? Value)> headers, string? content, string[] excludedHeaders)
    {
        return (($"{string.Join(Environment.NewLine, headers
            .Where(y => !excludedHeaders.Contains(y.Key))
            .Select(y => $"$my_code(gray)[{y.Key}={y.Value}]"))}" + Environment.NewLine).TrimStart() +
                Environment.NewLine +
                $"{(content?.StartsWith("{") ?? false
                    ? JToken.Parse(content).ToString()
                    : content?.Replace("&", Environment.NewLine))}".Trim()).Trim();
    }

    private static string CreatePlantUmlImageTag(string encodedPlantUml, string plantUmlServerRendererUrl)
    {
        return $"<img src=\"{plantUmlServerRendererUrl.TrimEnd('/')}/{encodedPlantUml}\">";
    }

    private static string CreatePlantUmlImageTag(RequestResponseLog requestResponseLog, string plantUmlServerRendererUrl, Func<string, string>? processor = null)
    {
        var plantUml = CreatePlantUml(requestResponseLog, processor);
        var encodedPlantUml = PlantUmlTextEncoder.Encode(plantUml); ;
        var plantUmlImageTag = CreatePlantUmlImageTag(encodedPlantUml, plantUmlServerRendererUrl);
        return plantUmlImageTag;
    }
}