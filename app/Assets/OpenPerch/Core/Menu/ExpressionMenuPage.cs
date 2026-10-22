using System;

namespace OpenPerch.App.Core
{
    /// <summary>
    /// One page of an expression menu: the engine-free counterpart of <c>OpenPerch.Runtime.MenuPageDef</c>.
    /// </summary>
    public sealed class ExpressionMenuPage
    {
        /// <summary>A page shows at most this many controls; any after them are ignored.</summary>
        public const int MaxControls = 8;

        public string Name = "";
        public ExpressionMenuControl[] Controls = Array.Empty<ExpressionMenuControl>();
    }
}
