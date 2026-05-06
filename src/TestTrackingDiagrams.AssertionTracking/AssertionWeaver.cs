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
/// Core IL weaving logic. Opens a compiled assembly with Cecil, finds FluentAssertions
/// .Should() call chains, wraps each assertion statement in try/catch that reports
/// pass/fail to Track.AssertionPassed/Track.AssertionFailed.
/// </summary>
public class AssertionWeaver
{
    private readonly TaskLoggingHelper? _log;

    public AssertionWeaver(TaskLoggingHelper? log = null)
    {
        _log = log;
    }

    public WeaveResult Weave(string assemblyPath, string pdbPath)
    {
        var result = new WeaveResult();

        var readerParams = new ReaderParameters
        {
            ReadWrite = true,
            ReadSymbols = true,
            SymbolReaderProvider = new DefaultSymbolReaderProvider(throwIfNoSymbol: false)
        };

        using var assembly = AssemblyDefinition.ReadAssembly(assemblyPath, readerParams);

        // Fast-path: check for [assembly: TrackAssertionsBeta]
        if (!HasTrackAssertionsBetaAttribute(assembly))
        {
            result.SkipReason = "No TrackAssertionsBeta attribute found";
            return result;
        }

        // Find or construct method references for Track.AssertionPassed/AssertionFailed
        var trackMethods = GetTrackMethodReferences(assembly);
        var passedRef = trackMethods.Passed;
        var failedRef = trackMethods.Failed;

        // Also need Exception.get_Message
        var exceptionType = assembly.MainModule.ImportReference(typeof(Exception));
        var getMessageMethod = assembly.MainModule.ImportReference(
            typeof(Exception).GetProperty("Message")!.GetGetMethod()!);

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
                // Skip async state machine MoveNext for now (complex)
                if (IsAsyncStateMachine(method))
                    continue;

                var assertions = FindAssertionStatements(method, result);
                if (assertions.Count == 0)
                    continue;

                WrapAssertions(method, assertions, passedRef, failedRef, getMessageMethod, exceptionType);
                result.WeavedCount += assertions.Count;
                result.MethodCount++;
            }
        }

        if (result.WeavedCount > 0)
        {
            var writerParams = new WriterParameters
            {
                WriteSymbols = true,
                SymbolWriterProvider = new DefaultSymbolWriterProvider()
            };
            assembly.Write(writerParams);
        }

        return result;
    }

    private static bool HasTrackAssertionsBetaAttribute(AssemblyDefinition assembly)
    {
        return assembly.CustomAttributes.Any(a =>
            a.AttributeType.Name == "TrackAssertionsBetaAttribute" ||
            a.AttributeType.FullName == "TestTrackingDiagrams.Tracking.TrackAssertionsBetaAttribute");
    }

    private static bool HasSuppressAttribute(ICustomAttributeProvider provider)
    {
        if (!provider.HasCustomAttributes)
            return false;
        return provider.CustomAttributes.Any(a =>
            a.AttributeType.Name == "SuppressAssertionTrackingAttribute");
    }

    private static bool IsAsyncStateMachine(MethodDefinition method)
    {
        // Check if the method's declaring type implements IAsyncStateMachine
        if (method.Name == "MoveNext" && method.DeclaringType.Interfaces.Any(i =>
                i.InterfaceType.FullName == "System.Runtime.CompilerServices.IAsyncStateMachine"))
            return true;
        return false;
    }

    private (MethodReference Passed, MethodReference Failed) GetTrackMethodReferences(AssemblyDefinition assembly)
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

        return (passedMethod, failedMethod);
    }

    /// <summary>
    /// Finds assertion "statements" in a method body. An assertion statement is a contiguous
    /// set of IL instructions (identified by sequence points) that contains a call to a method
    /// named "Should" on a FluentAssertions type.
    /// </summary>
    private List<AssertionStatement> FindAssertionStatements(MethodDefinition method, WeaveResult result)
    {
        var results = new List<AssertionStatement>();
        if (!method.DebugInformation.HasSequencePoints)
        {
            result.DiagMessages.Add($"Method {method.Name}: no sequence points");
            return results;
        }

        var sequencePoints = method.DebugInformation.SequencePoints.ToList();
        var instructions = method.Body.Instructions.ToList();

        // Group instructions by sequence point (each sequence point = one source statement)
        for (var spIdx = 0; spIdx < sequencePoints.Count; spIdx++)
        {
            var sp = sequencePoints[spIdx];
            if (sp.IsHidden)
                continue;

            var startOffset = sp.Offset;
            var endOffset = spIdx + 1 < sequencePoints.Count
                ? sequencePoints[spIdx + 1].Offset
                : int.MaxValue;

            // Get instructions in this statement range
            var statementInstructions = instructions
                .Where(i => i.Offset >= startOffset && i.Offset < endOffset)
                .ToList();

            // Check if any instruction is a call to .Should()
            var hasShouldCall = statementInstructions.Any(i =>
                (i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt) &&
                i.Operand is MethodReference mr &&
                mr.Name == "Should" &&
                IsFluentAssertionsType(mr.DeclaringType));

            if (!hasShouldCall)
                continue;

            // Collect any branches jumping outside the statement range.
            // These come from null-propagation (?.) which generates branches that
            // would cross try/catch boundaries. We'll retarget them during wrapping.
            var outboundBranches = statementInstructions
                .Where(i => i.Operand is Instruction target &&
                    (target.Offset < startOffset || target.Offset >= endOffset))
                .ToList();

            // Read the source text for this statement
            var sourceText = ReadSourceText(sp);

            results.Add(new AssertionStatement
            {
                FirstInstruction = statementInstructions.First(),
                LastInstruction = statementInstructions.Last(),
                SequencePoint = sp,
                SourceText = sourceText,
                OutboundBranches = outboundBranches
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

    private static string ReadSourceText(SequencePoint sp)
    {
        try
        {
            var documentUrl = sp.Document.Url;
            if (!File.Exists(documentUrl))
                return $"assertion at line {sp.StartLine}";

            var lines = File.ReadAllLines(documentUrl);
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
    /// Wraps each assertion statement in: try { [original] ; AssertionPassed(...) } catch(Exception ex) { AssertionFailed(..., ex.Message); throw; }
    /// </summary>
    private void WrapAssertions(
        MethodDefinition method,
        List<AssertionStatement> assertions,
        MethodReference passedRef,
        MethodReference failedRef,
        MethodReference getMessageRef,
        TypeReference exceptionTypeRef)
    {
        var il = method.Body.GetILProcessor();
        method.Body.SimplifyMacros();

        // Process in reverse order to avoid offset shifts affecting earlier statements
        for (var i = assertions.Count - 1; i >= 0; i--)
        {
            var assertion = assertions[i];
            WrapSingleAssertion(method, il, assertion, passedRef, failedRef, getMessageRef, exceptionTypeRef);
        }

        method.Body.OptimizeMacros();
    }

    private void WrapSingleAssertion(
        MethodDefinition method,
        ILProcessor il,
        AssertionStatement assertion,
        MethodReference passedRef,
        MethodReference failedRef,
        MethodReference getMessageRef,
        TypeReference exceptionTypeRef)
    {
        var body = method.Body;

        // Expression string and file/line for tracking
        var expressionText = assertion.SourceText;
        var filePath = assertion.SequencePoint.Document.Url;
        var lineNumber = assertion.SequencePoint.StartLine;

        // Get the file name only (for shorter display)
        var separatorIdx = filePath.LastIndexOfAny(new[] { '/', '\\' });
        var fileName = separatorIdx >= 0 ? filePath.Substring(separatorIdx + 1) : filePath;

        // We need to insert:
        // 1. A nop as try-start before the first instruction
        // 2. After the last instruction of the assertion:
        //    - ldstr expressionText
        //    - ldstr filePath
        //    - ldc.i4 lineNumber
        //    - call Track.AssertionPassed(string, string, int)
        //    - leave afterCatch
        // 3. Catch handler:
        //    - stloc exVar
        //    - ldstr expressionText
        //    - ldloc exVar
        //    - callvirt Exception.get_Message()
        //    - ldstr filePath  
        //    - ldc.i4 lineNumber
        //    - call Track.AssertionFailed(string, string, string, int)
        //    - rethrow
        // 4. afterCatch: nop

        var firstInstr = assertion.FirstInstruction;
        var lastInstr = assertion.LastInstruction;

        // Find the instruction AFTER the last assertion instruction
        var afterLastInstr = lastInstr.Next;

        // Create try-start nop (we'll insert before the first instruction)
        var tryStart = il.Create(OpCodes.Nop);
        il.InsertBefore(firstInstr, tryStart);

        // Create the "after assertion" block: call AssertionPassed
        var ldExprPassed = il.Create(OpCodes.Ldstr, expressionText);
        var ldFilePassed = il.Create(OpCodes.Ldstr, filePath);
        var ldLinePassed = il.Create(OpCodes.Ldc_I4, lineNumber);
        var callPassed = il.Create(OpCodes.Call, passedRef);

        // Insert after last instruction of assertion
        if (afterLastInstr != null)
        {
            il.InsertBefore(afterLastInstr, ldExprPassed);
            il.InsertBefore(afterLastInstr, ldFilePassed);
            il.InsertBefore(afterLastInstr, ldLinePassed);
            il.InsertBefore(afterLastInstr, callPassed);
        }
        else
        {
            il.Append(ldExprPassed);
            il.Append(ldFilePassed);
            il.Append(ldLinePassed);
            il.Append(callPassed);
        }

        // leave to after the catch
        var afterCatch = il.Create(OpCodes.Nop);
        var leaveInstr = il.Create(OpCodes.Leave, afterCatch);

        if (afterLastInstr != null)
            il.InsertBefore(afterLastInstr, leaveInstr);
        else
            il.Append(leaveInstr);

        // Catch handler: store exception, call AssertionFailed, rethrow
        // Add exception local variable
        var exVar = new VariableDefinition(exceptionTypeRef);
        body.Variables.Add(exVar);

        var catchStart = il.Create(OpCodes.Stloc, exVar);
        var ldExprFailed = il.Create(OpCodes.Ldstr, expressionText);
        var ldExVar = il.Create(OpCodes.Ldloc, exVar);
        var callGetMessage = il.Create(OpCodes.Callvirt, getMessageRef);
        var ldFileFailed = il.Create(OpCodes.Ldstr, filePath);
        var ldLineFailed = il.Create(OpCodes.Ldc_I4, lineNumber);
        var callFailed = il.Create(OpCodes.Call, failedRef);
        var rethrow = il.Create(OpCodes.Rethrow);

        if (afterLastInstr != null)
        {
            il.InsertBefore(afterLastInstr, catchStart);
            il.InsertBefore(afterLastInstr, ldExprFailed);
            il.InsertBefore(afterLastInstr, ldExVar);
            il.InsertBefore(afterLastInstr, callGetMessage);
            il.InsertBefore(afterLastInstr, ldFileFailed);
            il.InsertBefore(afterLastInstr, ldLineFailed);
            il.InsertBefore(afterLastInstr, callFailed);
            il.InsertBefore(afterLastInstr, rethrow);
            il.InsertBefore(afterLastInstr, afterCatch);
        }
        else
        {
            il.Append(catchStart);
            il.Append(ldExprFailed);
            il.Append(ldExVar);
            il.Append(callGetMessage);
            il.Append(ldFileFailed);
            il.Append(ldLineFailed);
            il.Append(callFailed);
            il.Append(rethrow);
            il.Append(afterCatch);
        }

        // Add exception handler
        var handler = new ExceptionHandler(ExceptionHandlerType.Catch)
        {
            TryStart = tryStart,
            TryEnd = catchStart,
            HandlerStart = catchStart,
            HandlerEnd = afterCatch,
            CatchType = exceptionTypeRef
        };
        body.ExceptionHandlers.Add(handler);

        // Retarget any outbound branches (from null-propagation ?.) to the leave instruction.
        // This keeps the branch inside the try block: when ?. short-circuits, execution
        // leaves the try cleanly via 'leave' without tracking (correct — no assertion ran).
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
}

public class WeaveResult
{
    public int WeavedCount { get; set; }
    public int MethodCount { get; set; }
    public string? SkipReason { get; set; }
    public List<string> DiagMessages { get; set; } = new List<string>();
}
