using UnityEngine;
using UnityEngine.Animations;

namespace OpenPerch.Runtime
{
    /// <summary>Replaces VRChat's Tracking Control. Forwards state enter to <see cref="IPerchSmbHandler"/>.</summary>
    public sealed class PerchTrackingControl : StateMachineBehaviour
    {
        public int schemaVersion = 1;
        public TrackingMode eyes;
        public TrackingMode mouth;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller)
        {
            if (animator.TryGetComponent<IPerchSmbHandler>(out var handler))
            {
                handler.OnTrackingControl(this);
            }
        }
    }
}
