using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace TestTrackingDiagrams.StepTracking;

/// <summary>
/// Core IL weaving logic for step tracking. Opens a compiled assembly with Cecil,
/// finds methods decorated with [GivenStep], [WhenStep], [ThenStep], [AndStep],
/// [ButStep], or [Step] attributes, and wraps them with StepCollector.StartStep/CompleteStep calls.
/// </summary>
public class StepWeaver
{
    private readonly TaskLoggingHelper? _log;
    private readonly string[] _searchDirectories;

    private static readonly HashSet<string> StepAttributeNames = new(StringComparer.Ordinal)
    {
        "GivenStepAttribute",
        "WhenStepAttribute",
        "ThenStepAttribute",
        "AndStepAttribute",
        "ButStepAttribute",
        "StepAttribute"
    };

    private static readonly Dictionary<string, string> AttributeToKeyword = new(StringComparer.Ordinal)
    {
        ["GivenStepAttribute"] = "Given",
        ["WhenStepAttribute"] = "When",
        ["ThenStepAttribute"] = "Then",
        ["AndStepAttribute"] = "And",
        ["ButStepAttribute"] = "But"
        // StepAttribute has no keyword (null)
    };

    public StepWeaver(TaskLoggingHelper? log = null, string[]? searchDirectories = null)
    {
        _log = log;
        _searchDirectories = searchDirectories ?? Array.Empty<string>();
    }

    public WeaveResult Weave(string assemblyPath, string pdbPath)
    {
        var result = new WeaveResult();

        var assemblyBytes = File.ReadAllBytes(assemblyPath);
        var pdbBytes = File.ReadAllBytes(pdbPath);

        var readerParams = new ReaderParameters
        {
            ReadSymbols = true,
            SymbolReaderProvider = new DefaultSymbolReaderProvider(throwIfNoSymbol: false),
            ReadingMode = ReadingMode.Immediate,
            AssemblyResolver = CreateResolver(assemblyPath)
        };
        readerParams.SymbolStream = new MemoryStream(pdbBytes);

        using var assemblyStream = new MemoryStream(assemblyBytes);
        using var assembly = AssemblyDefinition.ReadAssembly(assemblyStream, readerParams);

        // Fast-path: check for [assembly: TrackSteps]
        if (!HasTrackStepsAttribute(assembly))
        {
            result.SkipReason = "No TrackSteps attribute found";
            return result;
        }

        // Guard against double-weaving
        if (HasAlreadyBeenWeaved(assembly))
        {
            result.SkipReason = "Assembly already weaved (sentinel found)";
            return result;
        }

        // Find StepCollector method references
        var stepMethods = GetStepCollectorMethodReferences(assembly);
        if (stepMethods == null)
        {
            result.SkipReason = "TestTrackingDiagrams core library not referenced or too old";
            return result;
        }

        // Also need Exception.get_Message
        var exceptionType = assembly.MainModule.ImportReference(typeof(Exception));
        var getMessageMethod = assembly.MainModule.ImportReference(
            typeof(Exception).GetProperty("Message")!.GetGetMethod()!);

        foreach (var type in assembly.MainModule.GetTypes())
        {
            foreach (var method in type.Methods)
            {
                if (!method.HasBody)
                    continue;

                var stepAttr = GetStepAttribute(method);
                if (stepAttr == null)
                    continue;

                var keyword = GetKeyword(stepAttr);
                var stepText = GetStepText(stepAttr, method);

                WrapMethodWithStepTracking(method, keyword, stepText, stepMethods.Value, getMessageMethod, exceptionType);
                result.WeavedCount++;
            }
        }

        if (result.WeavedCount > 0)
        {
            AddWeavedSentinel(assembly);

            using var outputAssembly = new MemoryStream();
            using var outputPdb = new MemoryStream();

            var writerParams = new WriterParameters
            {
                WriteSymbols = true,
                SymbolWriterProvider = new PortablePdbWriterProvider(),
                SymbolStream = outputPdb
            };
            assembly.Write(outputAssembly, writerParams);

            File.WriteAllBytes(assemblyPath, outputAssembly.ToArray());
            File.WriteAllBytes(pdbPath, outputPdb.ToArray());
        }

        return result;
    }

