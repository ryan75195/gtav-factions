using System.Collections.Immutable;

namespace FactionWars.Analyzers;

internal static class AnalyzerConstants
{
    internal static readonly ImmutableHashSet<string> ExcludedMethodNames =
        ImmutableHashSet.Create(
            "Dispose", "DisposeAsync", "ToString", "Equals",
            "GetHashCode", "GetType", "Finalize",
            "Start", "StartAsync", "StopAsync", "ExecuteAsync");

    internal static bool IsAccessorLike(this Microsoft.CodeAnalysis.IMethodSymbol method)
    {
        return method.Name.StartsWith("Get", System.StringComparison.Ordinal)
            || method.Name.StartsWith("Set", System.StringComparison.Ordinal)
            || method.Name.StartsWith("Is", System.StringComparison.Ordinal)
            || method.Name.StartsWith("Has", System.StringComparison.Ordinal);
    }
}

