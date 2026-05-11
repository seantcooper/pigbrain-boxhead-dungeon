using TMPro;
using UnityEngine;
using pigbrain.core.UnityObject;

namespace pigbrain.game.Boxhead.UI
{
    [RequireComponent(typeof(TMP_Text))]
    [RequireComponent(typeof(Animation))]
    public class TMP_Message : MonoBehaviour
    {
        [SerializeField] TMP_Text tmp;

        public TMP_Message CreateInstance(object message)
        {
            TMP_Message inst = this.Instantiate();
            inst.tmp.text = $"{message}";
            return inst;
        }

        void OnValidate() => tmp = GetComponent<TMP_Text>();

        void Start()
        {
            var anim = GetComponent<Animation>();

            var rt = (RectTransform)transform;

            rt.sizeDelta = rt.anchoredPosition = rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;

            Destroy(gameObject, anim.clip.length);
        }
    }
}