    private void WrapMethodWithStepTracking(
        MethodDefinition method,
        string? keyword,
        string stepText,
        (MethodReference StartStep, MethodReference CompleteStepPassed, MethodReference CompleteStepFailed) stepMethods,
        MethodReference getMessageMethod,
        TypeReference exceptionType)
    {
        var body = method.Body;
        var il = body.GetILProcessor();
        body.SimplifyMacros();

        // Build parameter name and value arrays
        var paramNames = GetParameterNames(method);
        var hasParams = paramNames.Length > 0;

        // Create local variables for the exception in catch block
        var exLocal = new VariableDefinition(exceptionType);
        body.Variables.Add(exLocal);

        // Create local for parameter arrays if needed
        VariableDefinition? paramNamesLocal = null;
        VariableDefinition? paramValuesLocal = null;
        if (hasParams)
        {
            paramNamesLocal = new VariableDefinition(method.Module.ImportReference(typeof(string[])));
            paramValuesLocal = new VariableDefinition(method.Module.ImportReference(typeof(object[])));
            body.Variables.Add(paramNamesLocal);
            body.Variables.Add(paramValuesLocal);
        }

        body.InitLocals = true;

        // Get the first original instruction (entry point of try block)
        var originalFirst = body.Instructions[0];

        // === Emit preamble: build param arrays and call StartStep ===
        var preamble = new List<Instruction>();

        if (hasParams)
        {
            // Build string[] paramNames
            preamble.Add(il.Create(OpCodes.Ldc_I4, paramNames.Length));
            preamble.Add(il.Create(OpCodes.Newarr, method.Module.ImportReference(typeof(string))));
            for (int i = 0; i < paramNames.Length; i++)
            {
                preamble.Add(il.Create(OpCodes.Dup));
                preamble.Add(il.Create(OpCodes.Ldc_I4, i));
                preamble.Add(il.Create(OpCodes.Ldstr, paramNames[i]));
                preamble.Add(il.Create(OpCodes.Stelem_Ref));
            }
            preamble.Add(il.Create(OpCodes.Stloc, paramNamesLocal!));

            // Build object[] paramValues from method arguments
            preamble.Add(il.Create(OpCodes.Ldc_I4, paramNames.Length));
            preamble.Add(il.Create(OpCodes.Newarr, method.Module.ImportReference(typeof(object))));
            var methodParams = method.Parameters;
            for (int i = 0; i < paramNames.Length; i++)
            {
                preamble.Add(il.Create(OpCodes.Dup));
                preamble.Add(il.Create(OpCodes.Ldc_I4, i));

                // Load argument (offset by 1 for instance methods)
                var argIndex = method.IsStatic ? i : i + 1;
                preamble.Add(il.Create(OpCodes.Ldarg, argIndex));

                // Box value types
                var paramType = methodParams[i].ParameterType;
                if (paramType.IsValueType || paramType.IsGenericParameter)
                {
                    preamble.Add(il.Create(OpCodes.Box, paramType));
                }

                preamble.Add(il.Create(OpCodes.Stelem_Ref));
            }
            preamble.Add(il.Create(OpCodes.Stloc, paramValuesLocal!));
        }

        // Call StepCollector.StartStep(keyword, text, paramNames, paramValues)
        if (keyword != null)
            preamble.Add(il.Create(OpCodes.Ldstr, keyword));
        else
            preamble.Add(il.Create(OpCodes.Ldnull));

        preamble.Add(il.Create(OpCodes.Ldstr, stepText));

        if (hasParams)
        {
            preamble.Add(il.Create(OpCodes.Ldloc, paramNamesLocal!));
            preamble.Add(il.Create(OpCodes.Ldloc, paramValuesLocal!));
        }
        else
        {
            preamble.Add(il.Create(OpCodes.Ldnull));
            preamble.Add(il.Create(OpCodes.Ldnull));
        }

        preamble.Add(il.Create(OpCodes.Call, stepMethods.StartStep));

        // Insert preamble before original first instruction
        foreach (var instr in preamble)
        {
            il.InsertBefore(originalFirst, instr);
        }

        // === Wrap original body in try/catch ===
        // Layout:
        //   [preamble: StartStep call]
        //   TryStart: [original body with ret→leave]
        //   TryEnd/HandlerStart: [catch: stloc ex, CompleteStep(false, msg), rethrow]
        //   HandlerEnd: [success: CompleteStep(true, null), ret]  ← leave target

        // Create the catch block instructions
        var exLocal2 = exLocal; // already declared above
        var catchStart = il.Create(OpCodes.Stloc, exLocal2);
        var catchLoadFalse = il.Create(OpCodes.Ldc_I4_0); // false
        var catchLoadMessage = il.Create(OpCodes.Ldloc, exLocal2);
        var catchGetMessage = il.Create(OpCodes.Callvirt, getMessageMethod);
        var catchCompleteCall = il.Create(OpCodes.Call, stepMethods.CompleteStepFailed);
        var catchRethrow = il.Create(OpCodes.Rethrow);

        // Create the success block (AFTER catch) — this is where leave jumps to
        var successNop = il.Create(OpCodes.Nop);
        var successLoadTrue = il.Create(OpCodes.Ldc_I4_1); // true
        var successLoadNull = il.Create(OpCodes.Ldnull); // null errorMessage
        var successCompleteCall = il.Create(OpCodes.Call, stepMethods.CompleteStepPassed);
        var finalRet = il.Create(OpCodes.Ret);

        // Replace all existing 'ret' instructions with 'leave' to the success block (after handler)
        var rets = body.Instructions.Where(i => i.OpCode == OpCodes.Ret).ToList();
        foreach (var ret in rets)
        {
            var leave = il.Create(OpCodes.Leave, successNop);
            il.Replace(ret, leave);
        }

        // Append catch block after the body
        body.Instructions.Add(catchStart);
        body.Instructions.Add(catchLoadFalse);
        body.Instructions.Add(catchLoadMessage);
        body.Instructions.Add(catchGetMessage);
        body.Instructions.Add(catchCompleteCall);
        body.Instructions.Add(catchRethrow);

        // Append success block AFTER catch (leave target)
        body.Instructions.Add(successNop);
        body.Instructions.Add(successLoadTrue);
        body.Instructions.Add(successLoadNull);
        body.Instructions.Add(successCompleteCall);
        body.Instructions.Add(finalRet);

        // Set up exception handler
        var handler = new ExceptionHandler(ExceptionHandlerType.Catch)
        {
            TryStart = originalFirst,
            TryEnd = catchStart,
            HandlerStart = catchStart,
            HandlerEnd = successNop,
            CatchType = exceptionType
        };

        body.ExceptionHandlers.Add(handler);
        body.OptimizeMacros();
    }

