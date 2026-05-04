using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FactionWars.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ClassLengthAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "CI0017";
    private const int MaxLines = 500;

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Type is too large",
        "'{0}' is {1} lines long (max {2}) - split responsibilities or move cohesive members to partial files",
        "Maintainability",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeType, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeType(SyntaxNodeAnalysisContext context)
    {
        var typeDeclaration = (ClassDeclarationSyntax)context.Node;
        var type = context.SemanticModel.GetDeclaredSymbol(typeDeclaration);
        if (type == null || ShouldSkip(type))
            return;

        var lineSpan = typeDeclaration.GetLocation().GetLineSpan();
        var lineCount = lineSpan.EndLinePosition.Line - lineSpan.StartLinePosition.Line + 1;
        if (lineCount <= MaxLines)
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            typeDeclaration.Identifier.GetLocation(),
            type.Name,
            lineCount,
            MaxLines));
    }

    private static bool ShouldSkip(INamedTypeSymbol type)
    {
        var ns = type.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        return type.Name.StartsWith("Mock", System.StringComparison.Ordinal)
            || type.Name.EndsWith("Tests", System.StringComparison.Ordinal)
            || ns.Contains(".Models")
            || ns.Contains(".Tests")
            || type.DeclaringSyntaxReferences.Length == 0
            || type.AllInterfaces.Any(i => i.Name == "IEnumerable");
    }
}
