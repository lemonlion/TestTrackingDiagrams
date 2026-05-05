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

        // Wrap the expression
        var wrappedExpression = WrapExpression(node.Expression);
        ChangeCount++;

        return node.WithExpression(wrappedExpression)
            .WithLeadingTrivia(node.GetLeadingTrivia())
            .WithTrailingTrivia(node.GetTrailingTrivia());
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

        var wrappedExpression = WrapExpression(node.Expression);
        ChangeCount++;

        return node.WithExpression(wrappedExpression);
    }

    private ExpressionSyntax WrapExpression(ExpressionSyntax expression)
    {
        // Handle await expressions: await x.Should().ThrowAsync<T>()
        // -> await Track.ThatAsync(async () => await x.Should().ThrowAsync<T>())
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

            var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Track"),
                    SyntaxFactory.IdentifierName("ThatAsync")),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(asyncLambda))));

            return SyntaxFactory.AwaitExpression(
                SyntaxFactory.Token(SyntaxKind.AwaitKeyword).WithTrailingTrivia(SyntaxFactory.Space),
                invocation);
        }

        // Normal case: x.Should().Be(1) -> Track.That(() => x.Should().Be(1))
        var lambda = SyntaxFactory.ParenthesizedLambdaExpression(
                SyntaxFactory.ParameterList(),
                (CSharpSyntaxNode)expression.WithoutLeadingTrivia().WithoutTrailingTrivia())
            .WithArrowToken(SyntaxFactory.Token(SyntaxKind.EqualsGreaterThanToken)
                .WithLeadingTrivia(SyntaxFactory.Space)
                .WithTrailingTrivia(SyntaxFactory.Space));

        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Track"),
                SyntaxFactory.IdentifierName("That")),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(lambda))));
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
