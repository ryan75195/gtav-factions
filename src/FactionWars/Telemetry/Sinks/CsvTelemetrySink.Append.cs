using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.Telemetry.Sinks
{
    public sealed partial class CsvTelemetrySink
    {
        private void AppendLocked(string fileName, string header, IEnumerable<string> rows)
        {
            if (_saveDir == null) return;
            var path = Path.Combine(_saveDir, fileName);
            try
            {
                bool needsHeader = !File.Exists(path);
                var sb = new StringBuilder();
                if (needsHeader) sb.AppendLine(header);
                foreach (var row in rows) sb.AppendLine(row);
                File.AppendAllText(path, sb.ToString());
            }
            catch (Exception ex)
            {
                if (_erroredFiles.Add(path))
                    FileLogger.Error($"CsvTelemetrySink: failed to append to {path}", ex);
            }
        }

    }
}
