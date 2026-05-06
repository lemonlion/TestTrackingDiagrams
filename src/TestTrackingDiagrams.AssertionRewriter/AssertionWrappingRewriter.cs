using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TestTrackingDiagrams.AssertionRewriter;

/// <summary>
/// Roslyn syntax rewriter that wraps FluentAssertions .Should() expression statements
/// in Track.That(() => ...) or Track.ThatAsync(async () => await ...) calls.
/// </summary>
public class AssertionWrappingRewriter : CSharpSyntaxRewriter
{
    private bool _disabled;

    /// <summary>
    /// The original source file path. When set, the rewriter embeds this as an explicit
    /// <c>callerFilePath</c> argument so that assertion tooltips reference the original file
    /// rather than the rewritten intermediate file.
    /// </summary>
    public string? OriginalFilePath { get; set; }

    /// <summary>
    /// Number of expression statements that were wrapped.
    /// </summary>
    public int ChangeCount { get; private set; }

    public override SyntaxNode? VisitCompilationUnit(CompilationUnitSyntax node)
    {
        var result = (CompilationUnitSyntax)base.VisitCompilationUnit(node)!;

        // Add using directive if changes were made and it's not already present
        if (ChangeCount > 0 && !HasTrackingUsing(result))
        {
            var usingDirective = SyntaxFactory.UsingDirective(
                SyntaxFactory.ParseName(" TestTrackingDiagrams.Tracking"))
                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

            result = result.AddUsings(usingDirective);
        }

        return result;
    }

    public override SyntaxNode? VisitExpressionStatement(ExpressionStatementSyntax node)
    {
        UpdatePragmaStateFromTrivia(node.GetLeadingTrivia());

        // Check trailing trivia for inline pragma disable
        if (HasInlinePragmaDisable(node))
            return node;

        if (_disabled)
            return node;

        // Check if containing method or class has [SuppressAssertionTracking]
        if (IsInSuppressedScope(node))
            return node;

        // Check if expression contains .Should() invocation
        if (!ContainsShouldInvocation(node.Expression))
            return node;

        // Check if already wrapped in Track.That or Track.ThatAsync
        if (IsAlreadyWrapped(node.Expression))
            return node;

        // Skip if expression references out/ref/in parameters (can't capture in lambda)
        if (ReferencesOutRefInParameter(node.Expression, node))
            return node;

        // Wrap the expression
        var wrappedExpression = WrapExpression(node.Expression, node.GetLocation());
        ChangeCount++;

        // Surround with #pragma warning disable/restore for nullable warnings
        // that arise from lambda capture breaking flow analysis (CS8602/CS8604/CS8629)
        var pragmaDisable = SyntaxFactory.Trivia(
            SyntaxFactory.PragmaWarningDirectiveTrivia(SyntaxFactory.Token(SyntaxKind.DisableKeyword), true)
                .WithErrorCodes(SyntaxFactory.SeparatedList<ExpressionSyntax>(new[]
                {
                    SyntaxFactory.IdentifierName("CS8602"),
                    SyntaxFactory.IdentifierName("CS8604"),
                    SyntaxFactory.IdentifierName("CS8629")
                }))
                .WithEndOfDirectiveToken(SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.EndOfDirectiveToken, SyntaxFactory.TriviaList(SyntaxFactory.LineFeed))));

