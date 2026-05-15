using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using pigbrain.core.Collections;
using pigbrain.core.Inspector;
using pigbrain.core.UnityObject;
using pigbrain.game.Boxhead.Statistic;
using TMPro;
using UnityEngine;

namespace pigbrain.game.Boxhead
{
    public class PadBase : MonoBehaviour
    {
        [SerializeField] protected Trait traits;
        [SerializeField] protected float fillDuration = 0.5f;
        [SerializeField][ReadOnly] protected float fill;
        [SerializeField] protected Collider trigger;
        [SerializeField] protected Renderer bar;
        [SerializeField] protected Color bgcolorNormal;
        [SerializeField] protected Color bgcolorOver;

        public event Action OnFillComplete;

        protected bool SetFill(float fill)
        {
            this.fill = Mathf.Clamp01(fill);
            UpdateUI();
            return fill >= 1;
        }

        void OnValidate()
        {
            UpdateUI();
            if (!trigger) trigger = GetComponentsInChildren<Collider>().FirstOrDefault(c => c.isTrigger);
        }

        protected virtual void Start()
        {
            fill = 0;
            UpdateUI();
        }

        MaterialPropertyBlock mpb;
        protected virtual void UpdateUI()
        {
            mpb ??= new MaterialPropertyBlock();
            mpb.SetFloat("_Fill", fill);
            mpb.SetColor("_BackgroundColor", over ? bgcolorOver : bgcolorNormal);
            bar.SetPropertyBlock(mpb);
        }

        bool TriggerCondition(Collider other, out IPadTrigger md)
        { md = null; return !other.isTrigger && other.TryGetComponent(out md) && md.canTrigger; }

        bool over = false;
        protected void OnTriggerEnter(Collider other)
        {
            if (!TriggerCondition(other, out var md)) return;
            over = true;
            StartFill(other);
            UpdateUI();
        }

        protected void OnTriggerExit(Collider other)
        {
            if (!TriggerCondition(other, out var md)) return;
            over = false;
            if (traits.HasFlag(Trait.ResetOnLeave)) fill = 0;
            EndFill(other);
            UpdateUI();
        }

        protected virtual void CompleteFill(Collider other)
        {
            GetComponentInChildren<Collider>().enabled = false;
            OnFillComplete?.Invoke();
            if (traits.HasFlag(Trait.DeactivateOnUsed))
                StartCoroutine(CoroutineUtility.Delay(0.1f, () => gameObject.SetActive(false)));
            else if (traits.HasFlag(Trait.DestroyOnUsed))
                Destroy(gameObject, 0.1f);
        }

        protected virtual void StartFill(Collider other) { }
        protected virtual void EndFill(Collider other) { }

        [Flags]
        protected enum Trait
        {
            None = 0,
            ResetOnLeave = 1 << 0,
            DestroyOnUsed = 1 << 2,
            DeactivateOnUsed = 1 << 3,
            Another = 1 << 16,
        }
    }

    public interface IPadTrigger
    {
        bool canTrigger { get; }
    }
}
