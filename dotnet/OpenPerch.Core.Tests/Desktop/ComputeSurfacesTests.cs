using System;
using System.Linq;
using NUnit.Framework;
using OpenPerch.App.Core;

namespace OpenPerch.Core.Tests.Desktop
{
    public class ComputeSurfacesTests
    {
        private const string PrimaryFloor = "Floor:0 y=1032 x=[0,1920) h=1032 z=-1";

        [Test]
        [Category("T-GEO")]
        public void FloorComesFirstOnTheWorkAreaBottom()
        {
            var fixture = GeometryTestData.Load("overlap");

            var set = DesktopGeometry.ComputeSurfaces(fixture.ToSnapshot(), fixture.client, GeometryTestData.Options());

            Assert.That(set.Items[0].Kind, Is.EqualTo(SurfaceKind.Floor));
            Assert.That(set.Items[0].Owner, Is.EqualTo(IntPtr.Zero));
            Assert.That(set.Items[0].Y, Is.EqualTo(GeometryTestData.PrimaryWork.bottom));
            Assert.That(set.Items.Count(item => item.Kind == SurfaceKind.Floor), Is.EqualTo(1));
        }

        [Test]
        [Category("T-GEO")]
        public void WindowInFrontSplitsTheSpanOfTheWindowsBehind()
        {
            var surfaces = GeometryTestData.Compute(GeometryTestData.Load("overlap"));

            Assert.That(surfaces, Is.EqualTo(new[]
            {
                PrimaryFloor,
                "WindowTop:2 y=200 x=[608,1050) h=200 z=1",
                "WindowTop:3 y=400 x=[308,600) h=400 z=2",
                "WindowTop:3 y=400 x=[1200,1350) h=400 z=2",
                "WindowTop:4 y=800 x=[108,300) h=800 z=3",
            }));
        }

        [Test]
        [Category("T-GEO")]
        public void CornerInsetTrimsBothEnds()
        {
            var fixture = GeometryTestData.Load("inset-caption");
            var snapshot = fixture.ToSnapshot();
            snapshot.Windows[1] = GeometryTestData.WithCaptionButtons(snapshot.Windows[1], new RectI(400, 301, 536, 333));

            var surfaces = GeometryTestData.Compute(snapshot, fixture.client);

            Assert.That(surfaces, Is.EqualTo(new[]
            {
                PrimaryFloor,
                "WindowTop:2 y=300 x=[108,400) h=300 z=1",
                "WindowTop:2 y=300 x=[536,892) h=300 z=1",
            }));
        }

        [Test]
        [Category("T-GEO")]
        public void CaptionFallbackRemovesTheRightmost150Dip()
        {
            var surfaces = GeometryTestData.Compute(GeometryTestData.Load("inset-caption"));

            Assert.That(surfaces, Is.EqualTo(new[]
            {
                PrimaryFloor,
                "WindowTop:2 y=300 x=[108,750) h=300 z=1",
            }));
        }

        [TestCase(756, 892, 108, 756)]
        [TestCase(108, 244, 244, 892)]
        [Category("T-GEO")]
        public void ReportedCaptionButtonsReplaceTheFallback(int captionLeft, int captionRight, int expectedX0, int expectedX1)
        {
            var fixture = GeometryTestData.Load("inset-caption");
            var snapshot = fixture.ToSnapshot();
            snapshot.Windows[1] = GeometryTestData.WithCaptionButtons(snapshot.Windows[1], new RectI(captionLeft, 301, captionRight, 333));

            var surfaces = GeometryTestData.Compute(snapshot, fixture.client);

            Assert.That(surfaces, Is.EqualTo(new[]
            {
                PrimaryFloor,
                $"WindowTop:2 y=300 x=[{expectedX0},{expectedX1}) h=300 z=1",
            }));
        }

        [TestCase(1.5f, 112, 675)]
        [TestCase(1.75f, 114, 637)]
        [Category("T-GEO")]
        public void InsetAndCaptionFallbackScaleWithDpi(float scale, int expectedX0, int expectedX1)
        {
            var surfaces = GeometryTestData.Compute(GeometryTestData.Load("inset-caption"), GeometryTestData.Options(scale));

            Assert.That(surfaces, Is.EqualTo(new[]
            {
                PrimaryFloor,
                $"WindowTop:2 y=300 x=[{expectedX0},{expectedX1}) h=300 z=1",
            }));
        }

