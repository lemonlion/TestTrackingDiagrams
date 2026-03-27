using System.Reflection;
using LightBDD.XUnit2;
using Xunit;

namespace LightBDD.Contrib.ReportingEnhancements.Reports;

public static class AssemblyExtensions
{
    public static int CountNumberOfTestsInAssembly(this Assembly assembly)
    {
        return assembly.GetTypes()
            .SelectMany(t => t.GetMethods().AsParallel())
            .Where(m => m.GetCustomAttributes<ScenarioAttribute>().Any())
            .Sum(x =>
                CalculateNumberOfInlineTests(x) + CalculateNumberOfClassDataTests(x) + CalculateNumberOfMemberDataTests(x));
    }

    private static int CalculateNumberOfInlineTests(MemberInfo method)
    {
        // Each InlineData counts as an additional test
        var inlineData = method.GetCustomAttributes<InlineDataAttribute>().ToArray();
        return inlineData.Any() ? inlineData.Length : 1;
    }

    private static int CalculateNumberOfClassDataTests(MemberInfo method)
    {
        var classData = method.GetCustomAttribute<ClassDataAttribute>();

        if (classData is null)
            return 0;

        var classDataTypeInstance = Activator.CreateInstance(classData.Class) as TheoryData;

        return classDataTypeInstance?.Count() ?? 0;
    }

    private static int CalculateNumberOfMemberDataTests(MethodInfo method)
    {
        var memberData = method.GetCustomAttribute<MemberDataAttribute>();

        return memberData is null ? 0 : memberData.GetData(method).Count();
    }
}