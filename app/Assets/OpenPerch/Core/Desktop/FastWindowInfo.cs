namespace OpenPerch.App.Core
{
    public readonly struct FastWindowInfo
    {
        public readonly bool Exists;
        public readonly bool Iconic;
        public readonly bool Zoomed;
        public readonly bool Cloaked;
        public readonly uint Pid;
        public readonly RectI Bounds;
        public readonly RectI CaptionButtons;

        public FastWindowInfo(bool exists, bool iconic, bool zoomed, bool cloaked, uint pid, RectI bounds, RectI captionButtons)
        {
            Exists = exists;
            Iconic = iconic;
            Zoomed = zoomed;
            Cloaked = cloaked;
            Pid = pid;
            Bounds = bounds;
            CaptionButtons = captionButtons;
        }
    }
}
