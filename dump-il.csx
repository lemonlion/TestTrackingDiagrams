using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

var assemblyPath = args[0];
using var assembly = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters { ReadSymbols = true });
var type = assembly.MainModule.GetTypes().First(t => t.Name == "Tests");
var method = type.Methods.First(m => m.Name == "Method");
var instructions = method.Body.Instructions;
Console.WriteLine($"Method: {method.Name}, Locals: {method.Body.Variables.Count}");
foreach (var v in method.Body.Variables)
    Console.WriteLine($"  Local[{v.Index}]: {v.VariableType.FullName}");
Console.WriteLine("---");
int idx = 0;
foreach (var i in instructions)
{
    Console.WriteLine($"  [{idx:D3}] IL_{i.Offset:X4} {i.OpCode} {i.Operand}");
    idx++;
}
