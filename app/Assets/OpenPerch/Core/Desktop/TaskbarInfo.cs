namespace OpenPerch.App.Core
{
    public readonly struct TaskbarInfo
    {
        public readonly RectI Bounds;
        public readonly Edge Edge;
        public readonly bool AutoHide;

        public TaskbarInfo(RectI bounds, Edge edge, bool autoHide)
        {
            Bounds = bounds;
            Edge = edge;
            AutoHide = autoHide;
        }
    }
}
