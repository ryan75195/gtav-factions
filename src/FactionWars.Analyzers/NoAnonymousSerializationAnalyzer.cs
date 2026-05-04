using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FactionWars.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NoAnonymousSerializationAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "CI0011";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Do not serialize anonymous objects",
        "Anonymous object passed to {0} - use a named type instead",
        "Design",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var methodName = GetMethodName(invocation);

        if (!IsSerializationMethod(methodName))
            return;

        foreach (var argument in invocation.ArgumentList.Arguments)
        {
            if (ContainsAnonymousObject(argument.Expression, context.SemanticModel))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, argument.GetLocation(), methodName));
                return;
            }
        }
    }

    private static string? GetMethodName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null,
        };
    }

    private static bool IsSerializationMethod(string? methodName)
    {
        return methodName is "Serialize" or "SerializeAsync" or "SerializeObject";
    }

    private static bool ContainsAnonymousObject(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        if (expression is AnonymousObjectCreationExpressionSyntax)
            return true;

        var typeInfo = semanticModel.GetTypeInfo(expression);
        if (typeInfo.Type?.IsAnonymousType == true)
            return true;

        return expression.DescendantNodesAndSelf()
            .OfType<AnonymousObjectCreationExpressionSyntax>()
            .Any();
    }
}
