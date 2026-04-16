using System.Text.Json;
using System.Text.Json.Serialization;

namespace TestTrackingDiagrams.Tracking;

public static class TrackingSafeSerializer
{
    private static readonly TrackingSerializerOptions DefaultOptions = new();

    public static string? Serialize(object? value, TrackingSerializerOptions? options = null)
    {
        if (value is null) return null;

        options ??= DefaultOptions;

        // Skip mock proxy objects (Castle.Proxies namespace)
        if (options.SkipMockProxies && value.GetType().FullName?.Contains("Castle.Proxies") == true)
            return "\"<mock proxy>\"";

        // Unwrap completed Tasks
        if (options.UnwrapTasks && value is Task task)
            return SerializeTask(task, options);

        // Filter elements from arrays
        if (value is object[] array)
            return SerializeFilteredArray(array, options);

        return SerializeValue(value, options);
    }

    private static string? SerializeTask(Task task, TrackingSerializerOptions options)
    {
        if (!task.IsCompletedSuccessfully) return "\"<pending Task>\"";

        var taskType = task.GetType();
        if (!taskType.IsGenericType) return "\"<completed Task>\"";

        var resultProperty = taskType.GetProperty("Result");
        var result = resultProperty?.GetValue(task);
        return result is null ? null : SerializeValue(result, options);
    }

    private static string SerializeFilteredArray(object[] array, TrackingSerializerOptions options)
    {
        var filtered = array.Where(item =>
        {
            if (item is null) return true;
            var type = item.GetType();
            if (options.FilterCancellationTokens && type == typeof(CancellationToken)) return false;
            if (options.SkipTypes.Length > 0 && options.SkipTypes.Contains(type)) return false;
            if (options.SkipMockProxies && type.FullName?.Contains("Castle.Proxies") == true) return false;
            return true;
        }).ToArray();

        return SerializeValue(filtered, options);
    }

    private static string SerializeValue(object value, TrackingSerializerOptions options)
    {
        try
        {
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = options.WriteIndented,
                MaxDepth = options.MaxDepth,
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            return JsonSerializer.Serialize(value, value.GetType(), jsonOptions);
        }
        catch
        {
            return $"\"{value}\"";
        }
    }
}

public class TrackingSerializerOptions
{
    public int MaxDepth { get; set; } = 10;
    public bool WriteIndented { get; set; } = true;
    public bool UnwrapTasks { get; set; } = true;
    public bool FilterCancellationTokens { get; set; } = true;
    public bool SkipMockProxies { get; set; } = true;
    public Type[] SkipTypes { get; set; } = [];
}
