using System.Collections;
using pigbrain.core.Collections;
using pigbrain.game.Boxhead.UI;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using static pigbrain.game.Boxhead.Statistic.Stats;

namespace pigbrain.game.Boxhead.Statistic
{
    public class TMP_Stat : MonoBehaviour
    {
        [SerializeField] StatsSingle stats;
        [SerializeField] TMP_Text tmp;
        [SerializeField] Notify positive;
        [SerializeField] Notify negative;

        void OnValidate() { if (!tmp) tmp = GetComponentInChildren<TMP_Text>(); }
        void OnEnable() => stats.AddChangeListener(OnStatChanged);
        void OnDisable() => stats.RemoveChangeListener(OnStatChanged);
        void OnStatChanged(ChangeEvent ev)
        {
            if (tmp)
            {
                if (ev.oldValue < ev.newValue) this.TryPop(GetMagnitude(positive));
                else if (ev.oldValue > ev.newValue) this.TryPop(GetMagnitude(negative));
                tmp.text = $"{ev.newValue}";
            }
        }

        // Coroutine Alert(Notify notify)
        // {
        //     IEnumerator Zoom(float z)
        //     {
        //         var rt = transform as RectTransform;
        //         var pivot = rt ? rt.pivot : new Vector2(0.5f, 0.5f);

        //         if (rt) rt.pivot = new Vector2(0.5f, 0.5f);

        //         var from = Vector3.one * z;
        //         var to = Vector3.one;

        //         yield return new OverTime(0.25f, t => transform.localScale = Vector3.Lerp(from, to, t));

        //         if (rt) rt.pivot = pivot;
        //     }

        //     switch (notify)
        //     {
        //         default: return null;
        //         case Notify.Zoom: return isActiveAndEnabled ? StartCoroutine(Zoom(1.5f)) : null;
        //         case Notify.ZoomLarge: return isActiveAndEnabled ? StartCoroutine(Zoom(2f)) : null;
        //     }
        // }

        float GetMagnitude(Notify notify) => notify switch
        {
            Notify.Zoom => 1.5f,
            Notify.ZoomLarge => 2,
            _ => 1,
        };

        enum Notify
        {
            None = 0,
            Zoom = 1,
            ZoomLarge = 2,
        }
    }
}
