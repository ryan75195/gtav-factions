using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FactionWars.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NotNullOnlyAssertionAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "CI0009";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Test asserts non-null without verifying data",
        "Test '{0}' asserts '{1}' is not null but never verifies its data - add assertions on actual values",
        "Testing",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var method = (MethodDeclarationSyntax)context.Node;
        if (!IsTestMethod(method) || method.Body == null)
            return;

        var assertions = method.Body.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(IsAssertionCall)
            .ToImmutableArray();

        foreach (var assertion in assertions)
        {
            if (!TryGetNotNullTarget(assertion, out var variableName, out var location))
                continue;

            if (!HasFollowUpAssertion(assertions, variableName, assertion))
                context.ReportDiagnostic(Diagnostic.Create(Rule, location, method.Identifier.Text, variableName));
        }
    }

    private static bool IsTestMethod(MethodDeclarationSyntax method)
    {
        return method.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(attribute => IsTestAttribute(attribute.Name.ToString()));
    }

    private static bool IsTestAttribute(string name)
    {
        return name is "Fact" or "Xunit.Fact"
            or "Theory" or "Xunit.Theory"
            or "Test" or "NUnit.Framework.Test"
            or "TestCase" or "NUnit.Framework.TestCase";
    }

    private static bool IsAssertionCall(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;

        var target = memberAccess.Expression.ToString();
        return target is "Assert" or "Xunit.Assert" or "NUnit.Framework.Assert";
    }

    private static bool TryGetNotNullTarget(
        InvocationExpressionSyntax assertion,
        out string variableName,
        out Location location)
    {
        variableName = string.Empty;
        location = assertion.GetLocation();

        if (assertion.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;

        var arguments = assertion.ArgumentList.Arguments;
        if (memberAccess.Name.Identifier.Text == "NotNull" && arguments.Count >= 1)
        {
            return TryGetIdentifier(arguments[0].Expression, out variableName, out location);
        }

        if (memberAccess.Name.Identifier.Text != "That" || arguments.Count < 2)
            return false;

        if (!IsNUnitNotNullConstraint(arguments[1].Expression))
            return false;

        location = memberAccess.Name.GetLocation();
        return TryGetIdentifier(arguments[0].Expression, out variableName, out _);
    }

    private static bool TryGetIdentifier(ExpressionSyntax expression, out string variableName, out Location location)
    {
        if (expression is IdentifierNameSyntax identifier)
        {
            variableName = identifier.Identifier.Text;
            location = identifier.GetLocation();
            return true;
        }

        variableName = string.Empty;
        location = expression.GetLocation();
        return false;
    }

    private static bool IsNUnitNotNullConstraint(ExpressionSyntax expression)
    {
        return expression is MemberAccessExpressionSyntax outerAccess
            && outerAccess.Name.Identifier.Text == "Null"
            && outerAccess.Expression is MemberAccessExpressionSyntax innerAccess
            && innerAccess.Name.Identifier.Text == "Not"
            && innerAccess.Expression.ToString() is "Is" or "NUnit.Framework.Is";
    }

    private static bool HasFollowUpAssertion(
        ImmutableArray<InvocationExpressionSyntax> assertions,
        string variableName,
        InvocationExpressionSyntax notNullAssertion)
    {
        foreach (var assertion in assertions)
        {
            if (assertion == notNullAssertion || assertion.ArgumentList.Arguments.Count == 0)
                continue;

            var firstArgText = assertion.ArgumentList.Arguments[0].Expression.ToString();
            if (ReferencesVariable(firstArgText, variableName))
                return true;
        }

        return false;
    }

    private static bool ReferencesVariable(string expressionText, string variableName)
    {
        return expressionText.StartsWith(variableName + ".", System.StringComparison.Ordinal)
            || expressionText.StartsWith(variableName + "!", System.StringComparison.Ordinal)
            || expressionText.StartsWith(variableName + "?", System.StringComparison.Ordinal);
    }
}