        [Test]
        [Category("T-GEO")]
        public void HeadroomNearTheScreenTopIsTheDistanceToTheClientTop()
        {
            var surfaces = GeometryTestData.Compute(GeometryTestData.Load("headroom"));

            Assert.That(surfaces, Has.Member("WindowTop:2 y=60 x=[208,850) h=60 z=1"));
        }

        [Test]
        [Category("T-GEO")]
        public void HeadroomStopsAtTheBottomOfAWindowAboveInZ()
        {
            var surfaces = GeometryTestData.Compute(GeometryTestData.Load("headroom"));

            Assert.That(surfaces, Has.Member("WindowTop:3 y=700 x=[108,750) h=200 z=2"));
        }

        [Test]
        [Category("T-GEO")]
        public void HeadroomIgnoresWindowsBehindThePlatform()
        {
            var surfaces = GeometryTestData.Compute(GeometryTestData.Load("headroom"));

            Assert.That(surfaces, Is.EqualTo(new[]
            {
                PrimaryFloor,
                "WindowTop:2 y=60 x=[208,850) h=60 z=1",
                "WindowTop:3 y=700 x=[108,750) h=200 z=2",
                "WindowTop:4 y=600 x=[1108,1750) h=600 z=3",
                "WindowTop:5 y=200 x=[1208,1650) h=200 z=4",
            }));
        }

        [Test]
        [Category("T-GEO")]
        public void NegativeCoordinatesStayInVirtualScreenSpace()
        {
            var surfaces = GeometryTestData.Compute(GeometryTestData.Load("negative-coordinates"));

            Assert.That(surfaces, Is.EqualTo(new[]
            {
                "Floor:0 y=584 x=[-1280,0) h=984 z=-1",
                "WindowTop:3 y=-200 x=[-992,-450) h=200 z=2",
                "WindowTop:4 y=100 x=[-300,0) h=500 z=3",
            }));
        }

        [Test]
        [Category("T-GEO")]
        public void VerticalTaskbarLeavesTheFloorOnTheWorkAreaBottom()
        {
            var surfaces = GeometryTestData.Compute(GeometryTestData.Load("vertical-taskbar"));

            Assert.That(surfaces, Is.EqualTo(new[]
            {
                "Floor:0 y=1080 x=[62,1920) h=1080 z=-1",
                "WindowTop:2 y=300 x=[62,670) h=300 z=1",
                "WindowTop:3 y=500 x=[1008,1750) h=500 z=2",
            }));
        }

        [Test]
        [Category("T-GEO")]
        public void AutoHideTaskbarPutsTheFloorOnTheClientBottom()
        {
            var surfaces = GeometryTestData.Compute(GeometryTestData.Load("auto-hide"));

            Assert.That(surfaces, Is.EqualTo(new[]
            {
                "Floor:0 y=1079 x=[0,1920) h=1079 z=-1",
                "WindowTop:2 y=400 x=[308,950) h=400 z=1",
            }));
        }

        [Test]
        [Category("T-GEO")]
        public void AutoHideOnAnotherMonitorStillPutsTheFloorOnTheClientBottom()
        {
            var snapshot = new DesktopSnapshot
            {
                Monitors = new[]
                {
                    GeometryTestData.Monitor(GeometryTestData.PrimaryMonitor, GeometryTestData.PrimaryMonitor, primary: true),
                    GeometryTestData.Monitor(new RectI(1920, 0, 3840, 1080), new RectI(1920, 0, 3840, 1080)),
                },
                Taskbars = new[] { new TaskbarInfo(new RectI(0, 1078, 1920, 1126), Edge.Bottom, true) },
            };

            var surfaces = GeometryTestData.Compute(snapshot, new RectI(1920, 0, 3840, 1079));

            Assert.That(surfaces, Is.EqualTo(new[] { "Floor:0 y=1079 x=[1920,3840) h=1079 z=-1" }));
        }

