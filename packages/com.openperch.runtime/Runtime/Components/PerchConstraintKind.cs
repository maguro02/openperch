namespace OpenPerch.Runtime
{
    /// <summary>Which Unity-style constraint a <see cref="PerchConstraint"/> solves.</summary>
    public enum PerchConstraintKind
    {
        Position = 0,
        Rotation = 1,
        Scale = 2,
        Parent = 3,
        Aim = 4,
        LookAt = 5,
    }
}
