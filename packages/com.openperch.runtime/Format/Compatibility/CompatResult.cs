using System;
using System.Collections.Generic;

namespace OpenPerch.Format
{
    /// <summary>
    /// <see cref="Code"/> is the first rejection found, or <see cref="CompatCode.Ok"/>; warnings are only collected for
    /// accepted packages.
    /// </summary>
    public readonly struct CompatResult
    {
        private readonly IReadOnlyList<CompatCode> _warnings;

        public CompatResult(CompatCode code, IReadOnlyList<CompatCode> warnings)
        {
            Code = code;
            _warnings = warnings;
        }

        public CompatCode Code { get; }

        /// <summary>In evaluation order. Never null.</summary>
        public IReadOnlyList<CompatCode> Warnings => _warnings ?? Array.Empty<CompatCode>();

        public bool Accept => Code == CompatCode.Ok;
    }
}
