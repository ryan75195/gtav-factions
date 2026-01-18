using FactionWars.Core.Interfaces;
using FactionWars.Territory.Models;
using System;
using System.Collections.Generic;
using Xunit;

namespace FactionWars.Tests.Unit.Territory
{
    public class ZoneBoundaryTests
    {
        #region Circular Boundary Tests

        [Fact]
        public void CircularBoundary_ShouldContainPointAtCenter()
        {
            // Arrange
            var center = new Vector3(100f, 100f, 0f);
            var boundary = ZoneBoundary.CreateCircular(center, 50f);

            // Act
            var result = boundary.Contains(center);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CircularBoundary_ShouldContainPointInsideRadius()
        {
            // Arrange
            var center = new Vector3(100f, 100f, 0f);
            var boundary = ZoneBoundary.CreateCircular(center, 50f);
            var insidePoint = new Vector3(120f, 110f, 0f); // ~22 units from center

            // Act
            var result = boundary.Contains(insidePoint);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CircularBoundary_ShouldNotContainPointOutsideRadius()
        {
            // Arrange
            var center = new Vector3(100f, 100f, 0f);
            var boundary = ZoneBoundary.CreateCircular(center, 50f);
            var outsidePoint = new Vector3(200f, 200f, 0f); // ~141 units from center

            // Act
            var result = boundary.Contains(outsidePoint);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CircularBoundary_ShouldContainPointExactlyOnBoundary()
        {
            // Arrange
            var center = new Vector3(0f, 0f, 0f);
            var boundary = ZoneBoundary.CreateCircular(center, 50f);
            var edgePoint = new Vector3(50f, 0f, 0f); // Exactly 50 units from center

            // Act
            var result = boundary.Contains(edgePoint);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CircularBoundary_ShouldIgnoreZCoordinateBy2D()
        {
            // Arrange - GTA V zones should use 2D containment (X, Y only)
            var center = new Vector3(100f, 100f, 0f);
            var boundary = ZoneBoundary.CreateCircular(center, 50f);
            var pointAbove = new Vector3(100f, 100f, 500f); // Same X,Y but very different Z

            // Act
            var result = boundary.Contains(pointAbove);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CircularBoundary_ShouldThrowOnNegativeRadius()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ZoneBoundary.CreateCircular(Vector3.Zero, -10f));
        }

        [Fact]
        public void CircularBoundary_ShouldThrowOnZeroRadius()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ZoneBoundary.CreateCircular(Vector3.Zero, 0f));
        }

        [Fact]
        public void CircularBoundary_ShouldExposeCenter()
        {
            // Arrange
            var center = new Vector3(50f, 75f, 10f);
            var boundary = ZoneBoundary.CreateCircular(center, 100f);

            // Act & Assert
            Assert.Equal(center, boundary.Center);
        }

        [Fact]
        public void CircularBoundary_ShouldExposeBoundingRadius()
        {
            // Arrange
            var boundary = ZoneBoundary.CreateCircular(Vector3.Zero, 75f);

            // Act & Assert
            Assert.Equal(75f, boundary.BoundingRadius);
        }

        #endregion

        #region Polygon Boundary Tests

        [Fact]
        public void PolygonBoundary_ShouldContainPointInsideRectangle()
        {
            // Arrange - Create a simple square boundary
            var vertices = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(100f, 0f, 0f),
                new Vector3(100f, 100f, 0f),
                new Vector3(0f, 100f, 0f)
            };
            var boundary = ZoneBoundary.CreatePolygon(vertices);
            var insidePoint = new Vector3(50f, 50f, 0f);

            // Act
            var result = boundary.Contains(insidePoint);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void PolygonBoundary_ShouldNotContainPointOutsideRectangle()
        {
            // Arrange
            var vertices = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(100f, 0f, 0f),
                new Vector3(100f, 100f, 0f),
                new Vector3(0f, 100f, 0f)
            };
            var boundary = ZoneBoundary.CreatePolygon(vertices);
            var outsidePoint = new Vector3(150f, 50f, 0f);

            // Act
            var result = boundary.Contains(outsidePoint);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void PolygonBoundary_ShouldContainPointInsideTriangle()
        {
            // Arrange - Create a triangle
            var vertices = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(100f, 0f, 0f),
                new Vector3(50f, 100f, 0f)
            };
            var boundary = ZoneBoundary.CreatePolygon(vertices);
            var insidePoint = new Vector3(50f, 30f, 0f);

            // Act
            var result = boundary.Contains(insidePoint);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void PolygonBoundary_ShouldNotContainPointOutsideTriangle()
        {
            // Arrange
            var vertices = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(100f, 0f, 0f),
                new Vector3(50f, 100f, 0f)
            };
            var boundary = ZoneBoundary.CreatePolygon(vertices);
            var outsidePoint = new Vector3(10f, 90f, 0f); // Outside the triangle

            // Act
            var result = boundary.Contains(outsidePoint);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void PolygonBoundary_ShouldContainPointOnVertex()
        {
            // Arrange
            var vertices = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(100f, 0f, 0f),
                new Vector3(100f, 100f, 0f),
                new Vector3(0f, 100f, 0f)
            };
            var boundary = ZoneBoundary.CreatePolygon(vertices);
            var vertexPoint = new Vector3(0f, 0f, 0f);

            // Act
            var result = boundary.Contains(vertexPoint);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void PolygonBoundary_ShouldContainPointOnEdge()
        {
            // Arrange
            var vertices = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(100f, 0f, 0f),
                new Vector3(100f, 100f, 0f),
                new Vector3(0f, 100f, 0f)
            };
            var boundary = ZoneBoundary.CreatePolygon(vertices);
            var edgePoint = new Vector3(50f, 0f, 0f); // On bottom edge

            // Act
            var result = boundary.Contains(edgePoint);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void PolygonBoundary_ShouldIgnoreZCoordinateBy2D()
        {
            // Arrange
            var vertices = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(100f, 0f, 0f),
                new Vector3(100f, 100f, 0f),
                new Vector3(0f, 100f, 0f)
            };
            var boundary = ZoneBoundary.CreatePolygon(vertices);
            var pointAbove = new Vector3(50f, 50f, 500f);

            // Act
            var result = boundary.Contains(pointAbove);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void PolygonBoundary_ShouldThrowOnLessThanThreeVertices()
        {
            // Arrange
            var vertices = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(100f, 0f, 0f)
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => ZoneBoundary.CreatePolygon(vertices));
        }

