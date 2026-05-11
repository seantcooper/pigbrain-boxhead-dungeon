using System;
using System.Collections;
using System.Collections.Generic;
using pigbrain.core.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace pigbrain.game.Boxhead.UI
{
    public class PopEffect : MonoBehaviour
    {
        IEnumerator Pop(float scale = 2, float duration = 0.25f)
        {
            using var _ = new CanvasSortOrderScope(transform);
            yield return new OverTime(duration, (t) => ((RectTransform)transform)
                .localScale = Vector3.Lerp((float3)scale, (float3)1, t));
        }

        public static void TryPop(MonoBehaviour owner, float magnitude)
        {
            if (owner.isActiveAndEnabled && owner.TryGetComponent(out PopEffect pe))
                pe.StartCoroutine(pe.Pop(magnitude));
        }
    }

    public static class PopEffectX
    {
        public static void TryPop(this MonoBehaviour owner, float magnitude = 2) =>
            PopEffect.TryPop(owner, magnitude);
    }

    public class CanvasSortOrderScope : IDisposable
    {
        readonly static HashSet<Transform> Transforms = new();
        public Transform transform;
        public CanvasSortOrderScope(Transform transform)
        {
            Transforms.RemoveWhere(t => t);
            Transforms.Add(this.transform = transform);
            SetOrder(Transforms.Count);
        }
        void SetOrder(int order)
        {
            if (transform.TryGetComponent(out Canvas canvas))
                canvas.sortingOrder = order;
        }
        void IDisposable.Dispose() => SetOrder(0);
    }
}