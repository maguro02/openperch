using UnityEngine;
using UnityEngine.Animations;

namespace OpenPerch.Runtime
{
    /// <summary>
    /// Replaces VRChat's Playable Layer Control. Forwards state enter and exit to <see cref="IPerchSmbHandler"/>.
    /// </summary>
    public sealed class PerchPlayableLayerControl : StateMachineBehaviour
    {
        public int schemaVersion = 1;
        public PlayableLayer layer;
        public float goalWeight;
        public float blendDuration;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller)
        {
            if (animator.TryGetComponent<IPerchSmbHandler>(out var handler))
            {
                handler.OnPlayableLayerControl(this, true);
            }
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller)
        {
            if (animator.TryGetComponent<IPerchSmbHandler>(out var handler))
            {
                handler.OnPlayableLayerControl(this, false);
            }
        }
    }
}
