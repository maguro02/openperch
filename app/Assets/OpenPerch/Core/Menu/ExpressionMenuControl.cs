using System;

namespace OpenPerch.App.Core
{
    /// <summary>
    /// One control of an <see cref="ExpressionMenuPage"/>: the engine-free counterpart of <c>OpenPerch.Runtime.MenuControlDef</c>,
    /// without the icons.
    /// </summary>
    public sealed class ExpressionMenuControl
    {
        public string Name = "";
        public ExpressionMenuControlType Type;

        /// <summary>
        /// Written with <see cref="Value"/> by a button, toggle or sub-menu; held at 1 while a puppet is open. Empty for none.
        /// </summary>
        public string Parameter = "";

        public float Value;

        /// <summary>
        /// Parameters a puppet drives: horizontal and vertical for a two-axis puppet, up, right, down and left for a four-axis puppet,
        /// and the rotation for a radial puppet. Empty entries are skipped.
        /// </summary>
        public string[] SubParameters = Array.Empty<string>();

        /// <summary>
        /// Index of the page a sub-menu opens. An index outside the menu, including the default -1, makes the sub-menu do nothing.
        /// </summary>
        public int SubMenuIndex = -1;

        /// <summary>Direction labels of a two- or four-axis puppet, in up, right, down, left order.</summary>
        public string[] LabelNames = Array.Empty<string>();
    }
}
