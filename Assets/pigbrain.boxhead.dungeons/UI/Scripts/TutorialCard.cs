using System;
using System.Collections;
using pigbrain.core.Statistics;
using pigbrain.core.UnityObject;
using pigbrain.core.UnityUI;
using UnityEngine;
using static pigbrain.core.UnityUI.RectTransformUtility;

namespace pigbrain.game.Boxhead.UI
{
    public class TutorialCard : MonoBehaviour
    {
        [SerializeField] RectTransform target;

        public event Action<TutorialCard> OnClose;

        void OnValidate()
        {
            if (!target) target = (RectTransform)transform.parent;
        }

        void OnEnable() => StartCoroutine(Run());

        bool close = false;
        public void Close() => close = true;

        IEnumerator Run()
        {
            using TimeScale.Scope timeScaleScope = new(this, 0.5f, 0);
            using ForwardScope forwardScope = target.BringForward(1000);

            while (!close)
            {
                yield return null;
            }

            StopAllCoroutines();
            OnClose?.Invoke(this);
            this.SetActive(false);
        }
    }
}