        var pragmaRestore = SyntaxFactory.Trivia(
            SyntaxFactory.PragmaWarningDirectiveTrivia(SyntaxFactory.Token(SyntaxKind.RestoreKeyword), true)
                .WithErrorCodes(SyntaxFactory.SeparatedList<ExpressionSyntax>(new[]
                {
                    SyntaxFactory.IdentifierName("CS8602"),
                    SyntaxFactory.IdentifierName("CS8604"),
                    SyntaxFactory.IdentifierName("CS8629")
                }))
                .WithEndOfDirectiveToken(SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.EndOfDirectiveToken, SyntaxFactory.TriviaList(SyntaxFactory.LineFeed))));

        var existingLeading = node.GetLeadingTrivia();
        var existingTrailing = node.GetTrailingTrivia();

        var newLeading = existingLeading.Add(pragmaDisable);
        var newTrailing = existingTrailing.Insert(0, pragmaRestore);

        return node.WithExpression(wrappedExpression)
            .WithLeadingTrivia(newLeading)
            .WithTrailingTrivia(newTrailing);
    }

    public override SyntaxNode? VisitArrowExpressionClause(ArrowExpressionClauseSyntax node)
    {
        UpdatePragmaStateFromTrivia(node.GetLeadingTrivia());

        if (_disabled)
            return node;

        // Check if containing method or class has [SuppressAssertionTracking]
        if (IsInSuppressedScope(node))
            return node;

        if (!ContainsShouldInvocation(node.Expression))
            return node;

        if (IsAlreadyWrapped(node.Expression))
            return node;

        var wrappedExpression = WrapExpression(node.Expression, node.GetLocation());
        ChangeCount++;

        return node.WithExpression(wrappedExpression);
    }

    private ExpressionSyntax WrapExpression(ExpressionSyntax expression, Location? location)
    {
        var callerArgs = BuildCallerInfoArguments(location);

        // Handle await expressions: await x.Should().ThrowAsync<T>()
        // -> await Track.ThatAsync(async () => await x.Should().ThrowAsync<T>(), callerFilePath: "...", callerLineNumber: N)
        if (expression is AwaitExpressionSyntax awaitExpr)
        {
            var awaitInner = SyntaxFactory.AwaitExpression(
                SyntaxFactory.Token(SyntaxKind.AwaitKeyword).WithTrailingTrivia(SyntaxFactory.Space),
                awaitExpr.Expression.WithoutLeadingTrivia());

            var asyncLambda = SyntaxFactory.ParenthesizedLambdaExpression(
                    SyntaxFactory.ParameterList(),
                    (CSharpSyntaxNode)awaitInner)
                .WithAsyncKeyword(SyntaxFactory.Token(SyntaxKind.AsyncKeyword).WithTrailingTrivia(SyntaxFactory.Space))
                .WithArrowToken(SyntaxFactory.Token(SyntaxKind.EqualsGreaterThanToken)
                    .WithLeadingTrivia(SyntaxFactory.Space)
                    .WithTrailingTrivia(SyntaxFactory.Space));

            var args = new List<ArgumentSyntax> { SyntaxFactory.Argument(asyncLambda) };
            args.AddRange(callerArgs);

            var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Track"),
                    SyntaxFactory.IdentifierName("ThatAsync")),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(args)));

            return SyntaxFactory.AwaitExpression(
                SyntaxFactory.Token(SyntaxKind.AwaitKeyword).WithTrailingTrivia(SyntaxFactory.Space),
                invocation);
        }

        // Non-top-level await: expression contains await in arguments (e.g. string interpolation)
        // -> await Track.ThatAsync(async () => x.Should().Be(await Foo()), ...)
        if (ContainsAwaitExpression(expression))
        {
            var asyncLambda = SyntaxFactory.ParenthesizedLambdaExpression(
                    SyntaxFactory.ParameterList(),
                    (CSharpSyntaxNode)expression.WithoutLeadingTrivia().WithoutTrailingTrivia())
                .WithAsyncKeyword(SyntaxFactory.Token(SyntaxKind.AsyncKeyword).WithTrailingTrivia(SyntaxFactory.Space))
                .WithArrowToken(SyntaxFactory.Token(SyntaxKind.EqualsGreaterThanToken)
                    .WithLeadingTrivia(SyntaxFactory.Space)
                    .WithTrailingTrivia(SyntaxFactory.Space));

            var args = new List<ArgumentSyntax> { SyntaxFactory.Argument(asyncLambda) };
            args.AddRange(callerArgs);

            var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Track"),
                    SyntaxFactory.IdentifierName("ThatAsync")),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(args)));

            return SyntaxFactory.AwaitExpression(
                SyntaxFactory.Token(SyntaxKind.AwaitKeyword).WithTrailingTrivia(SyntaxFactory.Space),
                invocation);
        }

        // Normal case: x.Should().Be(1) -> Track.That(() => x.Should().Be(1), callerFilePath: "...", callerLineNumber: N)
        var lambda = SyntaxFactory.ParenthesizedLambdaExpression(
                SyntaxFactory.ParameterList(),
                (CSharpSyntaxNode)expression.WithoutLeadingTrivia().WithoutTrailingTrivia())
            .WithArrowToken(SyntaxFactory.Token(SyntaxKind.EqualsGreaterThanToken)
                .WithLeadingTrivia(SyntaxFactory.Space)
                .WithTrailingTrivia(SyntaxFactory.Space));

        var normalArgs = new List<ArgumentSyntax> { SyntaxFactory.Argument(lambda) };
        normalArgs.AddRange(callerArgs);

        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Track"),
                SyntaxFactory.IdentifierName("That")),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(normalArgs)));
    }

    private List<ArgumentSyntax> BuildCallerInfoArguments(Location? location)
    {
        var args = new List<ArgumentSyntax>();
        if (OriginalFilePath == null || location == null)
            return args;

        var lineSpan = location.GetLineSpan();
        var lineNumber = lineSpan.StartLinePosition.Line + 1; // 1-based

        // callerFilePath: @"C:\path\to\file.cs"
        args.Add(SyntaxFactory.Argument(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal($"@\"{OriginalFilePath.Replace("\"", "\"\"")}\"", OriginalFilePath)))
            .WithNameColon(SyntaxFactory.NameColon("callerFilePath")
                .WithTrailingTrivia(SyntaxFactory.Space)));

        // callerLineNumber: N
        args.Add(SyntaxFactory.Argument(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(lineNumber)))
            .WithNameColon(SyntaxFactory.NameColon("callerLineNumber")
                .WithTrailingTrivia(SyntaxFactory.Space)));

        return args;
    }

    private static bool ContainsShouldInvocation(ExpressionSyntax expression)
    {
        foreach (var node in expression.DescendantNodesAndSelf())
        {
            if (node is InvocationExpressionSyntax invocation &&
                invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name.Identifier.ValueText == "Should" &&
                invocation.ArgumentList.Arguments.Count == 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsAwaitExpression(ExpressionSyntax expression)
    {
        foreach (var node in expression.DescendantNodes())
        {
            if (node is AwaitExpressionSyntax)
                return true;
        }

        return false;
    }

    private static bool ReferencesOutRefInParameter(ExpressionSyntax expression, SyntaxNode containingNode)
    {
        // Find the containing method declaration
        var method = containingNode.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (method == null)
            return false;

        // Collect names of out/ref/in parameters
        var refParamNames = new HashSet<string>();
        foreach (var param in method.ParameterList.Parameters)
        {
            foreach (var modifier in param.Modifiers)
            {
                if (modifier.IsKind(SyntaxKind.OutKeyword) ||
                    modifier.IsKind(SyntaxKind.RefKeyword) ||
                    modifier.IsKind(SyntaxKind.InKeyword))
                {
                    refParamNames.Add(param.Identifier.ValueText);
                    break;
                }
            }
        }

        if (refParamNames.Count == 0)
            return false;

        // Check if the expression references any of those parameter names
        foreach (var node in expression.DescendantNodesAndSelf())
        {
            if (node is IdentifierNameSyntax identifier &&
                refParamNames.Contains(identifier.Identifier.ValueText))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAlreadyWrapped(ExpressionSyntax expression)
    {
        // Check for Track.That(...) or Track.ThatAsync(...)
        var target = expression is AwaitExpressionSyntax awaitExpr ? awaitExpr.Expression : expression;

        if (target is InvocationExpressionSyntax invocation &&
            invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Expression is IdentifierNameSyntax identifier &&
            identifier.Identifier.ValueText == "Track" &&
            (memberAccess.Name.Identifier.ValueText == "That" ||
             memberAccess.Name.Identifier.ValueText == "ThatAsync"))
        {
            return true;
        }

        return false;
    }

    private static bool IsInSuppressedScope(SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is MethodDeclarationSyntax method)
            {
                if (HasSuppressAttribute(method.AttributeLists))
                    return true;
            }
            else if (current is ClassDeclarationSyntax classDecl)
            {
                if (HasSuppressAttribute(classDecl.AttributeLists))
                    return true;
            }

            current = current.Parent;
        }

        return false;
    }

    private static bool HasSuppressAttribute(SyntaxList<AttributeListSyntax> attributeLists)
    {
        foreach (var attrList in attributeLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                var name = attr.Name.ToString();
                if (name == "SuppressAssertionTracking" ||
                    name == "SuppressAssertionTrackingAttribute")
                    return true;
            }
        }

        return false;
    }

    private void UpdatePragmaStateFromTrivia(SyntaxTriviaList triviaList)
    {
        foreach (var trivia in triviaList)
        {
            if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
            {
                var text = trivia.ToString().Trim();
                if (text.Contains("pragma:TrackAssertions:disable"))
                    _disabled = true;
                else if (text.Contains("pragma:TrackAssertions:enable"))
                    _disabled = false;
            }
        }
    }

    private static bool HasInlinePragmaDisable(ExpressionStatementSyntax node)
    {
        // Check trailing trivia of the statement (after the semicolon)
        var trailingTrivia = node.GetTrailingTrivia();
        foreach (var trivia in trailingTrivia)
        {
            if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) &&
                trivia.ToString().Contains("pragma:TrackAssertions:disable"))
                return true;
        }

        // Also check trailing trivia of the semicolon token
        var semicolonTrivia = node.SemicolonToken.TrailingTrivia;
        foreach (var trivia in semicolonTrivia)
        {
            if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) &&
                trivia.ToString().Contains("pragma:TrackAssertions:disable"))
                return true;
        }

        return false;
    }

    private static bool HasTrackingUsing(CompilationUnitSyntax root)
    {
        foreach (var usingDirective in root.Usings)
        {
            if (usingDirective.Name?.ToString() == "TestTrackingDiagrams.Tracking")
                return true;
        }

        return false;
    }
}
