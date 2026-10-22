using UnityEngine;
using UnityEngine.Animations;

namespace OpenPerch.Runtime
{
    /// <summary>Replaces VRChat's Locomotion Control. Forwards state enter to <see cref="IPerchSmbHandler"/>.</summary>
    public sealed class PerchLocomotionControl : StateMachineBehaviour
    {
        public int schemaVersion = 1;
        public bool disableLocomotion;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller)
        {
            if (animator.TryGetComponent<IPerchSmbHandler>(out var handler))
            {
                handler.OnLocomotionControl(this);
            }
        }
    }
}
