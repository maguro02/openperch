using NUnit.Framework;
using OpenPerch.App.Core;

namespace OpenPerch.Core.Tests.Primitives
{
    public class RectITests
    {
        [Test]
        public void RightAndBottomAreExclusive()
        {
            var r = new RectI(0, 0, 10, 10);

            Assert.That(r.Contains(9, 9), Is.True);
            Assert.That(r.Contains(10, 9), Is.False);
            Assert.That(r.Contains(9, 10), Is.False);
            Assert.That(r.Width(), Is.EqualTo(10));
        }

        [Test]
        public void AdjacentRectanglesDoNotIntersect()
        {
            var a = new RectI(0, 0, 10, 10);
            var b = new RectI(10, 0, 20, 10);

            Assert.That(RectI.Intersect(a, b).IsEmpty(), Is.True);
            Assert.That(RectI.Intersect(a, new RectI(5, 5, 15, 15)), Is.EqualTo(new RectI(5, 5, 10, 10)));
        }
    }
}
