using System.Text.Json;

namespace Example.Api.Tests.Component.LightBDD.XUnit.Extensions;

#pragma warning disable CS1998
public static class ObjectExtensions
{
    public static T GetWithPropertyRemoved<T>(this T @object, string propertyName)
    {
        return @object.GetWithPropertyValueChanged(propertyName, null);
    }

    public static T GetWithPropertyValueChanged<T>(this T @object, string propertyName, string propertyValue)
    {
        var objectAsJson = JsonSerializer.SerializeToNode(@object);
        objectAsJson![propertyName] = propertyValue;
        return objectAsJson.Deserialize<T>()!;
    }

    public static async Task<IEnumerable<TResult>> SelectAsync<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, Task<TResult>> method)
    {
        return await Task.WhenAll(source.Select(async s => await method(s)));
    }
}