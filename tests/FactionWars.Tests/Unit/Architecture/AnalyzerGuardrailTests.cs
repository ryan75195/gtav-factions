using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace FactionWars.Tests.Unit.Architecture
{
    public class AnalyzerGuardrailTests
    {
        [Fact]
        public void NoAnonymousSerializationAnalyzer_ShouldFlagAnonymousJsonPayload()
        {
            var diagnostics = Analyze(
                "FactionWars.Analyzers.NoAnonymousSerializationAnalyzer",
                @"
using Newtonsoft.Json;

public class Sample
{
    public string Run()
    {
        return JsonConvert.SerializeObject(new { id = 1 });
    }
}");

            Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "CI0011");
        }

        [Fact]
        public void NotNullOnlyAssertionAnalyzer_ShouldFlagXunitNotNullWithoutFollowUp()
        {
            var diagnostics = Analyze(
                "FactionWars.Analyzers.NotNullOnlyAssertionAnalyzer",
                @"
using Xunit;

public class SampleTests
{
    [Fact]
    public void Creates_value()
    {
        var result = new object();
        Assert.NotNull(result);
    }
}");

            Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "CI0009");
        }

        [Fact]
        public void NoCommentsAnalyzer_ShouldFlagSourceComments()
        {
            var diagnostics = Analyze(
                "FactionWars.Analyzers.NoCommentsAnalyzer",
                @"
public class Sample
{
    // comment
    public void Run()
    {
    }
}");

            Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "CI0013");
        }

        [Fact]
        public void ClassLengthAnalyzer_ShouldFlagTypeOverTwoThousandLines()
        {
            var fillerLines = string.Join(
                Environment.NewLine,
                Enumerable.Range(1, 2001).Select(i => "    private int _field" + i + ";"));

            var diagnostics = Analyze(
                "FactionWars.Analyzers.ClassLengthAnalyzer",
                @"
public class Oversized
{
" + fillerLines + @"
}");

            Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "CI0017");
        }

        private static IReadOnlyCollection<Diagnostic> Analyze(string analyzerTypeName, string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var compilation = CSharpCompilation.Create(
                "AnalyzerGuardrailSample",
                new[] { syntaxTree },
                MetadataReferences(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var analyzer = CreateAnalyzer(analyzerTypeName);
            var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
            return compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult();
        }

        private static DiagnosticAnalyzer CreateAnalyzer(string typeName)
        {
            var analyzerAssemblyPath = Path.Combine(
                SolutionRoot(),
                "src",
                "FactionWars.Analyzers",
                "bin",
                "Debug",
                "netstandard2.0",
                "FactionWars.Analyzers.dll");

            var analyzerAssembly = Assembly.LoadFrom(analyzerAssemblyPath);
            var analyzerType = analyzerAssembly.GetType(typeName, throwOnError: true);
            return (DiagnosticAnalyzer)Activator.CreateInstance(analyzerType);
        }

        private static IEnumerable<MetadataReference> MetadataReferences()
        {
            var assemblies = new[]
            {
                typeof(object).Assembly,
                typeof(Enumerable).Assembly,
                typeof(Xunit.FactAttribute).Assembly,
                typeof(Newtonsoft.Json.JsonConvert).Assembly
            };

            return assemblies
                .Select(assembly => MetadataReference.CreateFromFile(assembly.Location));
        }

        private static string SolutionRoot()
        {
            var directory = AppContext.BaseDirectory;
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory, "FactionWars.sln")))
                    return directory;

                directory = Path.GetDirectoryName(directory);
            }

            throw new InvalidOperationException("Could not find FactionWars.sln.");
        }
    }
}
