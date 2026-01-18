using FactionWars.Factions.Models;
using System;
using Xunit;

namespace FactionWars.Tests.Unit.Factions
{
    public class FactionColorTests
    {
        #region Constructor and Properties

        [Fact]
        public void FactionColor_ShouldStoreRgbValues()
        {
            // Arrange & Act
            var color = new FactionColor(100, 150, 200);

            // Assert
            Assert.Equal(100, color.R);
            Assert.Equal(150, color.G);
            Assert.Equal(200, color.B);
        }

        [Fact]
        public void FactionColor_ShouldHaveDefaultAlphaOf255()
        {
            // Arrange & Act
            var color = new FactionColor(100, 150, 200);

            // Assert
            Assert.Equal(255, color.A);
        }

        [Fact]
        public void FactionColor_ShouldAllowCustomAlpha()
        {
            // Arrange & Act
            var color = new FactionColor(100, 150, 200, 128);

            // Assert
            Assert.Equal(128, color.A);
        }

        [Fact]
        public void FactionColor_ShouldClampRToValidRange()
        {
            // Arrange & Act
            var colorMax = new FactionColor(300, 150, 200);
            var colorMin = new FactionColor(-50, 150, 200);

            // Assert
            Assert.Equal(255, colorMax.R);
            Assert.Equal(0, colorMin.R);
        }

        [Fact]
        public void FactionColor_ShouldClampGToValidRange()
        {
            // Arrange & Act
            var colorMax = new FactionColor(100, 300, 200);
            var colorMin = new FactionColor(100, -50, 200);

            // Assert
            Assert.Equal(255, colorMax.G);
            Assert.Equal(0, colorMin.G);
        }

        [Fact]
        public void FactionColor_ShouldClampBToValidRange()
        {
            // Arrange & Act
            var colorMax = new FactionColor(100, 150, 300);
            var colorMin = new FactionColor(100, 150, -50);

            // Assert
            Assert.Equal(255, colorMax.B);
            Assert.Equal(0, colorMin.B);
        }

        [Fact]
        public void FactionColor_ShouldClampAToValidRange()
        {
            // Arrange & Act
            var colorMax = new FactionColor(100, 150, 200, 300);
            var colorMin = new FactionColor(100, 150, 200, -50);

            // Assert
            Assert.Equal(255, colorMax.A);
            Assert.Equal(0, colorMin.A);
        }

        #endregion

        #region Predefined Colors

        [Fact]
        public void FactionColor_White_ShouldBe255AllChannels()
        {
            // Arrange & Act
            var white = FactionColor.White;

            // Assert
            Assert.Equal(255, white.R);
            Assert.Equal(255, white.G);
            Assert.Equal(255, white.B);
            Assert.Equal(255, white.A);
        }

        [Fact]
        public void FactionColor_Black_ShouldBe0AllColorChannels()
        {
            // Arrange & Act
            var black = FactionColor.Black;

            // Assert
            Assert.Equal(0, black.R);
            Assert.Equal(0, black.G);
            Assert.Equal(0, black.B);
            Assert.Equal(255, black.A);
        }

        [Fact]
        public void FactionColor_Red_ShouldBeCorrect()
        {
            // Arrange & Act
            var red = FactionColor.Red;

            // Assert
            Assert.Equal(255, red.R);
            Assert.Equal(0, red.G);
            Assert.Equal(0, red.B);
        }

        [Fact]
        public void FactionColor_Green_ShouldBeCorrect()
        {
            // Arrange & Act
            var green = FactionColor.Green;

            // Assert
            Assert.Equal(0, green.R);
            Assert.Equal(255, green.G);
            Assert.Equal(0, green.B);
        }

        [Fact]
        public void FactionColor_Blue_ShouldBeCorrect()
        {
            // Arrange & Act
            var blue = FactionColor.Blue;

            // Assert
            Assert.Equal(0, blue.R);
            Assert.Equal(0, blue.G);
            Assert.Equal(255, blue.B);
        }

        #endregion

        #region Equality

        [Fact]
        public void FactionColor_ShouldBeEqualWhenSameValues()
        {
            // Arrange
            var color1 = new FactionColor(100, 150, 200);
            var color2 = new FactionColor(100, 150, 200);

            // Act & Assert
            Assert.Equal(color1, color2);
        }

        [Fact]
        public void FactionColor_ShouldNotBeEqualWhenDifferentR()
        {
            // Arrange
            var color1 = new FactionColor(100, 150, 200);
            var color2 = new FactionColor(101, 150, 200);

            // Act & Assert
            Assert.NotEqual(color1, color2);
        }

        [Fact]
        public void FactionColor_ShouldNotBeEqualWhenDifferentG()
        {
            // Arrange
            var color1 = new FactionColor(100, 150, 200);
            var color2 = new FactionColor(100, 151, 200);

            // Act & Assert
            Assert.NotEqual(color1, color2);
        }

        [Fact]
        public void FactionColor_ShouldNotBeEqualWhenDifferentB()
        {
            // Arrange
            var color1 = new FactionColor(100, 150, 200);
            var color2 = new FactionColor(100, 150, 201);

            // Act & Assert
            Assert.NotEqual(color1, color2);
        }

        [Fact]
        public void FactionColor_ShouldNotBeEqualWhenDifferentA()
        {
            // Arrange
            var color1 = new FactionColor(100, 150, 200, 255);
            var color2 = new FactionColor(100, 150, 200, 128);

            // Act & Assert
            Assert.NotEqual(color1, color2);
        }

        [Fact]
        public void FactionColor_GetHashCode_ShouldBeConsistentWithEquals()
        {
            // Arrange
            var color1 = new FactionColor(100, 150, 200);
            var color2 = new FactionColor(100, 150, 200);

            // Act & Assert
            Assert.Equal(color1.GetHashCode(), color2.GetHashCode());
        }

        [Fact]
        public void FactionColor_EqualityOperator_ShouldWork()
        {
            // Arrange
            var color1 = new FactionColor(100, 150, 200);
            var color2 = new FactionColor(100, 150, 200);

            // Act & Assert
            Assert.True(color1 == color2);
        }

        [Fact]
        public void FactionColor_InequalityOperator_ShouldWork()
        {
            // Arrange
            var color1 = new FactionColor(100, 150, 200);
            var color2 = new FactionColor(200, 150, 100);

            // Act & Assert
            Assert.True(color1 != color2);
        }

        #endregion

        #region ToString

        [Fact]
        public void FactionColor_ToString_ShouldReturnRgbaFormat()
        {
            // Arrange
            var color = new FactionColor(100, 150, 200, 128);

            // Act
            var result = color.ToString();

            // Assert
            Assert.Contains("100", result);
            Assert.Contains("150", result);
            Assert.Contains("200", result);
            Assert.Contains("128", result);
        }

        #endregion
    }
}
