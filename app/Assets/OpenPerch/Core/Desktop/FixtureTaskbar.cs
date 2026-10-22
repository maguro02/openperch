using System;

namespace OpenPerch.App.Core
{
    /// <summary>Persisted form of a <see cref="TaskbarInfo"/> inside a <see cref="GeometryFixture"/>.</summary>
    [Serializable]
    public sealed class FixtureTaskbar
    {
        public RectI bounds;
        public Edge edge = Edge.Bottom;
        public bool autoHide;
    }
}