        [Test]
        [Category("T-GEO")]
        public void DesktopAndShellWindowsAreNeitherPlatformsNorOccluders()
        {
            var surfaces = GeometryTestData.Compute(GeometryTestData.Load("foreground-desktop"));

            Assert.That(surfaces, Is.EqualTo(new[]
            {
                PrimaryFloor,
                "WindowTop:2 y=300 x=[408,1250) h=300 z=1",
            }));
        }

        [Test]
        [Category("T-GEO")]
        public void SelfAndIgnoredWindowsAboveDoNotCutTheSpan()
        {
            var surfaces = GeometryTestData.Compute(GeometryTestData.Load("occlusion"));

            Assert.That(surfaces, Is.EqualTo(new[]
            {
                PrimaryFloor,
                "WindowTop:5 y=500 x=[208,700) h=500 z=4",
                "WindowTop:5 y=500 x=[1000,1250) h=500 z=4",
                "WindowTop:6 y=50 x=[108,650) h=50 z=5",
            }));
        }

        [TestCase(WindowClass.Platform, true)]
        [TestCase(WindowClass.OccluderOnly, true)]
        [TestCase(WindowClass.Taskbar, true)]
        [TestCase(WindowClass.Shell, false)]
        [TestCase(WindowClass.Desktop, false)]
        [TestCase(WindowClass.Self, false)]
        [TestCase(WindowClass.Ignored, false)]
        [Category("T-GEO")]
        public void OnlyApplicationAndTaskbarWindowsAboveCutTheSpan(WindowClass aboveKind, bool cuts)
        {
            var snapshot = new DesktopSnapshot
            {
                Monitors = new[] { GeometryTestData.Monitor(GeometryTestData.PrimaryMonitor, GeometryTestData.PrimaryWork, primary: true) },
                Windows = new[]
                {
                    GeometryTestData.Window(1, aboveKind, new RectI(500, 200, 700, 600), 0),
                    GeometryTestData.Window(2, WindowClass.Platform, new RectI(100, 300, 1100, 800), 1),
                },
            };

            var set = DesktopGeometry.ComputeSurfaces(snapshot, GeometryTestData.PrimaryWork, GeometryTestData.Options());
            var lower = set.Items.Where(item => item.Owner == new IntPtr(2)).Select(item => GeometryTestData.Describe(item));

            var expected = cuts
                ? new[] { "WindowTop:2 y=300 x=[108,500) h=300 z=1", "WindowTop:2 y=300 x=[700,950) h=300 z=1" }
                : new[] { "WindowTop:2 y=300 x=[108,950) h=300 z=1" };
            Assert.That(lower, Is.EqualTo(expected));
        }

        [Test]
        [Category("T-GEO")]
        public void StackedMonitorsUseTheUpperMonitorWhenTheClientIsThere()
        {
            var surfaces = GeometryTestData.Compute(GeometryTestData.Load("stacked-monitors"), GeometryTestData.Options(1.5f));

            Assert.That(surfaces, Is.EqualTo(new[]
            {
                "Floor:0 y=0 x=[0,2560) h=1440 z=-1",
                "WindowTop:3 y=-1000 x=[512,1475) h=440 z=2",
                "WindowTop:4 y=-300 x=[1812,2175) h=1140 z=3",
            }));
        }

        [Test]
        [Category("T-GEO")]
        public void StackedMonitorsUseTheLowerMonitorWhenTheClientIsThere()
        {
            var fixture = GeometryTestData.Load("stacked-monitors");

            var surfaces = GeometryTestData.Compute(fixture.ToSnapshot(), GeometryTestData.PrimaryWork);

            Assert.That(surfaces, Is.EqualTo(new[]
            {
                PrimaryFloor,
                "WindowTop:2 y=100 x=[108,750) h=100 z=1",
            }));
        }

        [Test]
        [Category("T-GEO")]
        public void MaximizedWindowCoversEverySurfaceBelowIt()
        {
            var surfaces = GeometryTestData.Compute(GeometryTestData.Load("maximized"));

            Assert.That(surfaces, Is.EqualTo(new[] { PrimaryFloor }));
        }

