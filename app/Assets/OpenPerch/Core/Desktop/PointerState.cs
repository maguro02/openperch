namespace OpenPerch.App.Core
{
    public struct PointerState
    {
        public Vec2 PosClientPx;
        public Vec2 VelocityPxPerSec;
        public bool InsideClient;
        public bool LeftHeld;
        public bool LeftDown;
        public bool LeftUp;
        public bool RightDown;
        public bool RightUp;
        public bool AltHeld;
        public bool EscDown;
        public int ClickCount;
    }
}
