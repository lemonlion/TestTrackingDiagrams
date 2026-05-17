using System.Reflection;

namespace Kronikol.TabularAttributes;

/// <summary>
/// Reads <see cref="InputsAttribute"/>, <see cref="OutputsAttribute"/>, and
/// <see cref="HeadOutAttribute"/> from a test method and constructs the
/// <see cref="TabularInputs{T}"/> / <see cref="TabularOutputs{T}"/> parameter values.
/// Called by framework-specific <c>[HeadIn]</c> attributes.
/// </summary>
public static class TabularResolver
{
    /// <summary>
    /// Resolves tabular parameters for a test method.
    /// </summary>
    /// <param name="method">The test method decorated with tabular attributes.</param>
    /// <param name="inputColumnNames">
    /// Column names from the <c>[HeadIn]</c> attribute, or <c>null</c> to infer from property names.
    /// </param>
    /// <returns>
    /// A single-element array of <c>object[]</c> representing one test invocation,
    /// with <see cref="TabularInputs{T}"/> and/or <see cref="TabularOutputs{T}"/>
    /// at the correct parameter positions.
    /// </returns>
    public static object[] Resolve(MethodInfo method, string[]? inputColumnNames)
    {
        var parameters = method.GetParameters();
        var inputAttributes = method.GetCustomAttributes<InputsAttribute>().ToArray();
        var outputAttributes = method.GetCustomAttributes<OutputsAttribute>().ToArray();
        var headOutAttribute = method.GetCustomAttribute<HeadOutAttribute>();

        var result = new object[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var paramType = parameters[i].ParameterType;

            if (IsTabularInputsType(paramType))
            {
                var elementType = paramType.GetGenericArguments()[0];
                var columnNames = inputColumnNames is { Length: > 0 }
                    ? inputColumnNames
                    : InferColumnNames(elementType);
                result[i] = CreateTypedInputs(elementType, columnNames, inputAttributes);
            }
            else if (IsTabularOutputsType(paramType))
            {
                var elementType = paramType.GetGenericArguments()[0];
                var columnNames = headOutAttribute?.ColumnNames is { Length: > 0 }
                    ? headOutAttribute.ColumnNames
                    : InferColumnNames(elementType);
                result[i] = CreateTypedOutputs(elementType, columnNames, outputAttributes);
            }
        }

        return result;
    }

    private static bool IsTabularInputsType(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(TabularInputs<>);

    private static bool IsTabularOutputsType(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(TabularOutputs<>);

    private static string[] InferColumnNames(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .Select(p => p.Name)
            .ToArray();

    private static object CreateTypedInputs(
        Type elementType, string[] columnNames, InputsAttribute[] attributes)
    {
        var deserialize = typeof(TabularDeserializer)
            .GetMethod(nameof(TabularDeserializer.Deserialize))!
            .MakeGenericMethod(elementType);

        var typedArray = Array.CreateInstance(elementType, attributes.Length);
        for (var i = 0; i < attributes.Length; i++)
            typedArray.SetValue(
                deserialize.Invoke(null, [columnNames, attributes[i].Values]), i);

        return Activator.CreateInstance(
            typeof(TabularInputs<>).MakeGenericType(elementType),
            typedArray, columnNames)!;
    }

    private static object CreateTypedOutputs(
        Type elementType, string[] columnNames, OutputsAttribute[] attributes)
    {
        var deserialize = typeof(TabularDeserializer)
            .GetMethod(nameof(TabularDeserializer.Deserialize))!
            .MakeGenericMethod(elementType);

        var typedArray = Array.CreateInstance(elementType, attributes.Length);
        for (var i = 0; i < attributes.Length; i++)
            typedArray.SetValue(
                deserialize.Invoke(null, [columnNames, attributes[i].Values]), i);

        return Activator.CreateInstance(
            typeof(TabularOutputs<>).MakeGenericType(elementType),
            typedArray, columnNames)!;
    }
}
