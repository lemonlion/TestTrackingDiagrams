using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;
using TestTrackingDiagrams.TabularAttributes;

namespace TestTrackingDiagrams.TabularAttributes;

/// <summary>
/// NUnit 4 test builder attribute that reads <see cref="InputsAttribute"/>,
/// <see cref="OutputsAttribute"/>, and <see cref="HeadOutAttribute"/> from the test method
/// to provide <see cref="TabularInputs{T}"/> and <see cref="TabularOutputs{T}"/>
/// as method parameters. Use with <c>[Test]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class HeadInAttribute : NUnitAttribute, ITestBuilder
{
    private readonly string[] _columnNames;

    public HeadInAttribute(params string[] columnNames) => _columnNames = columnNames;

    public IEnumerable<TestMethod> BuildFrom(IMethodInfo method, Test? suite)
    {
        var args = TabularResolver.Resolve(method.MethodInfo,
            _columnNames.Length > 0 ? _columnNames : null);
        var parameters = new TestCaseParameters(args);
        yield return new NUnitTestCaseBuilder().BuildTestMethod(method, suite, parameters);
    }
}
