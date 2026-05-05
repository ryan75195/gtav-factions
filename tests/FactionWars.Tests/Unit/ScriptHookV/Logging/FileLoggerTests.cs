using System;
using System.IO;
using System.Reflection;
using FactionWars.ScriptHookV.Logging;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Logging
{
    public sealed class FileLoggerTests : IDisposable
    {
        private readonly string? _originalLogDir;

        public FileLoggerTests()
        {
            _originalLogDir = Environment.GetEnvironmentVariable(FileLogger.LogDirectoryEnvironmentVariable);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(FileLogger.LogDirectoryEnvironmentVariable, _originalLogDir);
        }

        [Fact]
        public void GetResolvedLogDirectory_WhenRunningUnderTests_DefaultsToTempTestLogs()
        {
            Environment.SetEnvironmentVariable(FileLogger.LogDirectoryEnvironmentVariable, null);

            var logDirectory = ResolveLogDirectory();

            Assert.Equal(Path.Combine(Path.GetTempPath(), "FactionWars", "TestLogs"), logDirectory);
        }

        [Fact]
        public void GetResolvedLogDirectory_UsesEnvironmentOverride()
        {
            var overrideDir = Path.Combine(Path.GetTempPath(), "FactionWars", "OverrideLogs", Guid.NewGuid().ToString("N"));
            Environment.SetEnvironmentVariable(FileLogger.LogDirectoryEnvironmentVariable, overrideDir);

            var logDirectory = ResolveLogDirectory();

            Assert.Equal(overrideDir, logDirectory);
        }

        private static string ResolveLogDirectory()
        {
            return (string)typeof(FileLogger)
                .GetMethod("ResolveLogDirectory", BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, null)!;
        }
    }
}
