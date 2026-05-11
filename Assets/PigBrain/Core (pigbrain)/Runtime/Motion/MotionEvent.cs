using pigbrain.core.Inspector;
using UnityEngine;

namespace pigbrain.core.Motion
{
    public class MotionEvent : MonoBehaviour
    {
        [SerializeField][InlineScriptableObject] MotionData onStart;
        [SerializeField][InlineScriptableObject] MotionData onEnable;

        Vector3 localScale, localPosition;
        Quaternion localRotation;

        void Awake()
        {
            localScale = transform.localScale;
            transform.GetLocalPositionAndRotation(out localPosition, out localRotation);
        }

        void Animate(MotionData data)
        {
            if (!data || !isActiveAndEnabled) return;
            data.Play(this, transform);
        }

        void Start()
        {
            Animate(onStart);
            Animate(onEnable);
        }

        void OnEnable()
        {
            if (didStart) Animate(onEnable);
        }

        void OnDisable()
        {
            StopAllCoroutines();
            transform.localScale = localScale;
            transform.SetLocalPositionAndRotation(localPosition, localRotation);
        }
    }
}