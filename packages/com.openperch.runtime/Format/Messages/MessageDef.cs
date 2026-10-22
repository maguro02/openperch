using System;

namespace OpenPerch.Format
{
    /// <summary>A user-facing message shared by the exporter and the app.</summary>
    public sealed class MessageDef
    {
        public MessageDef(string code, MessageSeverity severity, string ja, string en, string remedyAnchor, bool autoFix)
        {
            if (string.IsNullOrEmpty(code)) throw new ArgumentException("A code is required.", nameof(code));
            Code = code;
            Severity = severity;
            Ja = ja ?? "";
            En = en ?? "";
            RemedyAnchor = remedyAnchor ?? "";
            AutoFix = autoFix;
        }

        public string Code { get; }

        public MessageSeverity Severity { get; }

        public string Ja { get; }

        public string En { get; }

        /// <summary>The fragment of the section in <see cref="MessageCatalog.TroubleshootingPath"/> that gives the cause and remedy.</summary>
        public string RemedyAnchor { get; }

        /// <summary>Whether the exporter can fix the problem itself when the user asks it to.</summary>
        public bool AutoFix { get; }
    }
}
