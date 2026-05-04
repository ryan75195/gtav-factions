using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FactionWars.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class OnePublicTypePerFileAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "CI0016";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Multiple public top-level types in one file",
        "File declares multiple public top-level types: {0}",
        "Design",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
    }

    private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
    {
        if (context.Tree.FilePath.Contains("\\tests\\") || context.Tree.FilePath.Contains("/tests/"))
        {
            return;
        }

        var root = context.Tree.GetRoot(context.CancellationToken);
        var publicTopLevelTypes = root.DescendantNodes()
            .OfType<BaseTypeDeclarationSyntax>()
            .Where(t => t.Modifiers.Any(SyntaxKind.PublicKeyword))
            .Where(t => t.Parent is CompilationUnitSyntax || t.Parent is BaseNamespaceDeclarationSyntax)
            .ToList();

        if (publicTopLevelTypes.Count <= 1)
        {
            return;
        }

        var typeNames = string.Join(", ", publicTopLevelTypes.Select(t => t.Identifier.Text));
        var location = publicTopLevelTypes[1].Identifier.GetLocation();
        context.ReportDiagnostic(Diagnostic.Create(Rule, location, typeNames));
    }
}
