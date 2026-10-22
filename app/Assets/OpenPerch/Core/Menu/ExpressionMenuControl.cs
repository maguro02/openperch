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

        /// <summary>
        /// What a button, toggle or sub-menu writes to <see cref="Parameter"/>. It must be a value the parameter's declared type
        /// can hold, such as 0 or 1 for a Bool, a whole number from 0 to 255 for an Int, or a number from -1 to 1 for a Float, so
        /// that the parameter reads back as exactly this value: a toggle shows as on only while it does.
        /// </summary>
        public float Value;

        /// <summary>
        /// Parameters a puppet drives: horizontal and vertical for a two-axis puppet, up, right, down and left for a four-axis puppet,
        /// and the rotation for a radial puppet. Empty entries are skipped.
        /// </summary>
        public string[] SubParameters = Array.Empty<string>();

        /// <summary>
        /// Index of the page a sub-menu opens. An index outside the menu, including the default -1, makes the sub-menu do nothing;
        /// an index whose page is null opens an empty page.
        /// </summary>
        public int SubMenuIndex = -1;

        /// <summary>Direction labels of a two- or four-axis puppet, in up, right, down, left order.</summary>
        public string[] LabelNames = Array.Empty<string>();
    }
}
