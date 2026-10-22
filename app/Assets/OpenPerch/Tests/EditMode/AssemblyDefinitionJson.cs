using System;

namespace OpenPerch.App.Tests.EditMode
{
    /// <summary>The parts of an .asmdef file that <see cref="AssemblyIsolationTests"/> checks.</summary>
    [Serializable]
    public sealed class AssemblyDefinitionJson
    {
        public string name = "";

        /// <summary>Assembly names, or <c>GUID:&lt;guid&gt;</c> when the asmdef references by GUID.</summary>
        public string[] references = Array.Empty<string>();

        public string[] includePlatforms = Array.Empty<string>();
        public string[] precompiledReferences = Array.Empty<string>();
        public bool noEngineReferences;
    }
}
