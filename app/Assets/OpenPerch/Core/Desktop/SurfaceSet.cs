using System;

namespace OpenPerch.App.Core
{
    /// <summary>
    /// Surfaces of one desktop snapshot. <c>Items[0]</c> is always the floor, with <see cref="Surface.Owner"/> set to
    /// <see cref="IntPtr.Zero"/> and <see cref="Surface.Z"/> set to -1.
    /// </summary>
    public sealed class SurfaceSet
    {
        public long Seq;
        public Surface[] Items = Array.Empty<Surface>();
    }
}
