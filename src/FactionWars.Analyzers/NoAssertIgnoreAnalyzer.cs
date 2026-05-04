using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FactionWars.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NoAssertIgnoreAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "CI0010";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Do not skip tests",
        "Do not skip tests with Assert.Ignore or Skip - tests should fail loudly when preconditions are not met",
        "Testing",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }

        if (memberAccess.Name.Identifier.Text != "Ignore")
        {
            return;
        }

        var symbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol;
        if (symbol is not IMethodSymbol method)
        {
            return;
        }

        var containingType = method.ContainingType?.ToDisplayString();
        if (containingType == "NUnit.Framework.Assert")
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
        }
    }

    private static void AnalyzeAttribute(SyntaxNodeAnalysisContext context)
    {
        var attribute = (AttributeSyntax)context.Node;
        var name = attribute.Name.ToString();
        if (name != "Fact"
            && name != "Theory"
            && name != "FactAttribute"
            && name != "TheoryAttribute"
            && !name.EndsWith(".Fact", System.StringComparison.Ordinal)
            && !name.EndsWith(".Theory", System.StringComparison.Ordinal))
        {
            return;
        }

        var hasSkip = attribute.ArgumentList?.Arguments.Any(arg =>
            arg.NameEquals?.Name.Identifier.Text == "Skip") == true;

        if (hasSkip)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, attribute.GetLocation()));
        }
    }
}


