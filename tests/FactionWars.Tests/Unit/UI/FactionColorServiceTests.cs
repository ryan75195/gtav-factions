using FactionWars.Factions.Models;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using FactionWars.UI.Services;
using Xunit;

namespace FactionWars.Tests.Unit.UI
{
    /// <summary>
    /// Unit tests for the FactionColorService.
    /// Tests faction-to-color mapping, boundary color assignment, and color configuration.
    /// </summary>
    public class FactionColorServiceTests
    {
        #region GetColorForFactionType Tests

        [Fact]
        public void GetColorForFactionType_Michael_ReturnsBlueColor()
        {
            // Arrange
            var service = new FactionColorService();

            // Act
            var color = service.GetColorForFactionType(FactionType.Michael);

            // Assert
            Assert.True(color.B > color.R && color.B > color.G, "Michael's color should be predominantly blue");
        }

        [Fact]
        public void GetColorForFactionType_Trevor_ReturnsOrangeColor()
        {
            // Arrange
            var service = new FactionColorService();

            // Act
            var color = service.GetColorForFactionType(FactionType.Trevor);

            // Assert
            Assert.True(color.R > color.B, "Trevor's color should have more red than blue (orange)");
            Assert.True(color.R >= color.G, "Trevor's color should have red >= green (orange)");
        }

        [Fact]
        public void GetColorForFactionType_Franklin_ReturnsGreenColor()
        {
            // Arrange
            var service = new FactionColorService();

            // Act
            var color = service.GetColorForFactionType(FactionType.Franklin);

            // Assert
            Assert.True(color.G > color.R && color.G > color.B, "Franklin's color should be predominantly green");
        }

        [Fact]
        public void GetColorForFactionType_EachFaction_ReturnsDifferentColor()
        {
            // Arrange
            var service = new FactionColorService();

            // Act
            var michaelColor = service.GetColorForFactionType(FactionType.Michael);
            var trevorColor = service.GetColorForFactionType(FactionType.Trevor);
            var franklinColor = service.GetColorForFactionType(FactionType.Franklin);

            // Assert
            Assert.NotEqual(michaelColor, trevorColor);
            Assert.NotEqual(michaelColor, franklinColor);
            Assert.NotEqual(trevorColor, franklinColor);
        }

        [Fact]
        public void GetColorForFactionType_SameFactionType_ReturnsSameColor()
        {
            // Arrange
            var service = new FactionColorService();

            // Act
            var color1 = service.GetColorForFactionType(FactionType.Michael);
            var color2 = service.GetColorForFactionType(FactionType.Michael);

            // Assert
            Assert.Equal(color1, color2);
        }

        #endregion

        #region GetBoundaryColorForFactionType Tests

        [Fact]
        public void GetBoundaryColorForFactionType_Michael_ReturnsMichaelBoundaryColor()
        {
            // Arrange
            var service = new FactionColorService();

            // Act
            var boundaryColor = service.GetBoundaryColorForFactionType(FactionType.Michael);

            // Assert
            Assert.Equal(BoundaryColor.Michael, boundaryColor);
        }

        [Fact]
        public void GetBoundaryColorForFactionType_Trevor_ReturnsTrevorBoundaryColor()
        {
            // Arrange
            var service = new FactionColorService();

            // Act
            var boundaryColor = service.GetBoundaryColorForFactionType(FactionType.Trevor);

            // Assert
            Assert.Equal(BoundaryColor.Trevor, boundaryColor);
        }

        [Fact]
        public void GetBoundaryColorForFactionType_Franklin_ReturnsFranklinBoundaryColor()
        {
            // Arrange
            var service = new FactionColorService();

            // Act
            var boundaryColor = service.GetBoundaryColorForFactionType(FactionType.Franklin);

            // Assert
            Assert.Equal(BoundaryColor.Franklin, boundaryColor);
        }

        #endregion

        #region GetNeutralColor Tests

