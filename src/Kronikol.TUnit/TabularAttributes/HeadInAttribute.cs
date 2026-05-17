using System.Reflection;
using TUnit.Core;

namespace Kronikol.TabularAttributes;

/// <summary>
/// TUnit data source attribute that reads <see cref="InputsAttribute"/>,
/// <see cref="OutputsAttribute"/>, and <see cref="HeadOutAttribute"/> from the test method
/// to provide <see cref="TabularInputs{T}"/> and <see cref="TabularOutputs{T}"/>
/// as method parameters. Use with <c>[Test]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class HeadInAttribute : Attribute, IDataSourceAttribute
{
    private readonly string[] _columnNames;

    public HeadInAttribute(params string[] columnNames) => _columnNames = columnNames;

    public bool SkipIfEmpty { get; set; }

    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(
        DataGeneratorMetadata dataGeneratorMetadata)
    {
        var testInfo = dataGeneratorMetadata.TestInformation!;
        var methodInfo = testInfo.Type.GetMethod(testInfo.Name,
            testInfo.Parameters.Select(p => p.Type).ToArray());

        var args = TabularResolver.Resolve(methodInfo!,
            _columnNames.Length > 0 ? _columnNames : null);

        // Register IDisposable args for auto-verify on test completion
        var builderContext = dataGeneratorMetadata.TestBuilderContext.Current;
        if (builderContext != null)
        {
            foreach (var arg in args)
            {
                if (arg is IDisposable disposable)
                {
                    builderContext.Events.OnDispose += (object _, TestContext _) =>
                    {
                        disposable.Dispose();
                        return ValueTask.CompletedTask;
                    };
                }
            }
        }

        yield return () => Task.FromResult<object?[]?>(args);
    }
}
