using System;
using System.Collections;
using pigbrain.core.Collections;
using pigbrain.core.Inspector;
using pigbrain.core.UnityObject;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class IconMessage : MonoBehaviour
{
    [SerializeField][Range(0, 1)] float transitionDuration = 0.25f;
    [SerializeField][Range(0, 2)] float holdDuration = 2f;
    [SerializeField] CanvasGroup canvasGroup;

    [ReadOnly] public float queueDuration;

    void OnValidate()
    {
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
    }

    public float totalDuration => transitionDuration * 2 + holdDuration;
    public float overlapDuration => transitionDuration + holdDuration;

    IEnumerator Start()
    {
        canvasGroup.alpha = 0;
        if (queueDuration > 0) yield return new WaitForSeconds(queueDuration);
        yield return new OverTime(transitionDuration, (t) => { canvasGroup.alpha = t; });
        yield return new WaitForSeconds(holdDuration);
        yield return new OverTime(transitionDuration, (t) => { canvasGroup.alpha = 1 - t; });
        Destroy(gameObject);
    }

    public class Queue
    {
        RectTransform container;
        float time;
        public Queue(RectTransform container)
        {
            this.container = container;
            this.time = 0;
        }

        public IconMessage Message(IconMessage prefab)
        {
            var inst = prefab.Instantiate(container);
            inst.queueDuration = Mathf.Max(0, time - Time.time);
            return inst;
        }
    }
}