        [Fact]
        public void GetNeutralColor_ReturnsGrayishColor()
        {
            // Arrange
            var service = new FactionColorService();

            // Act
            var color = service.GetNeutralColor();

            // Assert - neutral should be gray/white (similar R, G, B values)
            var maxDiff = System.Math.Max(
                System.Math.Max(
                    System.Math.Abs(color.R - color.G),
                    System.Math.Abs(color.G - color.B)),
                System.Math.Abs(color.R - color.B));
            Assert.True(maxDiff < 50, "Neutral color should be grayish (R, G, B values close together)");
        }

        [Fact]
        public void GetNeutralColor_IsDifferentFromFactionColors()
        {
            // Arrange
            var service = new FactionColorService();

            // Act
            var neutralColor = service.GetNeutralColor();
            var michaelColor = service.GetColorForFactionType(FactionType.Michael);
            var trevorColor = service.GetColorForFactionType(FactionType.Trevor);
            var franklinColor = service.GetColorForFactionType(FactionType.Franklin);

            // Assert
            Assert.NotEqual(neutralColor, michaelColor);
            Assert.NotEqual(neutralColor, trevorColor);
            Assert.NotEqual(neutralColor, franklinColor);
        }

        #endregion

        #region GetNeutralBoundaryColor Tests

        [Fact]
        public void GetNeutralBoundaryColor_ReturnsNeutralBoundaryColor()
        {
            // Arrange
            var service = new FactionColorService();

            // Act
            var boundaryColor = service.GetNeutralBoundaryColor();

            // Assert
            Assert.Equal(BoundaryColor.Neutral, boundaryColor);
        }

        #endregion

        #region FactionColorForFactionType Lookup Tests

        [Fact]
        public void GetFactionColorForBoundaryColor_Michael_ReturnsCorrectColor()
        {
            // Arrange
            var service = new FactionColorService();

            // Act
            var color = service.GetFactionColorForBoundaryColor(BoundaryColor.Michael);
            var expectedColor = service.GetColorForFactionType(FactionType.Michael);

            // Assert
            Assert.Equal(expectedColor, color);
        }

        [Fact]
        public void GetFactionColorForBoundaryColor_Trevor_ReturnsCorrectColor()
        {
            // Arrange
            var service = new FactionColorService();

            // Act
            var color = service.GetFactionColorForBoundaryColor(BoundaryColor.Trevor);
            var expectedColor = service.GetColorForFactionType(FactionType.Trevor);

            // Assert
            Assert.Equal(expectedColor, color);
        }

        [Fact]
        public void GetFactionColorForBoundaryColor_Franklin_ReturnsCorrectColor()
        {
            // Arrange
            var service = new FactionColorService();

            // Act
            var color = service.GetFactionColorForBoundaryColor(BoundaryColor.Franklin);
            var expectedColor = service.GetColorForFactionType(FactionType.Franklin);

            // Assert
            Assert.Equal(expectedColor, color);
        }

        [Fact]
        public void GetFactionColorForBoundaryColor_Neutral_ReturnsNeutralColor()
        {
            // Arrange
            var service = new FactionColorService();

            // Act
            var color = service.GetFactionColorForBoundaryColor(BoundaryColor.Neutral);
            var expectedColor = service.GetNeutralColor();

            // Assert
            Assert.Equal(expectedColor, color);
        }

        #endregion

        #region Predefined Color Constants Tests

        [Fact]
        public void MichaelBlue_HasCorrectRGBValues()
        {
            // Arrange
            var service = new FactionColorService();

            // Act
            var color = service.GetColorForFactionType(FactionType.Michael);

            // Assert - Michael's blue should be a distinct blue (RGB: ~64, ~100, ~255)
            Assert.True(color.B >= 200, "Michael's blue should have high blue component");
            Assert.True(color.A == 255, "Color should be fully opaque");
        }

        [Fact]
        public void TrevorOrange_HasCorrectRGBValues()
        {
            // Arrange
            var service = new FactionColorService();

            // Act
            var color = service.GetColorForFactionType(FactionType.Trevor);

            // Assert - Trevor's orange should be a distinct orange (RGB: ~255, ~150, ~0)
            Assert.True(color.R >= 200, "Trevor's orange should have high red component");
            Assert.True(color.B < 100, "Trevor's orange should have low blue component");
            Assert.True(color.A == 255, "Color should be fully opaque");
        }

