using System.Reflection;

namespace TestTrackingDiagrams.TabularAttributes;

/// <summary>
/// Creates instances of <c>T</c> from column names and row values,
/// matching columns to public writable properties via <see cref="SanitizeName"/>.
/// </summary>
public static class TabularDeserializer
{
    public static T Deserialize<T>(string[] columnNames, object?[] values)
    {
        var valueMap = new Dictionary<string, object?>();
        for (var i = 0; i < columnNames.Length && i < values.Length; i++)
            valueMap[SanitizeName(columnNames[i])] = values[i];

        // Try parameterless constructor first (classes with property setters)
        var ctor = typeof(T).GetConstructor(Type.EmptyTypes);
        if (ctor != null)
        {
            var instance = (T)ctor.Invoke(null);
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToDictionary(p => SanitizeName(p.Name), p => p);

            foreach (var kvp in valueMap)
                if (properties.TryGetValue(kvp.Key, out var prop))
                    prop.SetValue(instance, ConvertValue(kvp.Value, prop.PropertyType));

            return instance;
        }

        // Fall back to constructor-based instantiation (records)
        var bestCtor = typeof(T).GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length).First();
        var parameters = bestCtor.GetParameters();
        var args = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            var paramKey = SanitizeName(parameters[i].Name!);
            if (valueMap.TryGetValue(paramKey, out var value))
                args[i] = ConvertValue(value, parameters[i].ParameterType);
            else
                args[i] = parameters[i].ParameterType.IsValueType
                    ? Activator.CreateInstance(parameters[i].ParameterType) : null;
        }
        return (T)bestCtor.Invoke(args);
    }

    internal static string SanitizeName(string name) =>
        name.Replace(" ", "").Replace("&", "And").ToLowerInvariant();

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value is null)
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlying == typeof(string))
            return value.ToString();

        if (underlying.IsEnum)
            return value is string s
                ? Enum.Parse(underlying, s, ignoreCase: true)
                : Enum.ToObject(underlying, value);

        if (underlying.IsInstanceOfType(value))
            return value;

        return Convert.ChangeType(value, underlying);
    }
}
