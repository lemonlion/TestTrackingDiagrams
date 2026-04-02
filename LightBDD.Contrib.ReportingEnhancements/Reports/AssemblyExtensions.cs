using System.Collections;
using System.Reflection;

namespace LightBDD.Contrib.ReportingEnhancements.Reports;

public static class AssemblyExtensions
{
    public static int CountNumberOfTestsInAssembly(this Assembly assembly)
    {
        return assembly.GetTypes()
            .SelectMany(t => t.GetMethods().AsParallel())
            .Where(m => m.GetCustomAttributes().Any(a => a.GetType().Name == "ScenarioAttribute"))
            .Sum(x =>
                CalculateNumberOfInlineTests(x) + CalculateNumberOfClassDataTests(x) + CalculateNumberOfMemberDataTests(x));
    }

    private static int CalculateNumberOfInlineTests(MemberInfo method)
    {
        // Each InlineData counts as an additional test
        var inlineData = method.GetCustomAttributes().Where(a => a.GetType().Name == "InlineDataAttribute").ToArray();
        return inlineData.Any() ? inlineData.Length : 1;
    }

    private static int CalculateNumberOfClassDataTests(MemberInfo method)
    {
        var classData = method.GetCustomAttributes().FirstOrDefault(a => a.GetType().Name == "ClassDataAttribute");

        if (classData is null)
            return 0;

        var classProperty = classData.GetType().GetProperty("Class");
        var classType = classProperty?.GetValue(classData) as Type;

        if (classType is null)
            return 0;

        var instance = Activator.CreateInstance(classType);

        return instance is IEnumerable enumerable ? enumerable.Cast<object>().Count() : 0;
    }

    private static int CalculateNumberOfMemberDataTests(MethodInfo method)
    {
        var memberData = method.GetCustomAttributes().FirstOrDefault(a => a.GetType().Name == "MemberDataAttribute");

        if (memberData is null)
            return 0;

        var getDataMethod = memberData.GetType().GetMethod("GetData");
        if (getDataMethod is null)
            return 0;

        try
        {
            var data = getDataMethod.Invoke(memberData, [method]);
            return data is IEnumerable enumerable ? enumerable.Cast<object>().Count() : 0;
        }
        catch
        {
            return 0;
        }
    }
}