        [Fact]
        public void FranklinGreen_HasCorrectRGBValues()
        {
            // Arrange
            var service = new FactionColorService();

            // Act
            var color = service.GetColorForFactionType(FactionType.Franklin);

            // Assert - Franklin's green should be a distinct green (RGB: ~0, ~200, ~0)
            Assert.True(color.G >= 150, "Franklin's green should have high green component");
            Assert.True(color.A == 255, "Color should be fully opaque");
        }

        #endregion

        #region Color With Alpha Tests

        [Fact]
        public void GetColorWithAlpha_ReturnsColorWithModifiedAlpha()
        {
            // Arrange
            var service = new FactionColorService();
            var originalColor = service.GetColorForFactionType(FactionType.Michael);

            // Act
            var colorWithAlpha = service.GetColorWithAlpha(FactionType.Michael, 128);

            // Assert
            Assert.Equal(originalColor.R, colorWithAlpha.R);
            Assert.Equal(originalColor.G, colorWithAlpha.G);
            Assert.Equal(originalColor.B, colorWithAlpha.B);
            Assert.Equal(128, colorWithAlpha.A);
        }

        [Fact]
        public void GetColorWithAlpha_ClampsAlphaToValidRange()
        {
            // Arrange
            var service = new FactionColorService();

            // Act
            var colorWithHighAlpha = service.GetColorWithAlpha(FactionType.Michael, 300);
            var colorWithLowAlpha = service.GetColorWithAlpha(FactionType.Michael, -50);

            // Assert
            Assert.Equal(255, colorWithHighAlpha.A);
            Assert.Equal(0, colorWithLowAlpha.A);
        }

        #endregion

        #region TryGetFactionTypeFromColor Tests

        [Fact]
        public void TryGetFactionTypeFromColor_MatchingMichaelColor_ReturnsTrue()
        {
            // Arrange
            var service = new FactionColorService();
            var michaelColor = service.GetColorForFactionType(FactionType.Michael);

            // Act
            var result = service.TryGetFactionTypeFromColor(michaelColor, out var factionType);

            // Assert
            Assert.True(result);
            Assert.Equal(FactionType.Michael, factionType);
        }

        [Fact]
        public void TryGetFactionTypeFromColor_MatchingTrevorColor_ReturnsTrue()
        {
            // Arrange
            var service = new FactionColorService();
            var trevorColor = service.GetColorForFactionType(FactionType.Trevor);

            // Act
            var result = service.TryGetFactionTypeFromColor(trevorColor, out var factionType);

            // Assert
            Assert.True(result);
            Assert.Equal(FactionType.Trevor, factionType);
        }

        [Fact]
        public void TryGetFactionTypeFromColor_MatchingFranklinColor_ReturnsTrue()
        {
            // Arrange
            var service = new FactionColorService();
            var franklinColor = service.GetColorForFactionType(FactionType.Franklin);

            // Act
            var result = service.TryGetFactionTypeFromColor(franklinColor, out var factionType);

            // Assert
            Assert.True(result);
            Assert.Equal(FactionType.Franklin, factionType);
        }

        [Fact]
        public void TryGetFactionTypeFromColor_UnknownColor_ReturnsFalse()
        {
            // Arrange
            var service = new FactionColorService();
            var unknownColor = new FactionColor(123, 45, 67);

            // Act
            var result = service.TryGetFactionTypeFromColor(unknownColor, out var factionType);

            // Assert
            Assert.False(result);
            Assert.Equal(default(FactionType), factionType);
        }

        [Fact]
        public void TryGetFactionTypeFromColor_NeutralColor_ReturnsFalse()
        {
            // Arrange
            var service = new FactionColorService();
            var neutralColor = service.GetNeutralColor();

            // Act
            var result = service.TryGetFactionTypeFromColor(neutralColor, out var factionType);

            // Assert
            Assert.False(result, "Neutral color should not map to a faction type");
        }

        #endregion

        #region Interface Implementation Tests

        [Fact]
        public void FactionColorService_ImplementsIFactionColorService()
        {
            // Arrange & Act
            var service = new FactionColorService();

            // Assert
            Assert.IsAssignableFrom<IFactionColorService>(service);
        }

        #endregion
    }
}
