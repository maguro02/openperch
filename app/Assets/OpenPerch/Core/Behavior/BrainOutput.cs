namespace OpenPerch.App.Core
{
    /// <summary>
    /// Result of one <see cref="MascotBrain"/> tick. Positions are client pixels with y pointing down.
    /// </summary>
    public struct BrainOutput
    {
        /// <summary>State after this tick.</summary>
        public MascotState State;

        /// <summary>Feet position, to be fed back as the next <see cref="BrainInput.FeetPx"/>.</summary>
        public Vec2 FeetPx;

        /// <summary>
        /// Where the avatar root goes; it maps to the world position (x / PPM, -y / PPM, 0). Equal to
        /// <see cref="FeetPx"/> except while sitting on an edge, where the root hangs the seat height below the edge.
        /// </summary>
        public Vec2 RootPx;

        /// <summary>-1 or +1 faces the direction of travel (left or right); 0 faces the viewer.</summary>
        public int Facing;

        /// <summary>Role to play, already replaced by its fallback when the motion for it is missing.</summary>
        public LocomotionRole Role;

        /// <summary>Playback speed of <see cref="Role"/>; 0 while hidden.</summary>
        public float RoleSpeed;
    }
}
