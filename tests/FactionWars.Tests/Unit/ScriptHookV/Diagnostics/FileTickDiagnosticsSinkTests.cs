using System;
using System.Collections.Generic;
using System.IO;
using FactionWars.ScriptHookV.Diagnostics;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Diagnostics
{
    public class FileTickDiagnosticsSinkTests : IDisposable
    {
        private readonly string _path = Path.Combine(Path.GetTempPath(), "fw_breadcrumb_" + Guid.NewGuid().ToString("N") + ".txt");

        public void Dispose()
        {
            if (File.Exists(_path)) File.Delete(_path);
        }

        [Fact]
        public void WriteBreadcrumb_WritesContentToFile()
        {
            var sink = new FileTickDiagnosticsSink(_path, _ => { });
            sink.WriteBreadcrumb("phaseA (tick+1200ms)");
            Assert.Equal("phaseA (tick+1200ms)", File.ReadAllText(_path));
        }

        [Fact]
        public void WriteBreadcrumb_OverwritesPreviousContent()
        {
            var sink = new FileTickDiagnosticsSink(_path, _ => { });
            sink.WriteBreadcrumb("first");
            sink.WriteBreadcrumb("second");
            Assert.Equal("second", File.ReadAllText(_path));
        }

        [Fact]
        public void ReportSlowTick_InvokesLogger()
        {
            var captured = new List<string>();
            var sink = new FileTickDiagnosticsSink(_path, captured.Add);
            sink.ReportSlowTick("SLOW TICK total=1200ms");
            Assert.Single(captured);
            Assert.Equal("SLOW TICK total=1200ms", captured[0]);
        }
    }
}
