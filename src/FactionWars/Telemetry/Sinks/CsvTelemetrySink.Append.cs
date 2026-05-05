using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
                RotateIfHeaderMismatch(path, header);
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

        private static void RotateIfHeaderMismatch(string path, string header)
        {
            if (!File.Exists(path))
                return;

            using (var reader = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
            {
                var firstLine = reader.ReadLine();
                if (string.Equals(firstLine, header, StringComparison.Ordinal))
                    return;
            }

            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileNameWithoutExtension(path);
            var extension = Path.GetExtension(path);
            var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
            var legacyPath = Path.Combine(directory ?? string.Empty, fileName + ".legacy-" + stamp + extension);
            var suffix = 0;

            while (File.Exists(legacyPath))
            {
                suffix++;
                legacyPath = Path.Combine(directory ?? string.Empty,
                    fileName + ".legacy-" + stamp + "-" + suffix.ToString(CultureInfo.InvariantCulture) + extension);
            }

            File.Move(path, legacyPath);
        }

        private static void MergeDirectoryIntoTarget(string sourceDir, string targetDir)
        {
            foreach (var sourceFile in Directory.GetFiles(sourceDir))
            {
                var targetFile = Path.Combine(targetDir, Path.GetFileName(sourceFile));
                if (!File.Exists(targetFile))
                {
                    File.Move(sourceFile, targetFile);
                    continue;
                }

                var sourceLines = File.ReadAllLines(sourceFile);
                if (sourceLines.Length <= 1) continue;

                var targetLines = File.ReadAllLines(targetFile);
                if (targetLines.Length == 0 ||
                    !string.Equals(targetLines[0], sourceLines[0], StringComparison.Ordinal))
                {
                    RotateIfHeaderMismatch(targetFile, sourceLines[0]);
                    File.Move(sourceFile, targetFile);
                    continue;
                }

                var linesToAppend = sourceLines.Skip(1).Where(line => !string.IsNullOrWhiteSpace(line));
                File.AppendAllLines(targetFile, linesToAppend);
            }
        }

    }
}
