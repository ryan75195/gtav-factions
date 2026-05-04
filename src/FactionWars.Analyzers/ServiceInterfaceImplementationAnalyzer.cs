using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FactionWars.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ServiceInterfaceImplementationAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "CI0015";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Service class has no interface",
        "'{0}' is in a service-like namespace but implements no first-party interface",
        "Design",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly string[] ServiceNamespaceSegments =
    {
        ".Services",
        ".Repositories",
        ".Pools",
        ".Sinks",
        ".Handlers",
        ".Controllers"
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
    }

    private static void AnalyzeType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind != TypeKind.Class
            || type.IsAbstract
            || type.IsStatic
            || type.IsRecord
            || type.DeclaredAccessibility != Accessibility.Public)
        {
            return;
        }

        var ns = type.ContainingNamespace?.ToDisplayString() ?? "";
        if (ns.Contains(".Tests.", StringComparison.Ordinal)
            || ns.EndsWith(".Tests", StringComparison.Ordinal)
            || !ServiceNamespaceSegments.Any(segment => ns.Contains(segment, StringComparison.Ordinal)))
        {
            return;
        }

        var implementsFirstPartyInterface = type.AllInterfaces.Any(i =>
            (i.ContainingNamespace?.ToDisplayString() ?? "").StartsWith("FactionWars", StringComparison.Ordinal));

        if (!implementsFirstPartyInterface)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, type.Locations[0], type.Name));
        }
    }
}
