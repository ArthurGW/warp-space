using UnityEngine;

namespace Animations
{
    public class FadeCompleteState : StateMachineBehaviour
    {
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            animator.GetComponent<FadeComplete>()?.onComplete?.Invoke();
        }
    }
}