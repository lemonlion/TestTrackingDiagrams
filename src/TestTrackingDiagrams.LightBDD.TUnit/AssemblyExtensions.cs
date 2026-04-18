using System.Collections;
using System.Reflection;

namespace TestTrackingDiagrams.LightBDD.TUnit;

internal static class AssemblyExtensions
{
    public static int CountNumberOfTestsInAssembly(this Assembly assembly)
    {
        return assembly.GetTypes()
            .SelectMany(t => t.GetMethods().AsParallel())
            .Where(m => m.GetCustomAttributes().Any(a => a.GetType().Name is "ScenarioAttribute" or "TestAttribute"))
            .Sum(x =>
                CalculateNumberOfArgumentsTests(x));
    }

    private static int CalculateNumberOfArgumentsTests(MemberInfo method)
    {
        var arguments = method.GetCustomAttributes().Where(a => a.GetType().Name == "ArgumentsAttribute").ToArray();
        return arguments.Any() ? arguments.Length : 1;
    }
}
