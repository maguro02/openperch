using System;
using System.Collections.Generic;

namespace OpenPerch.Core.Tests.Settings
{
    /// <summary>Breaks each persisted-type rule once, to prove <c>PersistTypeRules</c> reports it.</summary>
    [Serializable]
    public sealed class RuleBreakingSample
    {
        public readonly int frozen = 1;
        public Dictionary<string, int> lookup = new Dictionary<string, int>();
        public HashSet<int> unique = new HashSet<int>();
        public IList<int> abstractList = new List<int>();
        public OpenSample open = new OpenSample();
        public DerivedSample derived = new DerivedSample();
        public NoDefaultConstructorSample noDefault = new NoDefaultConstructorSample(1);
        public UnserializableSample unmarked = new UnserializableSample();
        public FrozenPoint point;
        public DateTime when;
        public object anything = new object();
        public List<List<int>> nested = new List<List<int>>();
        public int[,] grid = new int[1, 1];
        public int? maybe;
        public WideSampleKind wide;
        public List<int> missing;

        private int _hidden;

        public int Count { get; set; }

        public int GetHidden() => _hidden;

        public void SetHidden(int value) => _hidden = value;
    }
}
