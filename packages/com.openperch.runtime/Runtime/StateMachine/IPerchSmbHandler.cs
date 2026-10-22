using UnityEngine.Animations;

namespace OpenPerch.Runtime
{
    /// <summary>
    /// Receives the events of the Perch state machine behaviours. The behaviours hold no state of their own; the handler,
    /// a component on the same GameObject as the Animator, keeps it all.
    /// </summary>
    public interface IPerchSmbHandler
    {
        void OnParameterDriver(PerchParameterDriver driver);

        void OnTrackingControl(PerchTrackingControl control);

        /// <summary>Called when a state carrying <paramref name="control"/> is entered or exited.</summary>
        /// <param name="control">The behaviour that raised the event.</param>
        /// <param name="controller">Playable of the controller whose state machine raised the event.</param>
        /// <param name="entering">True on state enter, false on state exit.</param>
        void OnLayerControl(PerchLayerControl control, AnimatorControllerPlayable controller, bool entering);

        /// <summary>Called when a state carrying <paramref name="control"/> is entered or exited.</summary>
        /// <param name="control">The behaviour that raised the event.</param>
        /// <param name="entering">True on state enter, false on state exit.</param>
        void OnPlayableLayerControl(PerchPlayableLayerControl control, bool entering);

        void OnLocomotionControl(PerchLocomotionControl control);
    }
}
