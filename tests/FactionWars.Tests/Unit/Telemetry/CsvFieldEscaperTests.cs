using FactionWars.Telemetry.Sinks;
using Xunit;

namespace FactionWars.Tests.Unit.Telemetry
{
    public class CsvFieldEscaperTests
    {
        [Fact]
        public void Escape_PlainString_ReturnsUnchanged()
        {
            Assert.Equal("hello", CsvFieldEscaper.Escape("hello"));
        }

        [Fact]
        public void Escape_Null_ReturnsEmptyString()
        {
            Assert.Equal(string.Empty, CsvFieldEscaper.Escape(null));
        }

        [Fact]
        public void Escape_ContainsComma_WrapsInQuotes()
        {
            Assert.Equal("\"hello, world\"", CsvFieldEscaper.Escape("hello, world"));
        }

        [Fact]
        public void Escape_ContainsQuote_DoublesQuoteAndWraps()
        {
            Assert.Equal("\"say \"\"hi\"\"\"", CsvFieldEscaper.Escape("say \"hi\""));
        }

        [Fact]
        public void Escape_ContainsNewline_WrapsInQuotes()
        {
            Assert.Equal("\"line1\nline2\"", CsvFieldEscaper.Escape("line1\nline2"));
        }
    }
}
