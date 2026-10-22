namespace OpenPerch.App.Core
{
    /// <summary>
    /// Index into a <see cref="SurfaceSet"/>; only meaningful while <see cref="Seq"/> matches the set it came from.
    /// </summary>
    public readonly struct SurfaceRef
    {
        public static readonly SurfaceRef None = new SurfaceRef(-1, 0);

        public readonly int Index;
        public readonly long Seq;

        public SurfaceRef(int index, long seq)
        {
            Index = index;
            Seq = seq;
        }

        public bool IsNone => Index < 0;
    }
}