    private static string[] GetParameterNames(MethodDefinition method)
    {
        return method.Parameters
            .Select(p => p.Name)
            .ToArray();
    }

    private static string? GetKeyword(CustomAttribute attr)
    {
        var attrName = attr.AttributeType.Name;
        return AttributeToKeyword.TryGetValue(attrName, out var keyword) ? keyword : null;
    }

    private static string GetStepText(CustomAttribute attr, MethodDefinition method)
    {
        // Check for Description property override
        if (attr.HasProperties)
        {
            var descProp = attr.Properties
                .FirstOrDefault(p => p.Name == "Description");
            if (descProp.Name != null && descProp.Argument.Value is string desc && !string.IsNullOrEmpty(desc))
                return desc;
        }

        // Humanize method name: PascalCase → "Pascal case", underscores → spaces
        return HumanizeMethodName(method.Name);
    }

    public static string HumanizeMethodName(string methodName)
    {
        // Replace underscores with spaces
        var text = methodName.Replace("_", " ");

        // Split PascalCase: insert space before uppercase that follows lowercase
        text = Regex.Replace(text, @"(\p{Ll})(\p{Lu})", "$1 $2");
        // Handle sequences like "HTTPClient" → "HTTP Client"
        text = Regex.Replace(text, @"(\p{Lu}+)(\p{Lu}\p{Ll})", "$1 $2");

        // Collapse multiple spaces and trim
        text = Regex.Replace(text, @"\s+", " ").Trim();

        // Sentence case: uppercase first letter, lowercase the rest
        if (text.Length > 0)
            text = char.ToUpper(text[0]) + text.Substring(1).ToLowerInvariant();

        return text;
    }

    private static CustomAttribute? GetStepAttribute(MethodDefinition method)
    {
        if (!method.HasCustomAttributes)
            return null;

        return method.CustomAttributes
            .FirstOrDefault(a => StepAttributeNames.Contains(a.AttributeType.Name));
    }

