using System;

namespace OpenPerch.Format
{
    /// <summary>A range of exporter versions known to write broken packages.</summary>
    public readonly struct KnownBad
    {
        /// <param name="semverRange">A <see cref="SemVerRange"/> over exporter versions.</param>
        /// <param name="messageCode">The <see cref="MessageCatalog"/> code that explains the defect.</param>
        /// <param name="reject">True to refuse matching packages; false to accept them with a warning.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="semverRange"/> is not a valid <see cref="SemVerRange"/>, or <paramref name="messageCode"/> is null or empty.
        /// </exception>
        public KnownBad(string semverRange, string messageCode, bool reject)
        {
            if (!SemVerRange.TryParse(semverRange, out _)) throw new ArgumentException("Not a valid SemVer range: " + semverRange, nameof(semverRange));
            if (string.IsNullOrEmpty(messageCode)) throw new ArgumentException("A message code is required.", nameof(messageCode));
            Range = semverRange;
            MessageCode = messageCode;
            Reject = reject;
        }

        /// <summary>The <see cref="SemVerRange"/> text as given to the constructor, for example "&gt;=1.0.0 &lt;1.0.3".</summary>
        public string Range { get; }

        /// <summary>
        /// The <see cref="MessageCatalog"/> code that explains this defect, shown next to the generic known-bad code.
        /// <see cref="CompatibilityChecker.TryFindKnownBad"/> finds the entry behind a verdict.
        /// </summary>
        public string MessageCode { get; }

        /// <summary>
        /// True to refuse matching packages with <see cref="CompatCode.KnownBadExporter"/>; false to accept them with that
        /// code among the warnings.
        /// </summary>
        public bool Reject { get; }
    }
}
