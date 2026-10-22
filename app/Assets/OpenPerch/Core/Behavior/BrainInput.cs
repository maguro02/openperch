namespace OpenPerch.App.Core
{
    /// <summary>
    /// What <see cref="MascotBrain"/> sees in one tick. Positions are client pixels with y pointing down.
    /// </summary>
    public struct BrainInput
    {
        /// <summary>Seconds since the previous tick.</summary>
        public float Dt;

        /// <summary>
        /// Current feet position: the previous <see cref="BrainOutput.FeetPx"/>, moved by the owner when the window
        /// being stood on moves, and taken from the cursor while dragged.
        /// </summary>
        public Vec2 FeetPx;

        /// <summary>
        /// Surface under the feet within <see cref="Surfaces"/>, or <see cref="SurfaceRef.None"/> when the owner finds none.
        /// Ignored while airborne or dragged; leaving <see cref="MascotState.Hidden"/> reads it to decide whether to fall.
        /// </summary>
        public SurfaceRef Standing;

        public SurfaceSet Surfaces;

        /// <summary>
        /// Pointer state accumulated since the previous tick. <see cref="PointerState.LeftHeld"/> must reflect the button,
        /// because a drag ends as soon as it is false.
        /// </summary>
        public PointerState Pointer;

        /// <summary>The hit test puts this avatar under the cursor; false while Alt lets clicks pass through.</summary>
        public bool PointerOnThisAvatar;

        /// <summary>The mascot has to stay hidden: a fullscreen app, hide-all, or a locked session.</summary>
        public bool Fullscreen;

        /// <summary>An FX locomotion control disabled locomotion, so walking and jumping are not allowed.</summary>
        public bool LocomotionBlockedByFx;

        /// <summary>
        /// The window being stood on was minimized, cloaked, destroyed, maximized, changed process, or moved further
        /// than the owner follows.
        /// </summary>
        public bool StandingLostHard;

        public MotionAvailability Motions;

        public PomodoroMode Pomodoro;

        /// <summary>Walking speed in pixels per second.</summary>
        public float WalkSpeedPx;

        /// <summary>On-screen height of the avatar in pixels.</summary>
        public float DisplayHeightPx;

        /// <summary>Seat height of the edge-sitting motion in pixels: how far the root hangs below the edge.</summary>
        public float SeatHeightPx;

        /// <summary>Pixels per metre of this avatar. Gravity is 9.81 m/s² scaled by it.</summary>
        public float PixelsPerMeter;

        /// <summary>
        /// The one-shot role (<see cref="LocomotionRole.Land"/> or <see cref="LocomotionRole.IdleVariant"/>) requested by
        /// the previous output has finished playing. Set for a single tick.
        /// </summary>
        public bool OneShotDone;
    }
}
