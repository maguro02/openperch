using System;

namespace OpenPerch.Format
{
    /// <summary>A range of exporter versions known to write broken packages.</summary>
    public readonly struct KnownBad
    {
        /// <param name="semverRange">A <see cref="SemVerRange"/> over exporter versions.</param>
        /// <param name="messageCode">The <see cref="MessageCatalog"/> code that explains the defect.</param>
        /// <param name="reject">True to refuse matching packages; false to accept them with a warning.</param>
        public KnownBad(string semverRange, string messageCode, bool reject)
        {
            if (!SemVerRange.TryParse(semverRange, out _)) throw new ArgumentException("Not a valid SemVer range: " + semverRange, nameof(semverRange));
            if (string.IsNullOrEmpty(messageCode)) throw new ArgumentException("A message code is required.", nameof(messageCode));
            Range = semverRange;
            MessageCode = messageCode;
            Reject = reject;
        }

        public string Range { get; }

        public string MessageCode { get; }

        public bool Reject { get; }
    }
}
