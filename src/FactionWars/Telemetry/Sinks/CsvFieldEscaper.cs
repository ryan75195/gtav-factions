namespace FactionWars.Telemetry.Sinks
{
    /// <summary>
    /// Escapes a single field value for inclusion in a CSV row per RFC 4180.
    /// Wraps in double-quotes when the field contains a comma, quote, or newline;
    /// internal quotes are doubled.
    /// </summary>
    public static class CsvFieldEscaper
    {
        public static string Escape(string? value)
        {
            if (value == null)
                return string.Empty;

            bool needsQuoting = value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
            if (!needsQuoting)
                return value;

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
