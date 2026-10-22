using System;

namespace OpenPerch.App.Core
{
    /// <summary>
    /// A window inside a <see cref="GeometryFixture"/>, reduced to its geometry so no window or process can be identified.
    /// </summary>
    [Serializable]
    public sealed class FixtureWindow
    {
        /// <summary>Sequential number standing in for the window handle; 0 is never used because it means "no window".</summary>
        public int id;

        /// <summary>Sequential number standing in for the process ID; windows of the same process share it.</summary>
        public int pid;

        public RectI rect;
        public WindowClass kind;

        /// <summary>Position in the Z order; 0 is the topmost window.</summary>
        public int z;

        public bool maximized;
    }
}