    private (MethodReference StartStep, MethodReference CompleteStepPassed, MethodReference CompleteStepFailed)? GetStepCollectorMethodReferences(AssemblyDefinition assembly)
    {
        var module = assembly.MainModule;

        // Find the TestTrackingDiagrams assembly reference
        var ttdAssemblyRef = module.AssemblyReferences
            .FirstOrDefault(r => r.Name == "TestTrackingDiagrams");

        if (ttdAssemblyRef == null)
        {
            // Add it — the runtime will need it for the injected StepCollector calls
            ttdAssemblyRef = new AssemblyNameReference("TestTrackingDiagrams", new Version(0, 0, 0, 0));
            module.AssemblyReferences.Add(ttdAssemblyRef);
        }

        // Build type reference for StepCollector
        var stepCollectorTypeRef = new TypeReference(
            "TestTrackingDiagrams.Tracking", "StepCollector",
            module, ttdAssemblyRef);

        // StartStep(string? keyword, string text, string[]? paramNames, object?[]? paramValues)
        var startStep = new MethodReference("StartStep", module.TypeSystem.Void, stepCollectorTypeRef)
        {
            HasThis = false
        };
        startStep.Parameters.Add(new ParameterDefinition(module.TypeSystem.String));
        startStep.Parameters.Add(new ParameterDefinition(module.TypeSystem.String));
        startStep.Parameters.Add(new ParameterDefinition(new ArrayType(module.TypeSystem.String)));
        startStep.Parameters.Add(new ParameterDefinition(new ArrayType(module.TypeSystem.Object)));

        // CompleteStep(bool passed, string? errorMessage = null)
        var completeStepPassed = new MethodReference("CompleteStep", module.TypeSystem.Void, stepCollectorTypeRef)
        {
            HasThis = false
        };
        completeStepPassed.Parameters.Add(new ParameterDefinition(module.TypeSystem.Boolean));
        completeStepPassed.Parameters.Add(new ParameterDefinition(module.TypeSystem.String));

        // Same method, different name reference (reuse)
        var completeStepFailed = new MethodReference("CompleteStep", module.TypeSystem.Void, stepCollectorTypeRef)
        {
            HasThis = false
        };
        completeStepFailed.Parameters.Add(new ParameterDefinition(module.TypeSystem.Boolean));
        completeStepFailed.Parameters.Add(new ParameterDefinition(module.TypeSystem.String));

        return (startStep, completeStepPassed, completeStepFailed);
    }

    private static bool HasTrackStepsAttribute(AssemblyDefinition assembly)
    {
        return assembly.CustomAttributes.Any(a =>
            a.AttributeType.Name == "TrackStepsAttribute" ||
            a.AttributeType.FullName == "TestTrackingDiagrams.Tracking.TrackStepsAttribute");
    }

    private static bool HasAlreadyBeenWeaved(AssemblyDefinition assembly)
    {
        return assembly.MainModule.CustomAttributes.Any(a =>
            a.AttributeType.Name == "__StepTrackingWeaved__");
    }

    private static void AddWeavedSentinel(AssemblyDefinition assembly)
    {
        var module = assembly.MainModule;
        var attrType = new TypeDefinition(
            "TestTrackingDiagrams.StepTracking.Internal",
            "__StepTrackingWeaved__",
            Mono.Cecil.TypeAttributes.NotPublic | Mono.Cecil.TypeAttributes.Sealed,
            module.TypeSystem.Object);
        module.Types.Add(attrType);

        var ctor = new MethodDefinition(
            ".ctor",
            Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.HideBySig |
            Mono.Cecil.MethodAttributes.SpecialName | Mono.Cecil.MethodAttributes.RTSpecialName,
            module.TypeSystem.Void);
        ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        attrType.Methods.Add(ctor);

        var attrInstance = new CustomAttribute(ctor);
        module.CustomAttributes.Add(attrInstance);
    }

    private IAssemblyResolver CreateResolver(string assemblyPath)
    {
        var resolver = new DefaultAssemblyResolver();

        var assemblyDir = Path.GetDirectoryName(assemblyPath);
        if (!string.IsNullOrEmpty(assemblyDir))
            resolver.AddSearchDirectory(assemblyDir);

        foreach (var dir in _searchDirectories)
        {
            resolver.AddSearchDirectory(dir);
        }

        return resolver;
    }
}
