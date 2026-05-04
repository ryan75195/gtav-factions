using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FactionWars.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ConcreteConstructorParameterAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "CI0014";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Constructor depends on concrete production type",
        "'{0}' constructor takes concrete type '{1}' - prefer an interface for service dependencies",
        "Design",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeConstructor, SyntaxKind.ConstructorDeclaration);
    }

    private static void AnalyzeConstructor(SyntaxNodeAnalysisContext context)
    {
        var constructor = (ConstructorDeclarationSyntax)context.Node;
        if (constructor.Modifiers.Any(SyntaxKind.StaticKeyword))
        {
            return;
        }

        if (constructor.Parent is not TypeDeclarationSyntax typeDecl)
        {
            return;
        }

        var containingType = context.SemanticModel.GetDeclaredSymbol(typeDecl);
        if (containingType == null || ShouldSkipContainingType(containingType))
        {
            return;
        }

        foreach (var parameter in constructor.ParameterList.Parameters)
        {
            if (parameter.Type == null)
            {
                continue;
            }

            var parameterType = context.SemanticModel.GetTypeInfo(parameter.Type).Type;
            if (parameterType == null || ShouldSkipParameterType(parameterType))
            {
                continue;
            }

            context.ReportDiagnostic(
                Diagnostic.Create(Rule, parameter.GetLocation(), containingType.Name, parameterType.Name));
        }
    }

    private static bool ShouldSkipContainingType(INamedTypeSymbol type)
    {
        if (type.TypeKind != TypeKind.Class || type.IsRecord)
        {
            return true;
        }

        var ns = type.ContainingNamespace?.ToDisplayString() ?? "";
        return ns.Contains(".Tests.", StringComparison.Ordinal)
            || ns.EndsWith(".Tests", StringComparison.Ordinal)
            || ns.Contains(".Models", StringComparison.Ordinal)
            || ns.Contains(".Events", StringComparison.Ordinal);
    }

    private static bool ShouldSkipParameterType(ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Interface
            || type.IsValueType
            || type.TypeKind == TypeKind.Enum
            || type.TypeKind == TypeKind.TypeParameter)
        {
            return true;
        }

        if (type.SpecialType == SpecialType.System_String || type is IArrayTypeSymbol)
        {
            return true;
        }

        if (IsDelegate(type))
        {
            return true;
        }

        var ns = type.ContainingNamespace?.ToDisplayString() ?? "";
        if (!ns.StartsWith("FactionWars", StringComparison.Ordinal))
        {
            return true;
        }

        return ns.Contains(".Models", StringComparison.Ordinal)
            || ns.Contains(".Events", StringComparison.Ordinal)
            || type.Name.EndsWith("Options", StringComparison.Ordinal)
            || type.Name.EndsWith("EventArgs", StringComparison.Ordinal);
    }

    private static bool IsDelegate(ITypeSymbol type)
    {
        var current = type.BaseType;
        while (current != null)
        {
            var displayName = current.ToDisplayString();
            if (displayName == "System.MulticastDelegate" || displayName == "System.Delegate")
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }
}
