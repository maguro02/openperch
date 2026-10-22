using System;

namespace OpenPerch.App.Core
{
    [Serializable]
    public struct Vec2I
    {
        public int x;
        public int y;

        public Vec2I(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public static Vec2I operator +(Vec2I a, Vec2I b) => new Vec2I(a.x + b.x, a.y + b.y);
        public static Vec2I operator -(Vec2I a, Vec2I b) => new Vec2I(a.x - b.x, a.y - b.y);

        public override string ToString() => $"({x}, {y})";
    }
}
