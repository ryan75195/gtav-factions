using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FactionWars.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PublicMethodCountAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "CI0004";
    public const int MaxPublicMethods = 10;

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Too many public methods",
        "'{0}' declares {1} public methods (max {2}) — consider splitting responsibilities",
        "Design",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

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

        if (type.TypeKind != TypeKind.Class)
        {
            return;
        }

        if (type.IsRecord)
        {
            return;
        }

        // Exclude abstract classes, but NOT static classes.
        // Static classes are IsAbstract && IsStatic in Roslyn.
        if (type.IsAbstract && !type.IsStatic)
        {
            return;
        }

        var ns = type.ContainingNamespace?.ToDisplayString() ?? "";
        if (ns.Contains(".Tests.") || ns.EndsWith(".Tests"))
        {
            return;
        }

        if (type.GetAttributes().Any(a =>
            a.AttributeClass?.Name == "TestFixtureAttribute"))
        {
            return;
        }

        if (type.Name.StartsWith("Mock", System.StringComparison.Ordinal)
            || ns.Contains(".Models"))
        {
            return;
        }

        var publicMethodCount = type.GetMembers()
            .OfType<IMethodSymbol>()
            .Count(m => m.DeclaredAccessibility == Accessibility.Public
                && m.MethodKind == MethodKind.Ordinary
                && !m.IsOverride
                && !ImplementsInterfaceMember(type, m)
                && !AnalyzerConstants.ExcludedMethodNames.Contains(m.Name));

        if (publicMethodCount > MaxPublicMethods)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rule, type.Locations[0],
                    type.Name, publicMethodCount, MaxPublicMethods));
        }
    }

    private static bool ImplementsInterfaceMember(INamedTypeSymbol type, IMethodSymbol method)
    {
        foreach (var interfaceType in type.AllInterfaces)
        {
            foreach (var interfaceMethod in interfaceType.GetMembers().OfType<IMethodSymbol>())
            {
                var implementation = type.FindImplementationForInterfaceMember(interfaceMethod);
                if (SymbolEqualityComparer.Default.Equals(implementation, method))
                    return true;
            }
        }

        return false;
    }
}

