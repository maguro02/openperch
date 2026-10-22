using UnityEngine;
using UnityEngine.Animations;

namespace OpenPerch.Runtime
{
    /// <summary>
    /// Replaces VRChat's Animator Layer Control. Forwards state enter and exit to <see cref="IPerchSmbHandler"/>.
    /// </summary>
    public sealed class PerchLayerControl : StateMachineBehaviour
    {
        public int schemaVersion = 1;
        public PlayableLayer playable;
        public int layer;
        public float goalWeight;
        public float blendDuration;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller)
        {
            if (animator.TryGetComponent<IPerchSmbHandler>(out var handler))
            {
                handler.OnLayerControl(this, controller, true);
            }
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller)
        {
            if (animator.TryGetComponent<IPerchSmbHandler>(out var handler))
            {
                handler.OnLayerControl(this, controller, false);
            }
        }
    }
}
