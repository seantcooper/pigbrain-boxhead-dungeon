using System;
using System.Collections;
using System.Collections.Generic;
using pigbrain.core.Collections;
using pigbrain.core.Geom;
using pigbrain.core.Motion;
using pigbrain.core.UnityObject;
using UnityEngine;

namespace pigbrain.game.Boxhead.UI
{
    public class MessageTicker : MonoBehaviourSingleton<MessageTicker>
    {
        [SerializeField]
        [Range(5, 40)] int entries = 10;
        [SerializeField] generated.TickerUI.PrefabView uiPrefab;
        [Range(0.5f, 20)] float uiDuration = 5;

        [SerializeField] generated.TickerWorld.PrefabView worldPrefab;
        [Range(0.5f, 20)] float worldDuration = 1.5f;

        readonly List<GameObject> messages = new();
        public static bool MuteMessages = false;

        void OnDisable()
        {
            messages.ForEach(m =>
            {
                m.DestroyObject();
            });
        }

        public static bool IsAvailable => Instance && Instance.gameObject.activeInHierarchy;
        public void CreateMessage(MessageTickerData message, Transform owner)
        {
            if (MuteMessages) return;
            if (Instance.uiPrefab) StartCoroutine(UIMessage(message));
            if (owner && Instance.worldPrefab)
            {
                switch (message.priority)
                {
                    case MessageTickerData.Priority.High:
                    case MessageTickerData.Priority.Medium:
                        StartCoroutine(WorldMessage(message, owner.position));
                        break;
                    default: break;
                }
            }
        }

        #region UI Message
        IEnumerator UIMessage(MessageTickerData message)
        {
            var inst = uiPrefab.Instantiate(transform);
            inst.textTicker.text.text = message.runtimeText;
            inst.textTicker.text.color = message.color;
            inst.imageIcon.image.sprite = message.icon;
            inst.imageIcon.image.color = message.color;
            inst.imageIcon.image.gameObject.SetActive(message.icon);
            inst.TryAddComponent(out CanvasGroup group);
            messages.Add(inst.gameObject);
            group.alpha = 1;

            RectTransform rt = (RectTransform)inst.transform;

            {
                Vector3 start = new(2, 1, 1), end = Vector3.one;
                yield return new OverTime(0.25f, (t) =>
                {
                    group.alpha = t;
                    inst.transform.localScale = Vector3.Lerp(start, end, t);
                });
            }

            yield return new WaitForSeconds(uiDuration);

            {
                Vector2 start = rt.anchoredPosition, end = start - new Vector2(200, 0);
                yield return new OverTime(0.25f, (t) =>
                {
                    group.alpha = 1 - t;
                    rt.anchoredPosition = Vector2.Lerp(start, end, t);
                });
            }

            messages.Remove(inst.gameObject);
            Destroy(inst.gameObject);
        }
        #endregion

        #region World Message
        IEnumerator WorldMessage(MessageTickerData message, Vector3 p)
        {
            var inst = worldPrefab.Instantiate();
            inst.transform.position = p;
            inst.textTMP.text.text = message.runtimeText;
            inst.textTMP.text.color = message.color;
            messages.Add(inst.gameObject);

            Vector3 scaleScale = new(4, 1, 1), endScale = Vector3.one;
            yield return new OverTime(0.25f, (t) =>
            {
                inst.textTMP.text.color = message.color.WithA(t);
                inst.transform.localScale = Vector3.Lerp(scaleScale, endScale, t);
            });

            Vector3 startPosition = inst.transform.localPosition, endPosition = startPosition.AddY(8);
            yield return new OverTime(worldDuration, (t) =>
            {
                inst.textTMP.text.color = message.color.WithA(1 - t);
                inst.transform.localPosition =
                    Vector3.Lerp(startPosition, endPosition, Ease.In(t, 1));
            });

            messages.Remove(inst.gameObject);
            Destroy(inst.gameObject);
        }
        #endregion

    }
}
