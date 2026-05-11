using UnityEngine;

public class TriggerAnimation : MonoBehaviour
{
    public void Trigger(string animation)
    {
        if (TryGetComponent(out Animator animator))
            animator.SetBool(animation, true);
    }
}
