using System.Reflection;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;
using TestTrackingDiagrams.TabularAttributes;

namespace TestTrackingDiagrams.TabularAttributes;

/// <summary>
/// xUnit v3 data source attribute that reads <see cref="InputsAttribute"/>,
/// <see cref="OutputsAttribute"/>, and <see cref="HeadOutAttribute"/> from the test method
/// to provide <see cref="TabularInputs{T}"/> and <see cref="TabularOutputs{T}"/>
/// as method parameters. Use with <c>[Theory]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class HeadInAttribute : DataAttribute
{
    private readonly string[] _columnNames;

    public HeadInAttribute(params string[] columnNames) => _columnNames = columnNames;

    public override bool SupportsDiscoveryEnumeration() => false;

    public override ValueTask<IReadOnlyCollection<ITheoryDataRow>> GetData(
        MethodInfo testMethod, DisposalTracker disposalTracker)
    {
        var args = TabularResolver.Resolve(testMethod,
            _columnNames.Length > 0 ? _columnNames : null);

        foreach (var arg in args)
        {
            if (arg is IDisposable disposable)
                disposalTracker.Add(disposable);
        }

        IReadOnlyCollection<ITheoryDataRow> result = [new TheoryDataRow(args)];
        return new(result);
    }
}
