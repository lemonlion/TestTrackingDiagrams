using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TestTrackingDiagrams.AssertionRewriter;

namespace TestTrackingDiagrams.Tests.AssertionRewriter;

public static class RewriterTestHelper
{
    public static string Rewrite(string source, string? originalFilePath = null)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetRoot();
        var rewriter = new AssertionWrappingRewriter { OriginalFilePath = originalFilePath };
        var newRoot = rewriter.Visit(root);
        return newRoot.ToFullString();
    }

    public static int GetChangeCount(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetRoot();
        var rewriter = new AssertionWrappingRewriter();
        rewriter.Visit(root);
        return rewriter.ChangeCount;
    }
}
