using System;
using System.Collections.Generic;

namespace OpenPerch.Core.Tests.Settings
{
    /// <summary>Complies with the rules; its static members would break them if they were inspected.</summary>
    [Serializable]
    public sealed class StaticMembersSample
    {
        public const int Limit = 3;
        public static readonly Dictionary<string, int> Cache = new Dictionary<string, int>();

        public int value;

        public static int Instances { get; set; }
    }
}
