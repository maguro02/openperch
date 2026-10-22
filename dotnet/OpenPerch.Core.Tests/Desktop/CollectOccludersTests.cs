using System;
using System.Linq;
using NUnit.Framework;
using OpenPerch.App.Core;

namespace OpenPerch.Core.Tests.Desktop
{
    public class CollectOccludersTests
    {
        [Test]
        [Category("T-GEO")]
        public void OnlyOccludingWindowsAboveTheStandingWindowAreCollected()
        {
            var fixture = GeometryTestData.Load("occlusion");
            var snapshot = fixture.ToSnapshot();
            var standing = SurfaceOf(snapshot, fixture.client, 5);
            var buffer = new RectI[DesktopGeometry.MaxOccluders];

            int count = DesktopGeometry.CollectOccluders(snapshot, standing, new RectI(550, 200, 750, 500), buffer);

            Assert.That(count, Is.EqualTo(1));
            Assert.That(buffer[0], Is.EqualTo(new RectI(700, 200, 750, 500)));
        }

        [Test]
        [Category("T-GEO")]
        public void RectanglesAreClippedToTheAvatarTopmostFirst()
        {
            var fixture = GeometryTestData.Load("occlusion");
            var snapshot = fixture.ToSnapshot();
            var standing = SurfaceOf(snapshot, fixture.client, 6);
            var buffer = new RectI[DesktopGeometry.MaxOccluders];

            int count = DesktopGeometry.CollectOccluders(snapshot, standing, new RectI(400, 0, 900, 700), buffer);

            Assert.That(count, Is.EqualTo(2));
            Assert.That(buffer.Take(count), Is.EqualTo(new[] { new RectI(700, 200, 900, 600), new RectI(400, 500, 900, 700) }));
        }

        [Test]
        [Category("T-GEO")]
        public void FloorHasNoOccluders()
        {
            var fixture = GeometryTestData.Load("occlusion");
            var snapshot = fixture.ToSnapshot();
            var floor = DesktopGeometry.ComputeSurfaces(snapshot, fixture.client, GeometryTestData.Options()).Items[0];
            var buffer = new RectI[DesktopGeometry.MaxOccluders];

            int count = DesktopGeometry.CollectOccluders(snapshot, floor, new RectI(0, 0, 1920, 1032), buffer);

            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        [Category("T-GEO")]
        public void NoSurfaceMeansNoOccluders()
        {
            var fixture = GeometryTestData.Load("occlusion");

            int count = DesktopGeometry.CollectOccluders(fixture.ToSnapshot(), default, new RectI(0, 0, 1920, 1032), new RectI[DesktopGeometry.MaxOccluders]);

            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        [Category("T-GEO")]
        public void CountGoesPastTheCapWhileWritesStopAtTheCap()
        {
            var buffer = new RectI[32];

            int count = DesktopGeometry.CollectOccluders(TwentyOccluders(), StandingBelowThem(), new RectI(0, 0, 1000, 600), buffer);

            Assert.That(count, Is.EqualTo(20));
            Assert.That(count, Is.GreaterThan(DesktopGeometry.MaxOccluders));
            Assert.That(buffer[DesktopGeometry.MaxOccluders - 1], Is.EqualTo(new RectI(150, 0, 250, 100)));
            Assert.That(buffer.Skip(DesktopGeometry.MaxOccluders), Is.All.EqualTo(default(RectI)));
        }

        [Test]
        [Category("T-GEO")]
        public void ShortDestinationIsFilledWithoutOverrun()
        {
            var buffer = new RectI[4];

            int count = DesktopGeometry.CollectOccluders(TwentyOccluders(), StandingBelowThem(), new RectI(0, 0, 1000, 600), buffer);

            Assert.That(count, Is.EqualTo(20));
            Assert.That(buffer, Is.EqualTo(new[]
            {
                new RectI(0, 0, 100, 100),
                new RectI(10, 0, 110, 100),
                new RectI(20, 0, 120, 100),
                new RectI(30, 0, 130, 100),
            }));
        }

        private static Surface SurfaceOf(DesktopSnapshot snapshot, RectI client, int owner)
        {
            var set = DesktopGeometry.ComputeSurfaces(snapshot, client, GeometryTestData.Options());
            return set.Items.First(item => item.Owner == new IntPtr(owner));
        }

        private static DesktopSnapshot TwentyOccluders()
        {
            var windows = new WindowInfo[21];
            for (int i = 0; i < 20; i++)
            {
                windows[i] = GeometryTestData.Window(i + 1, WindowClass.OccluderOnly, new RectI(i * 10, 0, i * 10 + 100, 100), i);
            }
            windows[20] = GeometryTestData.Window(21, WindowClass.Platform, new RectI(0, 500, 1000, 900), 20);
            return new DesktopSnapshot { Windows = windows };
        }

        private static Surface StandingBelowThem() => new Surface(SurfaceKind.WindowTop, new IntPtr(21), 500, 8, 850, 500, 20);
    }
}
