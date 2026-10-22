using System;

namespace OpenPerch.App.Core
{
    /// <summary>
    /// Integer rectangle with Win32 <c>RECT</c> semantics: <see cref="right"/> and <see cref="bottom"/> are exclusive.
    /// </summary>
    [Serializable]
    public struct RectI : IEquatable<RectI>
    {
        public int left;
        public int top;
        public int right;
        public int bottom;

        public RectI(int left, int top, int right, int bottom)
        {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }

        public int Width() => right - left;

        public int Height() => bottom - top;

        public bool IsEmpty() => right <= left || bottom <= top;

        public bool Contains(int x, int y) => x >= left && x < right && y >= top && y < bottom;

        /// <summary>Returns the overlap of <paramref name="a"/> and <paramref name="b"/>, or the default (empty) rectangle.</summary>
        public static RectI Intersect(RectI a, RectI b)
        {
            int l = Math.Max(a.left, b.left);
            int t = Math.Max(a.top, b.top);
            int r = Math.Min(a.right, b.right);
            int btm = Math.Min(a.bottom, b.bottom);
            return r <= l || btm <= t ? default : new RectI(l, t, r, btm);
        }

        public bool Equals(RectI other) =>
            left == other.left && top == other.top && right == other.right && bottom == other.bottom;

        public override bool Equals(object obj) => obj is RectI other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(left, top, right, bottom);

        public static bool operator ==(RectI a, RectI b) => a.Equals(b);

        public static bool operator !=(RectI a, RectI b) => !a.Equals(b);

        public override string ToString() => $"[{left}, {top}, {right}, {bottom})";
    }
}
