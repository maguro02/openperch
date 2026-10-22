namespace OpenPerch.Format
{
    /// <summary>One problem found in a manifest.</summary>
    public readonly struct ManifestError
    {
        public ManifestError(ManifestErrorCode code, string field)
        {
            Code = code;
            Field = field;
        }

        public ManifestErrorCode Code { get; }

        /// <summary>The JSON path of the offending field, for example "bundle.length" or "warnings[3].detail".</summary>
        public string Field { get; }

        public override string ToString() => string.IsNullOrEmpty(Field) ? Code.ToString() : Code + " (" + Field + ")";
    }
}
