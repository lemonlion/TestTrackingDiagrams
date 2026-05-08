using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestTrackingDiagrams.TabularAttributes;

namespace TestTrackingDiagrams.TabularAttributes;

/// <summary>
/// MSTest data source attribute that reads <see cref="InputsAttribute"/>,
/// <see cref="OutputsAttribute"/>, and <see cref="HeadOutAttribute"/> from the test method
/// to provide <see cref="TabularInputs{T}"/> and <see cref="TabularOutputs{T}"/>
/// as method parameters. Use with <c>[TestMethod]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class HeadInAttribute : Attribute, ITestDataSource
{
    private readonly string[] _columnNames;

    public HeadInAttribute(params string[] columnNames) => _columnNames = columnNames;

    public IEnumerable<object[]> GetData(MethodInfo methodInfo)
    {
        var args = TabularResolver.Resolve(methodInfo,
            _columnNames.Length > 0 ? _columnNames : null);
        yield return args;
    }

    public string? GetDisplayName(MethodInfo methodInfo, object?[]? data) => null;
}
