using System;

namespace OpenPerch.App.Core
{
    public readonly struct Surface
    {
        public readonly SurfaceKind Kind;
        public readonly IntPtr Owner;
        public readonly int Y;
        public readonly int X0;

        /// <summary>Exclusive end of the walkable span.</summary>
        public readonly int X1;

        public readonly int Headroom;
        public readonly int Z;

        public Surface(SurfaceKind kind, IntPtr owner, int y, int x0, int x1, int headroom, int z)
        {
            Kind = kind;
            Owner = owner;
            Y = y;
            X0 = x0;
            X1 = x1;
            Headroom = headroom;
            Z = z;
        }
    }
}
