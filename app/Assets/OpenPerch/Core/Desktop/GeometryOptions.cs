namespace OpenPerch.App.Core
{
    public sealed class GeometryOptions
    {
        /// <summary>DPI scale of the target monitor (DPI / 96). Values suffixed "Dip" are multiplied by it.</summary>
        public float Scale = 1f;

        public int FootWidthPx;
        public int MinPlatformWidthDip = 200;
        public int CornerInsetDip = 8;
        public int CaptionFallbackDip = 150;
    }
}
