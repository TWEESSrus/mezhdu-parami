using NUnit.Framework;
using UnityEngine;

namespace MezhduParami.Tests
{
    public sealed class CampusMinimapTests
    {
        private static readonly Rect Campus = new Rect(-20.48f, -20.48f, 40.96f, 40.96f);
        private static readonly Rect Map = new Rect(-100f, -100f, 200f, 200f);

        [TestCase(-20.48f, -20.48f, -100f, -100f)]
        [TestCase(-20.48f, 20.48f, -100f, 100f)]
        [TestCase(20.48f, -20.48f, 100f, -100f)]
        [TestCase(20.48f, 20.48f, 100f, 100f)]
        [TestCase(0f, 0f, 0f, 0f)]
        public void CampusCornersAndCenterKeepNorthUp(float x, float y, float expectedX, float expectedY)
        {
            var point = CampusMinimap.WorldToMapPosition(new Vector2(x, y), Campus, Map);
            Assert.That(point.x, Is.EqualTo(expectedX).Within(0.001f));
            Assert.That(point.y, Is.EqualTo(expectedY).Within(0.001f));
        }

        [Test] public void PositionOutsideCampusStaysOnMapEdge()
        {
            var point = CampusMinimap.WorldToMapPosition(new Vector2(100f, -100f), Campus, Map);
            Assert.That(point, Is.EqualTo(new Vector2(100f, -100f)));
        }

        [Test] public void TopLeftPivotAndDifferentAspectRatioPreservePosition()
        {
            var topLeftMap = new Rect(0f, -120f, 240f, 120f);
            var point = CampusMinimap.WorldToMapPosition(new Vector2(10.24f, -10.24f), Campus, topLeftMap);
            Assert.That(point.x, Is.EqualTo(180f).Within(0.001f));
            Assert.That(point.y, Is.EqualTo(-90f).Within(0.001f));
        }

        [Test] public void OffsetWorldUsesItsOwnBounds()
        {
            var shiftedWorld = new Rect(100f, 200f, 40f, 80f);
            var point = CampusMinimap.WorldToMapPosition(new Vector2(110f, 260f), shiftedWorld, Map);
            Assert.That(point, Is.EqualTo(new Vector2(-50f, 50f)));
        }

        [Test] public void InvalidWorldBoundsDoNotProduceNonFiniteMarkerCoordinates()
        {
            var point = CampusMinimap.WorldToMapPosition(Vector2.one, new Rect(10f, 20f, 0f, 0f), new Rect(0f, -100f, 200f, 100f));
            Assert.That(point, Is.EqualTo(new Vector2(100f, -50f)));
        }
    }
}