        [Fact]
        public void PolygonBoundary_ShouldThrowOnNullVertices()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => ZoneBoundary.CreatePolygon(null!));
        }

        [Fact]
        public void PolygonBoundary_ShouldCalculateCenterFromVertices()
        {
            // Arrange - Square centered at (50, 50)
            var vertices = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(100f, 0f, 0f),
                new Vector3(100f, 100f, 0f),
                new Vector3(0f, 100f, 0f)
            };
            var boundary = ZoneBoundary.CreatePolygon(vertices);

            // Act & Assert - Center should be centroid of vertices
            Assert.Equal(50f, boundary.Center.X);
            Assert.Equal(50f, boundary.Center.Y);
        }

        [Fact]
        public void PolygonBoundary_ShouldCalculateBoundingRadius()
        {
            // Arrange - Square from (0,0) to (100,100), center at (50, 50)
            var vertices = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(100f, 0f, 0f),
                new Vector3(100f, 100f, 0f),
                new Vector3(0f, 100f, 0f)
            };
            var boundary = ZoneBoundary.CreatePolygon(vertices);

            // Act & Assert - Bounding radius should be distance from center to farthest vertex
            // Distance from (50, 50) to corner (0, 0) = sqrt(50^2 + 50^2) ≈ 70.71
            var expectedRadius = (float)Math.Sqrt(50 * 50 + 50 * 50);
            Assert.Equal(expectedRadius, boundary.BoundingRadius, precision: 2);
        }

        [Fact]
        public void PolygonBoundary_ShouldExposeVertices()
        {
            // Arrange
            var vertices = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(100f, 0f, 0f),
                new Vector3(50f, 100f, 0f)
            };
            var boundary = ZoneBoundary.CreatePolygon(vertices);

            // Act
            var exposedVertices = boundary.Vertices;

            // Assert
            Assert.Equal(3, exposedVertices.Count);
            Assert.Equal(vertices[0], exposedVertices[0]);
            Assert.Equal(vertices[1], exposedVertices[1]);
            Assert.Equal(vertices[2], exposedVertices[2]);
        }

        #endregion

        #region Boundary Type Tests

        [Fact]
        public void CircularBoundary_ShouldHaveCircularType()
        {
            // Arrange & Act
            var boundary = ZoneBoundary.CreateCircular(Vector3.Zero, 50f);

            // Assert
            Assert.Equal(BoundaryType.Circular, boundary.Type);
        }

        [Fact]
        public void PolygonBoundary_ShouldHavePolygonType()
        {
            // Arrange
            var vertices = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(100f, 0f, 0f),
                new Vector3(50f, 100f, 0f)
            };

            // Act
            var boundary = ZoneBoundary.CreatePolygon(vertices);

            // Assert
            Assert.Equal(BoundaryType.Polygon, boundary.Type);
        }

        #endregion

        #region Complex Polygon Tests

        [Fact]
        public void PolygonBoundary_ShouldHandleConcavePolygon()
        {
            // Arrange - L-shaped polygon (concave)
            var vertices = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(100f, 0f, 0f),
                new Vector3(100f, 50f, 0f),
                new Vector3(50f, 50f, 0f),
                new Vector3(50f, 100f, 0f),
                new Vector3(0f, 100f, 0f)
            };
            var boundary = ZoneBoundary.CreatePolygon(vertices);

            // Points to test
            var insideL = new Vector3(25f, 25f, 0f); // Inside the L
            var outsideConcave = new Vector3(75f, 75f, 0f); // In the concave cutout

            // Act & Assert
            Assert.True(boundary.Contains(insideL));
            Assert.False(boundary.Contains(outsideConcave));
        }

        [Fact]
        public void PolygonBoundary_ShouldHandleIrregularPolygon()
        {
            // Arrange - Pentagon shape
            var vertices = new List<Vector3>
            {
                new Vector3(50f, 0f, 0f),
                new Vector3(100f, 35f, 0f),
                new Vector3(80f, 100f, 0f),
                new Vector3(20f, 100f, 0f),
                new Vector3(0f, 35f, 0f)
            };
            var boundary = ZoneBoundary.CreatePolygon(vertices);
            var centerPoint = new Vector3(50f, 50f, 0f);

            // Act & Assert
            Assert.True(boundary.Contains(centerPoint));
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void CircularBoundary_ShouldHandleVerySmallRadius()
        {
            // Arrange
            var boundary = ZoneBoundary.CreateCircular(Vector3.Zero, 0.001f);
            var nearPoint = new Vector3(0.0005f, 0.0005f, 0f);

            // Act & Assert
            Assert.True(boundary.Contains(nearPoint));
        }

        [Fact]
        public void CircularBoundary_ShouldHandleVeryLargeRadius()
        {
            // Arrange - GTA V map is roughly 8000 units
            var boundary = ZoneBoundary.CreateCircular(Vector3.Zero, 10000f);
            var farPoint = new Vector3(5000f, 5000f, 0f);

            // Act & Assert
            Assert.True(boundary.Contains(farPoint));
        }

        [Fact]
        public void CircularBoundary_ShouldHandleNegativeCoordinates()
        {
            // Arrange
            var center = new Vector3(-500f, -300f, 0f);
            var boundary = ZoneBoundary.CreateCircular(center, 100f);
            var insidePoint = new Vector3(-450f, -280f, 0f);

            // Act & Assert
            Assert.True(boundary.Contains(insidePoint));
        }

        [Fact]
        public void PolygonBoundary_ShouldHandleNegativeCoordinates()
        {
            // Arrange
            var vertices = new List<Vector3>
            {
                new Vector3(-100f, -100f, 0f),
                new Vector3(0f, -100f, 0f),
                new Vector3(0f, 0f, 0f),
                new Vector3(-100f, 0f, 0f)
            };
            var boundary = ZoneBoundary.CreatePolygon(vertices);
            var insidePoint = new Vector3(-50f, -50f, 0f);

            // Act & Assert
            Assert.True(boundary.Contains(insidePoint));
        }

        #endregion
    }
}
