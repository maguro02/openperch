using System;

namespace OpenPerch.App.Core
{
    [Flags]
    public enum MotionAvailability
    {
        None = 0,
        Idle = 1,
        IdleVariant = 2,
        Walk = 4,
        SitOnEdge = 8,
        SitOnFloor = 16,
        Dragged = 32,
        Fall = 64,
        Land = 128,
    }
}
