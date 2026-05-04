using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FactionWars.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NoCommentsAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "CI0013";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Comments should justify their existence",
        "Comment found - prefer self-documenting code unless this documents a native/game constraint or non-obvious reason",
        "Readability",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(AnalyzeTree);
    }

    private static void AnalyzeTree(SyntaxTreeAnalysisContext context)
    {
        var root = context.Tree.GetRoot(context.CancellationToken);
        foreach (var trivia in root.DescendantTrivia())
        {
            if (IsComment(trivia))
                context.ReportDiagnostic(Diagnostic.Create(Rule, trivia.GetLocation()));
        }
    }

    private static bool IsComment(SyntaxTrivia trivia)
    {
        var kind = trivia.Kind();
        return kind == SyntaxKind.SingleLineCommentTrivia
            || kind == SyntaxKind.MultiLineCommentTrivia
            || kind == SyntaxKind.SingleLineDocumentationCommentTrivia
            || kind == SyntaxKind.MultiLineDocumentationCommentTrivia;
    }
}
