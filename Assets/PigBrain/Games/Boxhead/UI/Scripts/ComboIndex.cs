using System.Collections;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Collections;
using pigbrain.core.Geom;
using pigbrain.core.Inspector;
using pigbrain.core.UnityObject;
using pigbrain.game.Boxhead.Statistic;
using pigbrain.game.Boxhead.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static pigbrain.game.Boxhead.Statistic.Stat;
using static pigbrain.game.Boxhead.Statistic.Stats;
using static pigbrain.game.Boxhead.UI.Console;

namespace pigbrain.game.Boxhead
{
    public class ComboIndex : MonoBehaviourSingleton<ComboIndex>
    {
        [SerializeField][MinMaxRange(1, 1000)] MinMaxInt count = new(1, 999);
        [SerializeField][MinMaxRange(0.01f, 5)] MinMaxFloat time = new(0.1f, 5f);
        [SerializeField][Range(1f, 10f)] float curve = 5f;

        [Header("Rage")]
        [SerializeField] GameObject rage;

        [Header("UI")]
        [SerializeField] Image image;
        [SerializeField] Image fire;
        [SerializeField] TMP_Text text;

        [Header("Debug")]
        [ReadOnly] public float uiindex = 1;
        [SerializeField][ReadOnly] int index = 1;
        [SerializeField][ReadOnly] float deadline;
        [SerializeField][ReadOnly] float duration;
        [SerializeField][ReadOnly] State state;

        ControlValue enemykills, comboIndex, maxComboIndex;

        void OnEnable()
        {
            enemykills = StatsCatalog.Session.TryGetControl(Track_EnemyKills);
            comboIndex = StatsCatalog.Session.TryGetControl(Track_ComboIndex);
            maxComboIndex = StatsCatalog.Session.TryGetControl(Track_MaxComboIndex);
            enemykills.AddChangeListener(OnKill);
            StartNormal();
            if (ActivePlayer.Instance) ActivePlayer.Instance.OnPlayerDead += OnPlayerDead;
        }

        void OnPlayerDead(Player player)
        {
            comboIndex.Set(index = count.min);
            duration = deadline = 0;
        }

        void OnDisable()
        {
            if (ActivePlayer.Instance) ActivePlayer.Instance.OnPlayerDead -= OnPlayerDead; enemykills.RemoveChangeListener(OnKill);
            StopAllCoroutines();
        }

        void OnKill(ChangeEvent ev)
        {
            if (ev.delta == 0) return;
            index = Mathf.Clamp(index + Mathf.RoundToInt(ev.delta), count.min, count.max);

            comboIndex.Set(index);
            if (index > maxComboIndex) maxComboIndex.Set(index);

            duration = GetTime(index);
            deadline = Time.time + duration;
        }

        float GetTime(float cindex)
        {
            float t = Mathf.Clamp01((Mathf.Floor(cindex) - count.min) / count.range);
            return Mathf.Lerp(time.max, time.min, 1f - Mathf.Pow(1f - t, curve));
        }

        [ConsoleCommand("Rage")]
        public static (Console.Command.Status, string) ActivateRage(Console console, string[] args)
        {
            Instance.StartRage();
            return (Console.Command.Status.Success, "");
        }

        enum State { Normal, Rage }
        #region State
        Coroutine stateCoroutine;
        void StartStateCoroutine(IEnumerator routine)
        {
            if (stateCoroutine != null) StopCoroutine(stateCoroutine);
            stateCoroutine = StartCoroutine(routine);
        }

        float rageEndTime;
        void StartRage()
        {
            rage.SetActive(true);
            rageEndTime = ActiveRoom.Instance.player.Rage(true);
            ActiveRoom.Instance.player.onChangeState += StopRage;
            StartStateCoroutine(UpdateRage());
        }

        void StopRage()
        {
            rage.SetActive(false);
            ActiveRoom.Instance.player.Rage(false);
            ActiveRoom.Instance.player.onChangeState -= StopRage;
            StartNormal();
        }

        IEnumerator UpdateRage()
        {
            state = State.Rage;
            yield return new RepeatUntil(0.01f, UpdateUI, () => state != State.Rage);

            void UpdateUI()
            {
                float left = Mathf.Max(0, rageEndTime - Time.time);
                int secs = Mathf.CeilToInt(left);
                image.material.SetFloat("_Fill", secs / duration);
                text.text = $"{secs}";
                Shake(Mathf.PingPong(Time.time, 0.5f) + 0.5f);
            }
        }

        void StartNormal()
        {
            state = State.Normal;
            comboIndex.Set(index = count.min);
            duration = 0f;
            deadline = 0f;
            StartStateCoroutine(UpdateNormal());
        }

        IEnumerator UpdateNormal()
        {
            while (state == State.Normal)
            {
                UpdateUI();
                if (index == count.max)
                {
                    StartRage();
                    yield break;
                }

                if (duration > 0f && index > count.min && Time.time > deadline)
                {
                    float over = Time.time - deadline;
                    index = Mathf.Max(count.min, index - 1);
                    duration = GetTime(index);
                    deadline = Time.time + Mathf.Max(0f, duration - over);
                }
                yield return new WaitForNextUpdate();
            }

            void UpdateUI()
            {
                uiindex = duration <= 0f ? index : index + Mathf.Clamp01((deadline - Time.time) / duration);
                image.material.SetFloat("_Fill", uiindex % 1);
                text.text = $"{index}";
                Shake(Mathf.InverseLerp(count.min, count.max, uiindex));
            }
        }

        void Shake(float t)
        {
            fire.color = fire.color.WithA(Mathf.Lerp(0.2f, 1f, t));
            float scale = Mathf.Lerp(0.8f, 1.5f, t);
            float speed = Mathf.Lerp(1f, 20f, t);
            float rotZ = Mathf.Sin(Time.time * speed) * Mathf.Lerp(2f, 25f, t);
            fire.rectTransform.localScale = Vector3.one * scale;
            fire.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
        }
        #endregion
    }
}