        [Test]
        [Category("T-GEO")]
        public void MaximizedWindowIsNeverAPlatform()
        {
            var fixture = GeometryTestData.Load("inset-caption");
            var snapshot = fixture.ToSnapshot();
            snapshot.Windows[1] = GeometryTestData.WithMaximized(snapshot.Windows[1], true);

            var surfaces = GeometryTestData.Compute(snapshot, fixture.client);

            Assert.That(surfaces, Is.EqualTo(new[] { PrimaryFloor }));
        }

        [Test]
        [Category("T-GEO")]
        public void NarrowWindowsAndShortSpansAreDropped()
        {
            var surfaces = GeometryTestData.Compute(GeometryTestData.Load("narrow-windows"));

            Assert.That(surfaces, Is.EqualTo(new[]
            {
                PrimaryFloor,
                "WindowTop:4 y=300 x=[408,450) h=300 z=3",
                "WindowTop:6 y=300 x=[1108,1250) h=300 z=5",
            }));
        }

        [TestCase(42, true)]
        [TestCase(43, false)]
        [Category("T-GEO")]
        public void SpanAsWideAsTheFootIsKept(int footWidthPx, bool kept)
        {
            var surfaces = GeometryTestData.Compute(GeometryTestData.Load("narrow-windows"), GeometryTestData.Options(footWidthPx: footWidthPx));

            Assert.That(surfaces.Contains("WindowTop:4 y=300 x=[408,450) h=300 z=3"), Is.EqualTo(kept));
            Assert.That(surfaces, Has.Member("WindowTop:6 y=300 x=[1108,1250) h=300 z=5"));
        }

        [Test]
        [Category("T-GEO")]
        public void WindowsOutsideTheClientAreClippedOrSkipped()
        {
            var surfaces = GeometryTestData.Compute(GeometryTestData.Load("outside-client"));

            Assert.That(surfaces, Is.EqualTo(new[]
            {
                PrimaryFloor,
                "WindowTop:5 y=500 x=[1708,1920) h=500 z=4",
                "WindowTop:6 y=600 x=[0,350) h=200 z=5",
            }));
        }

        [Test]
        [Category("T-GEO")]
        public void MultipleTaskbarsUseTheWorkAreaOfTheClientMonitor()
        {
            var surfaces = GeometryTestData.Compute(GeometryTestData.Load("multiple-taskbars"));

            Assert.That(surfaces, Is.EqualTo(new[]
            {
                "Floor:0 y=824 x=[1920,3200) h=984 z=-1",
                "WindowTop:3 y=-100 x=[2008,2650) h=60 z=2",
                "WindowTop:4 y=300 x=[1920,2000) h=460 z=3",
            }));
        }

        [Test]
        [Category("T-GEO")]
        public void MultipleTaskbarsOnThePrimaryMonitorIgnoreTheSecondary()
        {
            var fixture = GeometryTestData.Load("multiple-taskbars");

            var surfaces = GeometryTestData.Compute(fixture.ToSnapshot(), GeometryTestData.PrimaryWork);

            Assert.That(surfaces, Is.EqualTo(new[]
            {
                PrimaryFloor,
                "WindowTop:4 y=300 x=[1508,1920) h=300 z=3",
            }));
        }

        [Test]
        [Category("T-GEO")]
        public void SeqIsCopiedFromTheSnapshot()
        {
            var fixture = GeometryTestData.Load("overlap");
            var snapshot = fixture.ToSnapshot();
            snapshot.Seq = 42;

            var set = DesktopGeometry.ComputeSurfaces(snapshot, fixture.client, GeometryTestData.Options());

            Assert.That(set.Seq, Is.EqualTo(42));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [Category("T-GEO")]
        public void NonPositiveScaleIsRejected(float scale)
        {
            var fixture = GeometryTestData.Load("overlap");

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                DesktopGeometry.ComputeSurfaces(fixture.ToSnapshot(), fixture.client, GeometryTestData.Options(scale)));
        }
    }
}
