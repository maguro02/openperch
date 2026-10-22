namespace OpenPerch.App.Core
{
    /// <summary>
    /// Kind of an <see cref="ExpressionMenuControl"/>. The values match <c>OpenPerch.Runtime.MenuControlType</c>,
    /// so converting between the two is a cast.
    /// </summary>
    public enum ExpressionMenuControlType
    {
        Button = 0,
        Toggle = 1,
        SubMenu = 2,
        TwoAxisPuppet = 3,
        FourAxisPuppet = 4,
        RadialPuppet = 5,
    }
}
