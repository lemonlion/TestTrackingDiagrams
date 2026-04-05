using System.Text.Json;
using System.Text.RegularExpressions;

var path = @"C:\dev\NewDay.ClosedLoop.MerchantGatewayService\tests\NewDay.ClosedLoop.MerchantGatewayService.Tests.Component\bin\Debug\net8.0\Reports\FeaturesReport - Copy.html";

Console.WriteLine("Reading file...");
var content = File.ReadAllText(path);

// Extract __iflowSegments JSON
var match = Regex.Match(content, @"window\.__iflowSegments\s*=\s*(\{.*?\});\s*</script>", RegexOptions.Singleline);
if (!match.Success) { Console.WriteLine("No __iflowSegments found"); return; }

var json = match.Groups[1].Value;
Console.WriteLine($"JSON size: {json.Length / 1024.0:F0} KB");

using var doc = JsonDocument.Parse(json);
var root = doc.RootElement;

int withContent = 0, withMessage = 0, withFlameData = 0;
var contentLengths = new List<int>();
var messageCounts = new Dictionary<string, int>();

foreach (var prop in root.EnumerateObject())
{
    var key = prop.Name;
    var obj = prop.Value;

    if (obj.TryGetProperty("content", out var contentEl))
    {
        withContent++;
        contentLengths.Add(contentEl.GetString()!.Length);
        if (obj.TryGetProperty("flameData", out _)) withFlameData++;
    }
    else if (obj.TryGetProperty("message", out var msgEl))
    {
        withMessage++;
        var msg = msgEl.GetString()!;
        messageCounts.TryGetValue(msg, out var c);
        messageCounts[msg] = c + 1;
    }
}

Console.WriteLine($"\nTotal segments: {root.EnumerateObject().Count()}");
Console.WriteLine($"With content (have diagrams): {withContent}");
Console.WriteLine($"With message (no data): {withMessage}");
Console.WriteLine($"With flameData: {withFlameData}");

if (contentLengths.Count > 0)
{
    contentLengths.Sort();
    Console.WriteLine($"\nContent lengths: min={contentLengths[0]}, max={contentLengths[^1]}, " +
        $"avg={contentLengths.Average():F0}, median={contentLengths[contentLengths.Count/2]}");
}

Console.WriteLine("\nMessages:");
foreach (var kv in messageCounts.OrderByDescending(x => x.Value))
    Console.WriteLine($"  {kv.Value}x: \"{kv.Key}\"");

// Now analyze what types of arrows have data vs no data
// Look at the PlantUML source for iflow links
Console.WriteLine("\n--- PlantUML Link Analysis ---");
var iflowLinks = Regex.Matches(content, @"\[\[#(iflow-[^\s\]]+)\s+([^\]]+)\]\]");
Console.WriteLine($"Total #iflow- links in PlantUML: {iflowLinks.Count}");

// Check which links have matching segments with content vs message
int linksWithContent = 0, linksWithNoData = 0, linksNoSegment = 0;
var noDataExamples = new List<string>();
var withDataExamples = new List<string>();
foreach (Match m in iflowLinks)
{
    var segId = m.Groups[1].Value;
    var label = m.Groups[2].Value;
    if (root.TryGetProperty(segId, out var seg))
    {
        if (seg.TryGetProperty("content", out _))
        {
            linksWithContent++;
            if (withDataExamples.Count < 5) withDataExamples.Add($"{segId} → {label}");
        }
        else
        {
            linksWithNoData++;
            if (noDataExamples.Count < 10) noDataExamples.Add($"{segId} → {label}");
        }
    }
    else
    {
        linksNoSegment++;
    }
}
Console.WriteLine($"Links with content: {linksWithContent}");
Console.WriteLine($"Links with no data: {linksWithNoData}");
Console.WriteLine($"Links with no matching segment: {linksNoSegment}");

Console.WriteLine("\nSample links WITH content:");
foreach (var ex in withDataExamples) Console.WriteLine($"  {ex}");

Console.WriteLine("\nSample links with NO data:");
foreach (var ex in noDataExamples) Console.WriteLine($"  {ex}");

// Now look at the ACTORS/participants involved - extract the PlantUML arrows
Console.WriteLine("\n--- Arrow Direction Analysis ---");
// PlantUML arrows: "Actor -> Service: [[#iflow-xxx label]]"  
var arrowPattern = Regex.Matches(content, @"(\w+)\s+->\s+(\w+)\s*:\s*\[\[#(iflow-[^\s\]]+)\s+([^\]]+)\]\]");
var directionStats = new Dictionary<string, (int withData, int noData)>();
foreach (Match m in arrowPattern)
{
    var caller = m.Groups[1].Value;
    var service = m.Groups[2].Value;
    var segId = m.Groups[3].Value;
    var direction = $"{caller} → {service}";
    
    var hasData = root.TryGetProperty(segId, out var seg) && seg.TryGetProperty("content", out _);
    
    directionStats.TryGetValue(direction, out var stats);
    if (hasData) directionStats[direction] = (stats.withData + 1, stats.noData);
    else directionStats[direction] = (stats.withData, stats.noData + 1);
}

Console.WriteLine("\nURL pattern analysis:");
// Extract base paths from with-data vs no-data links
var withDataPaths = new Dictionary<string, int>();
var noDataPaths = new Dictionary<string, int>();

foreach (Match m in iflowLinks)
{
    var segId = m.Groups[1].Value;
    var label = m.Groups[2].Value.Replace("\\n", "").Trim();
    // Extract base path (method + first path segment)
    var pathMatch = Regex.Match(label, @"^(\w+):\s*(/[^/]+(?:/[^/]+)?)");
    var basePath = pathMatch.Success ? pathMatch.Groups[1].Value + " " + Regex.Replace(pathMatch.Groups[2].Value, @"[0-9a-f]{8}-[0-9a-f]{4}-", "...") : label;
    // Truncate to service name level
    basePath = basePath.Length > 60 ? basePath[..60] : basePath;
    
    var hasData = root.TryGetProperty(segId, out var seg2) && seg2.TryGetProperty("content", out _);
    var dict = hasData ? withDataPaths : noDataPaths;
    dict.TryGetValue(basePath, out var count);
    dict[basePath] = count + 1;
}

Console.WriteLine("\nPaths WITH data (top 15):");
foreach (var kv in withDataPaths.OrderByDescending(x => x.Value).Take(15))
    Console.WriteLine($"  {kv.Value,5}x  {kv.Key}");

Console.WriteLine("\nPaths with NO data (top 15):");
foreach (var kv in noDataPaths.OrderByDescending(x => x.Value).Take(15))
    Console.WriteLine($"  {kv.Value,5}x  {kv.Key}");

// Check segments that have content - look at what the activity diagrams contain
Console.WriteLine("\n--- Activity Diagram Span Sources ---");
var sourceCount = new Dictionary<string, int>();
foreach (var prop in root.EnumerateObject())
{
    if (!prop.Value.TryGetProperty("content", out var c)) continue;
    var html = c.GetString()!;
    // Extract swimlane names from HTML-encoded PlantUML: |SourceName|
    foreach (Match sm in Regex.Matches(html, @"\|([^|]+)\|"))
    {
        var src = sm.Groups[1].Value;
        sourceCount.TryGetValue(src, out var sc);
        sourceCount[src] = sc + 1;
    }
}
Console.WriteLine("Activity diagram sources (swimlanes):");
foreach (var kv in sourceCount.OrderByDescending(x => x.Value))
    Console.WriteLine($"  {kv.Value,5}x  {kv.Key}");
