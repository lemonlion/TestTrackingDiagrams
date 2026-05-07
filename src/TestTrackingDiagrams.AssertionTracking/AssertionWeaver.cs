using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace TestTrackingDiagrams.AssertionTracking;

/// <summary>
/// Core IL weaving logic. Opens a compiled assembly with Cecil, finds FluentAssertions/AwesomeAssertions
/// .Should() call chains, wraps each assertion statement in try/catch that reports
/// pass/fail to Track.AssertionPassed/Track.AssertionFailed.
/// </summary>
public class AssertionWeaver
{
    private readonly TaskLoggingHelper? _log;
    private readonly string[] _searchDirectories;
    private readonly Dictionary<string, string[]?> _sourceFileCache = new(StringComparer.Ordinal);

    public AssertionWeaver(TaskLoggingHelper? log = null, string[]? searchDirectories = null)
    {
        _log = log;
        _searchDirectories = searchDirectories ?? Array.Empty<string>();
    }

    public WeaveResult Weave(string assemblyPath, string pdbPath)
    {
        var result = new WeaveResult();
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Read assembly from a byte array to avoid holding file locks (ReadWrite=true
        // can stall on Linux overlay filesystems used by CI runners).
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

        var readMs = sw.ElapsedMilliseconds;

        // Fast-path: check for [assembly: TrackAssertionsBeta]
        if (!HasTrackAssertionsBetaAttribute(assembly))
        {
            result.SkipReason = "No TrackAssertionsBeta attribute found";
            return result;
        }

        // Fast-path: if neither FluentAssertions nor AwesomeAssertions is referenced, there can't be any .Should() calls
        if (!ReferencesAssertionLibrary(assembly))
        {
            result.SkipReason = "FluentAssertions/AwesomeAssertions not referenced";
            return result;
        }

        // Guard against double-weaving: check for our sentinel module attribute
        if (HasAlreadyBeenWeaved(assembly))
        {
            result.SkipReason = "Assembly already weaved (sentinel found)";
            _log?.LogMessage(MessageImportance.Normal,
                "TestTrackingDiagrams.AssertionTracking: Skipping — assembly was already weaved");
            return result;
        }

        // Find or construct method references for Track.AssertionPassed/AssertionFailed
        var trackMethods = GetTrackMethodReferences(assembly);
        if (trackMethods == null)
        {
            result.SkipReason = "TestTrackingDiagrams core library version too old";
            return result;
        }
        var passedRef = trackMethods.Value.Passed;
        var failedRef = trackMethods.Value.Failed;
        var passedWithValuesRef = trackMethods.Value.PassedWithValues;
        var failedWithValuesRef = trackMethods.Value.FailedWithValues;

        // Also need Exception.get_Message
        var exceptionType = assembly.MainModule.ImportReference(typeof(Exception));
        var getMessageMethod = assembly.MainModule.ImportReference(
            typeof(Exception).GetProperty("Message")!.GetGetMethod()!);

        var setupMs = sw.ElapsedMilliseconds;

        foreach (var type in assembly.MainModule.GetTypes())
        {
            if (HasSuppressAttribute(type))
                continue;

            foreach (var method in type.Methods)
            {
                if (!method.HasBody)
                    continue;
                if (HasSuppressAttribute(method))
                    continue;

                var assertions = FindAssertionStatements(method, result);
                if (assertions.Count == 0)
                    continue;

                WrapAssertions(method, assertions, passedRef, failedRef, passedWithValuesRef, failedWithValuesRef, getMessageMethod, exceptionType);
                result.WeavedCount += assertions.Count;
                result.MethodCount++;
            }
        }

        var weaveMs = sw.ElapsedMilliseconds;

        if (result.WeavedCount > 0)
        {
            // Add sentinel attribute to prevent double-weaving
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

            var writeMs = sw.ElapsedMilliseconds;

            // Write back to disk
            File.WriteAllBytes(assemblyPath, outputAssembly.ToArray());
            File.WriteAllBytes(pdbPath, outputPdb.ToArray());

            _log?.LogMessage(MessageImportance.Low,
                "AssertionTracking timing: read={0}ms setup={1}ms weave={2}ms write={3}ms total={4}ms",
                readMs, setupMs - readMs, weaveMs - setupMs, writeMs - weaveMs, sw.ElapsedMilliseconds);
        }

        return result;
    }

    private static bool HasAlreadyBeenWeaved(AssemblyDefinition assembly)
    {
        return assembly.MainModule.CustomAttributes.Any(a =>
            a.AttributeType.Name == "__AssertionTrackingWeaved__");
    }

    private static void AddWeavedSentinel(AssemblyDefinition assembly)
    {
        var module = assembly.MainModule;
        // Create a minimal attribute type in the module itself
        var attrType = new TypeDefinition(
            "TestTrackingDiagrams.AssertionTracking.Internal",
            "__AssertionTrackingWeaved__",
            Mono.Cecil.TypeAttributes.NotPublic | Mono.Cecil.TypeAttributes.Sealed,
            module.TypeSystem.Object);
        module.Types.Add(attrType);

        // Add a parameterless constructor
        var ctor = new MethodDefinition(
            ".ctor",
            Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.HideBySig |
            Mono.Cecil.MethodAttributes.SpecialName | Mono.Cecil.MethodAttributes.RTSpecialName,
            module.TypeSystem.Void);
        ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        attrType.Methods.Add(ctor);

        // Apply [module: __AssertionTrackingWeaved__]
        var attrInstance = new CustomAttribute(ctor);
        module.CustomAttributes.Add(attrInstance);
    }

    private IAssemblyResolver CreateResolver(string assemblyPath)
    {
        var resolver = new DefaultAssemblyResolver();

        // Add the directory containing the assembly itself
        var assemblyDir = Path.GetDirectoryName(assemblyPath);
        if (!string.IsNullOrEmpty(assemblyDir))
            resolver.AddSearchDirectory(assemblyDir);

        // Add all directories from resolved references (NuGet packages, framework, etc.)
        foreach (var dir in _searchDirectories)
        {
            resolver.AddSearchDirectory(dir);
        }

        return resolver;
    }

    private static bool HasTrackAssertionsBetaAttribute(AssemblyDefinition assembly)
    {
        return assembly.CustomAttributes.Any(a =>
            a.AttributeType.Name == "TrackAssertionsBetaAttribute" ||
            a.AttributeType.FullName == "TestTrackingDiagrams.Tracking.TrackAssertionsBetaAttribute");
    }

    private static bool ReferencesAssertionLibrary(AssemblyDefinition assembly)
    {
        return assembly.MainModule.AssemblyReferences
            .Any(r => r.Name == "FluentAssertions" || r.Name == "AwesomeAssertions");
    }

    private static bool HasSuppressAttribute(ICustomAttributeProvider provider)
    {
        if (!provider.HasCustomAttributes)
            return false;
        return provider.CustomAttributes.Any(a =>
            a.AttributeType.Name == "SuppressAssertionTrackingAttribute");
    }

    private (MethodReference Passed, MethodReference Failed, MethodReference PassedWithValues, MethodReference FailedWithValues)? GetTrackMethodReferences(AssemblyDefinition assembly)
    {
        var module = assembly.MainModule;

        // Find the TestTrackingDiagrams assembly reference
        var ttdAssemblyRef = module.AssemblyReferences
            .FirstOrDefault(r => r.Name == "TestTrackingDiagrams");

        if (ttdAssemblyRef == null)
        {
            // The compiler may have trimmed the reference if no types are used directly.
            // Add it — the runtime will need it for the injected Track.AssertionPassed/Failed calls.
            ttdAssemblyRef = new AssemblyNameReference("TestTrackingDiagrams", new Version(0, 0, 0, 0));
            module.AssemblyReferences.Add(ttdAssemblyRef);
            _log?.LogMessage(MessageImportance.Low,
                "AssertionTracking: Added TestTrackingDiagrams assembly reference");
        }
        else if (ttdAssemblyRef.Version != new Version(0, 0, 0, 0) &&
                 ttdAssemblyRef.Version < new Version(2, 30, 7, 0))
        {
            // Track.AssertionPassed/Failed were introduced in v2.30.7.
            // If the referenced core library is older, the weaved code will fail at runtime.
            _log?.LogError(
                "TestTrackingDiagrams.AssertionTracking requires TestTrackingDiagrams >= 2.30.7, " +
                $"but the project references version {ttdAssemblyRef.Version}. " +
                "Please update all TestTrackingDiagrams packages to the same version.");
            return null;
        }

        // Construct the Track type reference
        var trackTypeRef = new TypeReference(
            "TestTrackingDiagrams.Tracking", "Track",
            module, ttdAssemblyRef);

        var stringType = module.TypeSystem.String;
        var int32Type = module.TypeSystem.Int32;
        var voidType = module.TypeSystem.Void;

        // AssertionPassed(string expression, string? callerFilePath, int callerLineNumber)
        var passedMethod = new MethodReference("AssertionPassed", voidType, trackTypeRef)
        {
            HasThis = false,
        };
        passedMethod.Parameters.Add(new ParameterDefinition("expression", ParameterAttributes.None, stringType));
        passedMethod.Parameters.Add(new ParameterDefinition("callerFilePath", ParameterAttributes.None, stringType));
        passedMethod.Parameters.Add(new ParameterDefinition("callerLineNumber", ParameterAttributes.None, int32Type));

        // AssertionFailed(string expression, string failureMessage, string? callerFilePath, int callerLineNumber)
        var failedMethod = new MethodReference("AssertionFailed", voidType, trackTypeRef)
        {
            HasThis = false,
        };
        failedMethod.Parameters.Add(new ParameterDefinition("expression", ParameterAttributes.None, stringType));
        failedMethod.Parameters.Add(new ParameterDefinition("failureMessage", ParameterAttributes.None, stringType));
        failedMethod.Parameters.Add(new ParameterDefinition("callerFilePath", ParameterAttributes.None, stringType));
        failedMethod.Parameters.Add(new ParameterDefinition("callerLineNumber", ParameterAttributes.None, int32Type));

        // AssertionPassedWithValues(string expression, string[] varNames, object?[] varValues, string? callerFilePath, int callerLineNumber)
        var stringArrayType = new ArrayType(stringType);
        var objectType = module.TypeSystem.Object;
        var objectArrayType = new ArrayType(objectType);

        var passedWithValuesMethod = new MethodReference("AssertionPassedWithValues", voidType, trackTypeRef)
        {
            HasThis = false,
        };
        passedWithValuesMethod.Parameters.Add(new ParameterDefinition("expression", ParameterAttributes.None, stringType));
        passedWithValuesMethod.Parameters.Add(new ParameterDefinition("varNames", ParameterAttributes.None, stringArrayType));
        passedWithValuesMethod.Parameters.Add(new ParameterDefinition("varValues", ParameterAttributes.None, objectArrayType));
        passedWithValuesMethod.Parameters.Add(new ParameterDefinition("callerFilePath", ParameterAttributes.None, stringType));
        passedWithValuesMethod.Parameters.Add(new ParameterDefinition("callerLineNumber", ParameterAttributes.None, int32Type));

        // AssertionFailedWithValues(string expression, string failureMessage, string[] varNames, object?[] varValues, string? callerFilePath, int callerLineNumber)
        var failedWithValuesMethod = new MethodReference("AssertionFailedWithValues", voidType, trackTypeRef)
        {
            HasThis = false,
        };
        failedWithValuesMethod.Parameters.Add(new ParameterDefinition("expression", ParameterAttributes.None, stringType));
        failedWithValuesMethod.Parameters.Add(new ParameterDefinition("failureMessage", ParameterAttributes.None, stringType));
        failedWithValuesMethod.Parameters.Add(new ParameterDefinition("varNames", ParameterAttributes.None, stringArrayType));
        failedWithValuesMethod.Parameters.Add(new ParameterDefinition("varValues", ParameterAttributes.None, objectArrayType));
        failedWithValuesMethod.Parameters.Add(new ParameterDefinition("callerFilePath", ParameterAttributes.None, stringType));
        failedWithValuesMethod.Parameters.Add(new ParameterDefinition("callerLineNumber", ParameterAttributes.None, int32Type));

        return (passedMethod, failedMethod, passedWithValuesMethod, failedWithValuesMethod);
    }

    /// <summary>
    /// Finds assertion "statements" in a method body. An assertion statement is a contiguous
    /// set of IL instructions (identified by sequence points) that contains a call to a method
    /// named "Should" on a FluentAssertions or AwesomeAssertions type.
    /// </summary>
    private List<AssertionStatement> FindAssertionStatements(MethodDefinition method, WeaveResult result)
    {
        var results = new List<AssertionStatement>();
        if (!method.DebugInformation.HasSequencePoints)
        {
            result.DiagMessages.Add($"Method {method.Name}: no sequence points");
            return results;
        }

        // Fast-path: scan all instructions once for ANY .Should() call.
        // This avoids the expensive per-sequence-point analysis for the vast majority
        // of methods (async state machines, closures, framework code) that have no assertions.
        var hasAnyShouldCall = false;
        foreach (var instr in method.Body.Instructions)
        {
            if ((instr.OpCode == OpCodes.Call || instr.OpCode == OpCodes.Callvirt) &&
                instr.Operand is MethodReference mr &&
                mr.Name == "Should" &&
                IsFluentAssertionsType(mr.DeclaringType))
            {
                hasAnyShouldCall = true;
                break;
            }
        }

        if (!hasAnyShouldCall)
            return results;

        var sequencePoints = method.DebugInformation.SequencePoints.ToList();
        var instructions = method.Body.Instructions;

        // Build instruction list indexed by offset for O(1) range lookups.
        // Walk the linked list once and bucket instructions by sequence point ranges.
        var instrByOffset = new List<Instruction>(instructions.Count);
        foreach (var instr in instructions)
            instrByOffset.Add(instr);

        // Pre-compute instruction index boundaries for each sequence point
        // using a single pass through the instruction list.
        var spBoundaries = new int[sequencePoints.Count + 1];
        {
            var spIdx2 = 0;
            for (var i = 0; i < instrByOffset.Count && spIdx2 < sequencePoints.Count; i++)
            {
                while (spIdx2 < sequencePoints.Count && instrByOffset[i].Offset >= sequencePoints[spIdx2].Offset)
                {
                    spBoundaries[spIdx2] = i;
                    spIdx2++;
                }
            }
            while (spIdx2 <= sequencePoints.Count)
            {
                spBoundaries[spIdx2] = instrByOffset.Count;
                spIdx2++;
            }
        }

        // Group instructions by sequence point (each sequence point = one source statement)
        for (var spIdx = 0; spIdx < sequencePoints.Count; spIdx++)
        {
            var sp = sequencePoints[spIdx];
            if (sp.IsHidden)
                continue;

            // Use pre-computed boundaries for O(1) range access
            var startIdx = spBoundaries[spIdx];
            var endIdx = spIdx + 1 < sequencePoints.Count
                ? spBoundaries[spIdx + 1]
                : instrByOffset.Count;

            // Get instructions in this statement range
            var statementInstructions = new List<Instruction>(endIdx - startIdx);
            for (var i = startIdx; i < endIdx; i++)
                statementInstructions.Add(instrByOffset[i]);

            if (statementInstructions.Count == 0)
                continue;

            // Check if any instruction is a call to .Should()
            var hasShouldCall = statementInstructions.Any(i =>
                (i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt) &&
                i.Operand is MethodReference mr &&
                mr.Name == "Should" &&
                IsFluentAssertionsType(mr.DeclaringType));

            if (!hasShouldCall)
                continue;

            // Collect conditional/unconditional branches jumping outside the statement range.
            // These come from null-propagation (?.) which generates brfalse/brtrue that skip
            // past the expression. We exclude leave/leave.s since those are structural control
            // flow for exception handling (e.g. async state machine's outer try/catch exit).
            var startOffset = statementInstructions[0].Offset;
            var endOffset = statementInstructions.Count > 0
                ? statementInstructions[statementInstructions.Count - 1].Offset + 1
                : startOffset;
            var outboundBranches = statementInstructions
                .Where(i => i.OpCode != OpCodes.Leave && i.OpCode != OpCodes.Leave_S &&
                    i.Operand is Instruction target &&
                    (target.Offset < startOffset || target.Offset >= endOffset))
                .ToList();

            // Read the source text for this statement
            var sourceText = ReadSourceText(sp);

            // Exclude trailing leave/leave.s and ret from the statement. In async state machines,
            // the compiler places a leave at the end of user code to exit the outer try.
            // A ret instruction cannot be inside a try block (CLR verifier rejects it).
            // These instructions should remain after our wrapper.
            var lastInstr = statementInstructions.Last();
            while (lastInstr != statementInstructions.First() &&
                   (lastInstr.OpCode == OpCodes.Leave || lastInstr.OpCode == OpCodes.Leave_S ||
                    lastInstr.OpCode == OpCodes.Ret))
            {
                lastInstr = lastInstr.Previous;
            }

            results.Add(new AssertionStatement
            {
                FirstInstruction = statementInstructions.First(),
                LastInstruction = lastInstr,
                SequencePoint = sp,
                SourceText = sourceText,
                OutboundBranches = outboundBranches,
                CapturedVariables = DetectCapturedVariables(method, statementInstructions, sourceText)
            });
        }

        return results;
    }

    private static bool IsFluentAssertionsType(TypeReference type)
    {
        // FluentAssertions and AwesomeAssertions both use these namespaces
        var ns = type.Namespace;
        return ns.StartsWith("FluentAssertions", StringComparison.Ordinal) ||
               ns.StartsWith("AwesomeAssertions", StringComparison.Ordinal);
    }

    private string ReadSourceText(SequencePoint sp)
    {
        try
        {
            var documentUrl = sp.Document.Url;

            if (!_sourceFileCache.TryGetValue(documentUrl, out var lines))
            {
                lines = File.Exists(documentUrl) ? File.ReadAllLines(documentUrl) : null;
                _sourceFileCache[documentUrl] = lines;
            }

            if (lines == null)
                return $"assertion at line {sp.StartLine}";

            if (sp.StartLine < 1 || sp.StartLine > lines.Length)
                return $"assertion at line {sp.StartLine}";

            // Single-line statement
            if (sp.StartLine == sp.EndLine)
            {
                var line = lines[sp.StartLine - 1];
                return line.Trim().TrimEnd(';');
            }

            // Multi-line: concatenate lines
            var parts = new List<string>();
            for (var i = sp.StartLine; i <= Math.Min(sp.EndLine, lines.Length); i++)
            {
                parts.Add(lines[i - 1].Trim());
            }
            return string.Join(" ", parts).TrimEnd(';');
        }
        catch
        {
            return $"assertion at line {sp.StartLine}";
        }
    }

    /// <summary>
    /// Detects variables loaded as arguments AFTER the .Should() call in a statement.
    /// These are arguments to assertion methods (e.g. .Be(expected), .BeInRange(min, max)).
    /// </summary>
    private static List<CapturedVariable> DetectCapturedVariables(
        MethodDefinition method,
        List<Instruction> statementInstructions,
        string sourceText)
    {
        var captured = new List<CapturedVariable>();
        var seenNames = new HashSet<string>(StringComparer.Ordinal);

        // Find the Should call index
        var shouldIdx = -1;
        for (var i = 0; i < statementInstructions.Count; i++)
        {
            var instr = statementInstructions[i];
            if ((instr.OpCode == OpCodes.Call || instr.OpCode == OpCodes.Callvirt) &&
                instr.Operand is MethodReference mr &&
                mr.Name == "Should" &&
                IsFluentAssertionsType(mr.DeclaringType))
            {
                shouldIdx = i;
                break;
            }
        }

        if (shouldIdx < 0)
            return captured;

        // Build local variable name map from debug info
        var localNames = GetLocalVariableNames(method);

        // Scan instructions after Should
        for (var i = shouldIdx + 1; i < statementInstructions.Count; i++)
        {
            var instr = statementInstructions[i];

            // ldloc / ldloc.s / ldloc.0-3 — regular local variable or display class
            if (IsLdloc(instr, out var localIndex))
            {
                var varType = method.Body.Variables[localIndex].VariableType;

                // Check if this is a display class (closure) load
                if (IsDisplayClassType(varType))
                {
                    // Resolve the display class type to enumerate its fields
                    var displayClassType = ResolveDisplayClassType(varType, method);
                    if (displayClassType != null)
                    {
                        foreach (var field in displayClassType.Fields)
                        {
                            var fieldName = field.Name;
                            if (!NameAppearsInExpression(fieldName, sourceText))
                                continue;
                            if (!seenNames.Add(fieldName))
                                continue;

                            var fieldRef = new FieldReference(field.Name, field.FieldType, varType);
                            captured.Add(new CapturedVariable
                            {
                                Name = fieldName,
                                ClosureLocalIndex = localIndex,
                                ClosureField = fieldRef,
                                NeedsBoxing = field.FieldType.IsValueType,
                                Type = field.FieldType
                            });
                        }
                    }
                    continue;
                }

                if (!localNames.TryGetValue(localIndex, out var name))
                    continue;
                if (!NameAppearsInExpression(name, sourceText))
                    continue;
                if (!seenNames.Add(name))
                    continue;

                captured.Add(new CapturedVariable
                {
                    Name = name,
                    LocalIndex = localIndex,
                    NeedsBoxing = varType.IsValueType,
                    Type = varType
                });
            }
            // ldarg.0 + ldfld — async state machine field access
            else if (instr.OpCode == OpCodes.Ldarg_0 &&
                     i + 1 < statementInstructions.Count &&
                     statementInstructions[i + 1].OpCode == OpCodes.Ldfld &&
                     statementInstructions[i + 1].Operand is FieldReference fieldRef)
            {
                // Check if the field type is a display class (closure inside async method)
                if (IsDisplayClassType(fieldRef.FieldType))
                {
                    var displayClassType = ResolveDisplayClassType(fieldRef.FieldType, method);
                    if (displayClassType != null)
                    {
                        foreach (var field in displayClassType.Fields)
                        {
                            var fieldName = field.Name;
                            if (!NameAppearsInExpression(fieldName, sourceText))
                                continue;
                            if (!seenNames.Add(fieldName))
                                continue;

                            // For async+closure: load this->displayClassField->valueField
                            captured.Add(new CapturedVariable
                            {
                                Name = fieldName,
                                StateField = fieldRef, // the display class field on state machine
                                ClosureLocalIndex = -1,
                                ClosureField = new FieldReference(field.Name, field.FieldType, fieldRef.FieldType),
                                NeedsBoxing = field.FieldType.IsValueType,
                                Type = field.FieldType
                            });
                        }
                    }
                    i++; // skip the ldfld instruction
                    continue;
                }

                var name = GetStateFieldOriginalName(fieldRef);
                if (name == null)
                    continue;
                if (!NameAppearsInExpression(name, sourceText))
                    continue;
                if (!seenNames.Add(name))
                    continue;

                captured.Add(new CapturedVariable
                {
                    Name = name,
                    StateField = fieldRef,
                    NeedsBoxing = fieldRef.FieldType.IsValueType,
                    Type = fieldRef.FieldType
                });
                i++; // skip the ldfld instruction
            }
        }

        return captured;
    }

    private static bool IsLdloc(Instruction instr, out int index)
    {
        if (instr.OpCode == OpCodes.Ldloc_0) { index = 0; return true; }
        if (instr.OpCode == OpCodes.Ldloc_1) { index = 1; return true; }
        if (instr.OpCode == OpCodes.Ldloc_2) { index = 2; return true; }
        if (instr.OpCode == OpCodes.Ldloc_3) { index = 3; return true; }
        if (instr.OpCode == OpCodes.Ldloc || instr.OpCode == OpCodes.Ldloc_S)
        {
            if (instr.Operand is VariableDefinition varDef)
            {
                index = varDef.Index;
                return true;
            }
            if (instr.Operand is int idx)
            {
                index = idx;
                return true;
            }
        }
        index = -1;
        return false;
    }

    private static Dictionary<int, string> GetLocalVariableNames(MethodDefinition method)
    {
        var names = new Dictionary<int, string>();
        if (!method.DebugInformation.HasSequencePoints)
            return names;

        var scope = method.DebugInformation.Scope;
        if (scope == null)
            return names;

        CollectVariableNames(scope, names);
        return names;
    }

    private static void CollectVariableNames(ScopeDebugInformation scope, Dictionary<int, string> names)
    {
        if (scope.HasVariables)
        {
            foreach (var v in scope.Variables)
            {
                if (!v.IsDebuggerHidden && !names.ContainsKey(v.Index))
                    names[v.Index] = v.Name;
            }
        }

        if (scope.HasScopes)
        {
            foreach (var child in scope.Scopes)
                CollectVariableNames(child, names);
        }
    }

    /// <summary>
    /// Extracts the original variable name from a state machine field.
    /// Pattern: &lt;name&gt;5__N → name
    /// </summary>
    private static string? GetStateFieldOriginalName(FieldReference field)
    {
        var fieldName = field.Name;
        if (fieldName.StartsWith("<") && fieldName.Contains(">"))
        {
            var end = fieldName.IndexOf('>');
            return fieldName.Substring(1, end - 1);
        }
        return null;
    }

    /// <summary>
    /// Checks whether a variable name appears in the expression as a whole word
    /// (not as a substring of a larger identifier).
    /// </summary>
    private static bool NameAppearsInExpression(string name, string expression)
    {
        var idx = 0;
        while (true)
        {
            idx = expression.IndexOf(name, idx, StringComparison.Ordinal);
            if (idx < 0) return false;

            var before = idx > 0 ? expression[idx - 1] : ' ';
            var after = idx + name.Length < expression.Length ? expression[idx + name.Length] : ' ';

            if (!IsIdentifierChar(before) && !IsIdentifierChar(after))
                return true;

            idx += name.Length;
        }
    }

    private static bool IsIdentifierChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    /// <summary>
    /// Checks whether a type is a compiler-generated display class (closure container).
    /// Display classes are named like &lt;&gt;c__DisplayClass0_0.
    /// </summary>
    private static bool IsDisplayClassType(TypeReference type)
    {
        var name = type.Name;
        return name.Contains("<>c__DisplayClass") || name.Contains("<>c__");
    }

    /// <summary>
    /// Resolves a display class TypeReference to its TypeDefinition to enumerate fields.
    /// </summary>
    private static TypeDefinition? ResolveDisplayClassType(TypeReference typeRef, MethodDefinition method)
    {
        // Try to resolve directly
        try
        {
            var resolved = typeRef.Resolve();
            if (resolved != null)
                return resolved;
        }
        catch { /* fall through to search */ }

        // Search in the declaring type's nested types
        var declaringType = method.DeclaringType;
        foreach (var nested in declaringType.NestedTypes)
        {
            if (nested.Name == typeRef.Name || nested.FullName == typeRef.FullName)
                return nested;
        }

        return null;
    }

    /// <summary>
    /// Wraps each assertion statement in: try { [original] ; AssertionPassed(...) } catch(Exception ex) { AssertionFailed(..., ex.Message); throw; }
    /// </summary>
    private void WrapAssertions(
        MethodDefinition method,
        List<AssertionStatement> assertions,
        MethodReference passedRef,
        MethodReference failedRef,
        MethodReference passedWithValuesRef,
        MethodReference failedWithValuesRef,
        MethodReference getMessageRef,
        TypeReference exceptionTypeRef)
    {
        var il = method.Body.GetILProcessor();
        method.Body.SimplifyMacros();

        // Process in reverse order to avoid offset shifts affecting earlier statements
        for (var i = assertions.Count - 1; i >= 0; i--)
        {
            var assertion = assertions[i];
            WrapSingleAssertion(method, il, assertion, passedRef, failedRef,
                passedWithValuesRef, failedWithValuesRef, getMessageRef, exceptionTypeRef);
        }

        method.Body.OptimizeMacros();
    }

    private void WrapSingleAssertion(
        MethodDefinition method,
        ILProcessor il,
        AssertionStatement assertion,
        MethodReference passedRef,
        MethodReference failedRef,
        MethodReference passedWithValuesRef,
        MethodReference failedWithValuesRef,
        MethodReference getMessageRef,
        TypeReference exceptionTypeRef)
    {
        var body = method.Body;
        var module = method.Module;
        var hasValues = assertion.CapturedVariables.Count > 0;

        // Expression string and file/line for tracking
        var expressionText = assertion.SourceText;
        var filePath = assertion.SequencePoint.Document.Url;
        var lineNumber = assertion.SequencePoint.StartLine;

        // Get the file name only (for shorter display)
        var separatorIdx = filePath.LastIndexOfAny(new[] { '/', '\\' });
        var fileName = separatorIdx >= 0 ? filePath.Substring(separatorIdx + 1) : filePath;

        var firstInstr = assertion.FirstInstruction;
        var lastInstr = assertion.LastInstruction;

        // Find the instruction AFTER the last assertion instruction
        var afterLastInstr = lastInstr.Next;

        // Create try-start nop (we'll insert before the first instruction)
        var tryStart = il.Create(OpCodes.Nop);
        il.InsertBefore(firstInstr, tryStart);

        // If we have captured variables, build arrays at try-start so both paths can use them
        VariableDefinition? namesLocal = null;
        VariableDefinition? valuesLocal = null;

        if (hasValues)
        {
            var stringArrayType = new ArrayType(module.TypeSystem.String);
            var objectArrayType = new ArrayType(module.TypeSystem.Object);
            namesLocal = new VariableDefinition(stringArrayType);
            valuesLocal = new VariableDefinition(objectArrayType);
            body.Variables.Add(namesLocal);
            body.Variables.Add(valuesLocal);

            // Build names array: new string[N] { "var1", "var2", ... }
            var count = assertion.CapturedVariables.Count;
            il.InsertBefore(firstInstr, il.Create(OpCodes.Ldc_I4, count));
            il.InsertBefore(firstInstr, il.Create(OpCodes.Newarr, module.TypeSystem.String));
            for (var vi = 0; vi < count; vi++)
            {
                il.InsertBefore(firstInstr, il.Create(OpCodes.Dup));
                il.InsertBefore(firstInstr, il.Create(OpCodes.Ldc_I4, vi));
                il.InsertBefore(firstInstr, il.Create(OpCodes.Ldstr, assertion.CapturedVariables[vi].Name));
                il.InsertBefore(firstInstr, il.Create(OpCodes.Stelem_Ref));
            }
            il.InsertBefore(firstInstr, il.Create(OpCodes.Stloc, namesLocal));

            // Build values array: new object[N] { var1, var2, ... } (with boxing if needed)
            il.InsertBefore(firstInstr, il.Create(OpCodes.Ldc_I4, count));
            il.InsertBefore(firstInstr, il.Create(OpCodes.Newarr, module.TypeSystem.Object));
            for (var vi = 0; vi < count; vi++)
            {
                var cv = assertion.CapturedVariables[vi];
                il.InsertBefore(firstInstr, il.Create(OpCodes.Dup));
                il.InsertBefore(firstInstr, il.Create(OpCodes.Ldc_I4, vi));

                // Load the variable
                if (cv.ClosureField != null && cv.ClosureLocalIndex >= 0)
                {
                    // Closure in non-async: ldloc displayClass; ldfld field
                    il.InsertBefore(firstInstr, il.Create(OpCodes.Ldloc, body.Variables[cv.ClosureLocalIndex]));
                    il.InsertBefore(firstInstr, il.Create(OpCodes.Ldfld, cv.ClosureField));
                }
                else if (cv.ClosureField != null && cv.StateField != null)
                {
                    // Closure in async: ldarg.0; ldfld stateField (display class); ldfld closureField
                    il.InsertBefore(firstInstr, il.Create(OpCodes.Ldarg_0));
                    il.InsertBefore(firstInstr, il.Create(OpCodes.Ldfld, cv.StateField));
                    il.InsertBefore(firstInstr, il.Create(OpCodes.Ldfld, cv.ClosureField));
                }
                else if (cv.StateField != null)
                {
                    il.InsertBefore(firstInstr, il.Create(OpCodes.Ldarg_0));
                    il.InsertBefore(firstInstr, il.Create(OpCodes.Ldfld, cv.StateField));
                }
                else
                {
                    il.InsertBefore(firstInstr, il.Create(OpCodes.Ldloc, body.Variables[cv.LocalIndex]));
                }

                // Box value types
                if (cv.NeedsBoxing)
                    il.InsertBefore(firstInstr, il.Create(OpCodes.Box, cv.Type));

                il.InsertBefore(firstInstr, il.Create(OpCodes.Stelem_Ref));
            }
            il.InsertBefore(firstInstr, il.Create(OpCodes.Stloc, valuesLocal));
        }

        // Create the "after assertion success" block
        if (afterLastInstr != null)
        {
            if (hasValues)
            {
                il.InsertBefore(afterLastInstr, il.Create(OpCodes.Ldstr, expressionText));
                il.InsertBefore(afterLastInstr, il.Create(OpCodes.Ldloc, namesLocal!));
                il.InsertBefore(afterLastInstr, il.Create(OpCodes.Ldloc, valuesLocal!));
                il.InsertBefore(afterLastInstr, il.Create(OpCodes.Ldstr, filePath));
                il.InsertBefore(afterLastInstr, il.Create(OpCodes.Ldc_I4, lineNumber));
                il.InsertBefore(afterLastInstr, il.Create(OpCodes.Call, passedWithValuesRef));
            }
            else
            {
                il.InsertBefore(afterLastInstr, il.Create(OpCodes.Ldstr, expressionText));
                il.InsertBefore(afterLastInstr, il.Create(OpCodes.Ldstr, filePath));
                il.InsertBefore(afterLastInstr, il.Create(OpCodes.Ldc_I4, lineNumber));
                il.InsertBefore(afterLastInstr, il.Create(OpCodes.Call, passedRef));
            }
        }
        else
        {
            if (hasValues)
            {
                il.Append(il.Create(OpCodes.Ldstr, expressionText));
                il.Append(il.Create(OpCodes.Ldloc, namesLocal!));
                il.Append(il.Create(OpCodes.Ldloc, valuesLocal!));
                il.Append(il.Create(OpCodes.Ldstr, filePath));
                il.Append(il.Create(OpCodes.Ldc_I4, lineNumber));
                il.Append(il.Create(OpCodes.Call, passedWithValuesRef));
            }
            else
            {
                il.Append(il.Create(OpCodes.Ldstr, expressionText));
                il.Append(il.Create(OpCodes.Ldstr, filePath));
                il.Append(il.Create(OpCodes.Ldc_I4, lineNumber));
                il.Append(il.Create(OpCodes.Call, passedRef));
            }
        }

        // leave to after the catch
        var afterCatch = il.Create(OpCodes.Nop);
        var leaveInstr = il.Create(OpCodes.Leave, afterCatch);

        if (afterLastInstr != null)
            il.InsertBefore(afterLastInstr, leaveInstr);
        else
            il.Append(leaveInstr);

        // Catch handler: store exception, call AssertionFailed/FailedWithValues, rethrow
        var exVar = new VariableDefinition(exceptionTypeRef);
        body.Variables.Add(exVar);

        var catchStart = il.Create(OpCodes.Stloc, exVar);

        if (afterLastInstr != null)
        {
            il.InsertBefore(afterLastInstr, catchStart);
            il.InsertBefore(afterLastInstr, il.Create(OpCodes.Ldstr, expressionText));
            il.InsertBefore(afterLastInstr, il.Create(OpCodes.Ldloc, exVar));
            il.InsertBefore(afterLastInstr, il.Create(OpCodes.Callvirt, getMessageRef));
            if (hasValues)
            {
                il.InsertBefore(afterLastInstr, il.Create(OpCodes.Ldloc, namesLocal!));
                il.InsertBefore(afterLastInstr, il.Create(OpCodes.Ldloc, valuesLocal!));
                il.InsertBefore(afterLastInstr, il.Create(OpCodes.Ldstr, filePath));
                il.InsertBefore(afterLastInstr, il.Create(OpCodes.Ldc_I4, lineNumber));
                il.InsertBefore(afterLastInstr, il.Create(OpCodes.Call, failedWithValuesRef));
            }
            else
            {
                il.InsertBefore(afterLastInstr, il.Create(OpCodes.Ldstr, filePath));
                il.InsertBefore(afterLastInstr, il.Create(OpCodes.Ldc_I4, lineNumber));
                il.InsertBefore(afterLastInstr, il.Create(OpCodes.Call, failedRef));
            }
            il.InsertBefore(afterLastInstr, il.Create(OpCodes.Rethrow));
            il.InsertBefore(afterLastInstr, afterCatch);
        }
        else
        {
            il.Append(catchStart);
            il.Append(il.Create(OpCodes.Ldstr, expressionText));
            il.Append(il.Create(OpCodes.Ldloc, exVar));
            il.Append(il.Create(OpCodes.Callvirt, getMessageRef));
            if (hasValues)
            {
                il.Append(il.Create(OpCodes.Ldloc, namesLocal!));
                il.Append(il.Create(OpCodes.Ldloc, valuesLocal!));
                il.Append(il.Create(OpCodes.Ldstr, filePath));
                il.Append(il.Create(OpCodes.Ldc_I4, lineNumber));
                il.Append(il.Create(OpCodes.Call, failedWithValuesRef));
            }
            else
            {
                il.Append(il.Create(OpCodes.Ldstr, filePath));
                il.Append(il.Create(OpCodes.Ldc_I4, lineNumber));
                il.Append(il.Create(OpCodes.Call, failedRef));
            }
            il.Append(il.Create(OpCodes.Rethrow));
            il.Append(afterCatch);
        }

        // Add exception handler (innermost first for CLR nesting rules)
        var handler = new ExceptionHandler(ExceptionHandlerType.Catch)
        {
            TryStart = tryStart,
            TryEnd = catchStart,
            HandlerStart = catchStart,
            HandlerEnd = afterCatch,
            CatchType = exceptionTypeRef
        };
        body.ExceptionHandlers.Insert(0, handler);

        // Retarget any outbound branches (from null-propagation ?.) to the leave instruction.
        foreach (var branch in assertion.OutboundBranches)
        {
            branch.Operand = leaveInstr;
        }
    }
}

public class AssertionStatement
{
    public Instruction FirstInstruction { get; set; } = null!;
    public Instruction LastInstruction { get; set; } = null!;
    public SequencePoint SequencePoint { get; set; } = null!;
    public string SourceText { get; set; } = "";
    /// <summary>
    /// Branch instructions within this statement that jump outside the statement range
    /// (e.g. null-propagation ?. short-circuit). These must be retargeted to land inside
    /// the try block to avoid InvalidProgramException.
    /// </summary>
    public List<Instruction> OutboundBranches { get; set; } = new List<Instruction>();
    /// <summary>
    /// Variables used as arguments after the .Should() call that can be captured
    /// at runtime for value resolution in the assertion diagram note.
    /// </summary>
    public List<CapturedVariable> CapturedVariables { get; set; } = new List<CapturedVariable>();
}

public class CapturedVariable
{
    public string Name { get; set; } = "";
    /// <summary>Local variable index for ldloc-based access. -1 if using a state machine field or closure.</summary>
    public int LocalIndex { get; set; } = -1;
    /// <summary>State machine field reference (async methods). Null for regular locals and closures.</summary>
    public FieldReference? StateField { get; set; }
    /// <summary>Local variable index of the display class instance for closure captures. -1 if not a closure.</summary>
    public int ClosureLocalIndex { get; set; } = -1;
    /// <summary>Display class field reference for closure captures. Null if not a closure.</summary>
    public FieldReference? ClosureField { get; set; }
    /// <summary>Whether the variable needs boxing (value type) when stored in object[].</summary>
    public bool NeedsBoxing { get; set; }
    /// <summary>The type of the variable (for boxing emission).</summary>
    public TypeReference Type { get; set; } = null!;
}

public class WeaveResult
{
    public int WeavedCount { get; set; }
    public int MethodCount { get; set; }
    public string? SkipReason { get; set; }
    public List<string> DiagMessages { get; set; } = new List<string>();
}
