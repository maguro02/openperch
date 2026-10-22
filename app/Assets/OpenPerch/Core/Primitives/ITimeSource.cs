using System;

namespace OpenPerch.App.Core
{
    public interface ITimeSource
    {
        DateTime UtcNow { get; }

        /// <summary>Seconds from an arbitrary origin. Unlike <see cref="UtcNow"/>, it never jumps when the wall clock is adjusted.</summary>
        double MonotonicSeconds { get; }
    }
}
