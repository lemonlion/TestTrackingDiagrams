using System.Collections.Generic;
using System.Reflection;
using TestTrackingDiagrams.TabularAttributes;
using Xunit.Sdk;

namespace TestTrackingDiagrams.TabularAttributes;

/// <summary>
/// xUnit v2 data source attribute that reads <see cref="InputsAttribute"/>,
/// <see cref="OutputsAttribute"/>, and <see cref="HeadOutAttribute"/> from the test method
/// to provide <see cref="TabularInputs{T}"/> and <see cref="TabularOutputs{T}"/>
/// as method parameters. Use with <c>[Theory]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class HeadInAttribute : DataAttribute
{
    private readonly string[] _columnNames;

    public HeadInAttribute(params string[] columnNames) => _columnNames = columnNames;

    public override IEnumerable<object[]> GetData(MethodInfo testMethod)
    {
        var args = TabularResolver.Resolve(testMethod,
            _columnNames.Length > 0 ? _columnNames : null);
        yield return args;
    }
}
