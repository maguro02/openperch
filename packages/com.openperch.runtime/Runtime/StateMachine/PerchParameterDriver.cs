using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

namespace OpenPerch.Runtime
{
    /// <summary>Replaces VRChat's Avatar Parameter Driver. Forwards state enter to <see cref="IPerchSmbHandler"/>.</summary>
    public sealed class PerchParameterDriver : StateMachineBehaviour
    {
        public int schemaVersion = 1;
        public bool localOnly;
        public List<DriverEntry> parameters = new();

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller)
        {
            if (animator.TryGetComponent<IPerchSmbHandler>(out var handler))
            {
                handler.OnParameterDriver(this);
            }
        }
    }
}
