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
        var instance = Activator.CreateInstance<T>();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToDictionary(p => SanitizeName(p.Name), p => p);

        for (var i = 0; i < columnNames.Length && i < values.Length; i++)
        {
            var key = SanitizeName(columnNames[i]);
            if (properties.TryGetValue(key, out var prop))
            {
                prop.SetValue(instance, ConvertValue(values[i], prop.PropertyType));
            }
        }

        return instance;
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
