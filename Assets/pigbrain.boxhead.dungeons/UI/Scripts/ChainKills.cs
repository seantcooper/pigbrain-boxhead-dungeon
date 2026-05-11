using UnityEngine;
using static pigbrain.game.Boxhead.Statistic.Stat;
using pigbrain.game.Boxhead.Statistic;
using System;
using static pigbrain.game.Boxhead.Statistic.Stats;
using pigbrain.core.UnityObject;
using pigbrain.core.Geom;
using TMPro;
using System.Collections;
using pigbrain.core.Collections;
using System.Collections.Generic;
using pigbrain.game.Boxhead.Environment;
using System.Linq;
using UnityEngine.AI;

namespace pigbrain.game.Boxhead
{
    // Chain Kills (combination killing)
    //      - i.e. comboCount++ if<chain time and max time
    //      - Chain kill count reset when chain time or max time elapses
    //      - Chain kill reward

    public class ChainKills : MonoBehaviour
    {
        const float MinChainKill = 2;
        [SerializeField][Range(0.01f, 1)] float chainTime = 0.1f;
        [SerializeField][Range(0.01f, 30)] float chainTimeMax = 1f;
        [SerializeField] GameObject prefab;
        ChainRewardContainer rewards;

        // readonly List<Pickup> pickups = new();

        float delta, endTime, maxEndTime;
        Vector3? lastPosition;

        ControlValue enemykills, maxChainKills;
        void OnKill(ChangeEvent ev)
        {
            if (ev.delta == 0) return;
            if (delta == 0) maxEndTime = Time.time + chainTimeMax;
            delta += ev.delta;
            endTime = Time.time + chainTime;
            if (ev.stats.GetPosition(ev.stat, out Vector3 p)) lastPosition = p;
        }

        void LateUpdate() => OnChain();

        void OnEnable()
        {
            enemykills = StatsCatalog.Session.TryGetControl(Track_EnemyKills);
            maxChainKills = StatsCatalog.Session.TryGetControl(Track_MaxChainKills);
            enemykills.AddChangeListener(OnKill);
            rewards = EnvironmentData.GetChainRewardContainer();
            // if (ActivePlayer.Instance)
            // {
            //     Pickup.OnCreated += OnPickupCreated;
            //     ActivePlayer.Instance.OnPlayerDead += OnPlayerDead;
            //     ActivePlayer.Instance.OnPlayerRespawn += OnPlayerRespawn;
            // }
        }

        // void OnPlayerRespawn(Player player) => ClearPopulation();
        // void OnPlayerDead(Player player) { } // => ClearPopulation();

        void OnDisable()
        {
            // if (ActivePlayer.Instance) ActivePlayer.Instance.OnPlayerDead -= OnPlayerDead;
            enemykills.RemoveChangeListener(OnKill);
            // ClearPopulation();
            StopAllCoroutines();
        }

        void OnChain()
        {
            if (Time.time < endTime && Time.time < maxEndTime) return;

            int count = Mathf.RoundToInt(delta);
            delta = 0;

            // Not enough
            if (count < MinChainKill) return;

            if (count > maxChainKills) maxChainKills.Set(count);

            if (lastPosition == null) { Debug.LogError("No position for Reward"); return; }
            CreateReward((Vector3)lastPosition, count);
        }

        #region Create Box reward
        void CreateReward(Vector3 position, int count)
        {
            ChainReward best = null;
            foreach (var reward in rewards.items)
                if (reward.prefab && count > reward.count) best = reward;

            if (best)
            {
                if (NavMesh.SamplePosition(position, out NavMeshHit hit, 5, 1 << NavMesh.GetAreaFromName("Walkable")))
                    position = hit.position;

                CreateNumber(position, count, best.color);
                best.prefab.Instantiate(position);
            }
        }

        // void ClearPopulation()
        // {
        //     Debug.Log($"Clear Population {pickups.Count}");
        //     pickups.Where(g => g).ForEach(g => Destroy(g));
        //     pickups.Clear();
        // }
        #endregion

        #region Chain Kill Number
        void CreateNumber(Vector3 position, int count, Color color)
        {
            if (!prefab) return;

            // Debug.Log($"Create {count} @ {position}");

            var inst = prefab.Instantiate(position.WithY(1)).transform;
            var tmp = inst.GetComponentInChildren<TMP_Text>();
            tmp.color = color;
            tmp.text = $"{count}";

            StartCoroutine(Motion(inst, tmp));
            const float Duration = 3, Fade = 2.5f / Duration, Start = 2, End = 10;
            IEnumerator Motion(Transform transform, TMP_Text tmp)
            {
                Vector3 start = transform.position.AddY(Start), end = transform.position.AddY(End);
                Vector3 startScale = transform.localScale, endScale = startScale * 3;
                yield return new OverTime(Duration, (t) =>
                {
                    if (!transform) return;
                    transform.position = Vector3.Lerp(start, end, t);
                    transform.localScale = Vector3.Lerp(startScale, endScale, t);
                    float fade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(Fade, 1f, t));
                    tmp.color = tmp.color.WithA(1 - fade);
                });
                inst.DestroyObject();
            }
        }
        #endregion
    }
}

namespace pigbrain.game.Boxhead
{
    using pigbrain.core.Inspector;

    [Serializable]
    public class ChainRewardContainer : DropBox<ChainReward> { }
    [Serializable]
    public class ChainReward
    {
        [DropBoxTarget] public GameObject prefab;
        public int count = 3;
        public Color color;
        public static implicit operator bool(ChainReward empty) => empty != null;
    }
}

#region "Editor"
#if UNITY_EDITOR
namespace pigbrain.game.Boxhead
{
    using UnityEditor;
    using UnityEngine;
    using pigbrain.core.Geom;
    using static pigbrain.core.Inspector.InspectorUtility;

    [CustomPropertyDrawer(typeof(ChainReward), true)]
    public class PrefabList_PrefabInfo_Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            EditorGUI.BeginProperty(pos, label, prop);
            prop.GetProperties(nameof(ChainReward.prefab), nameof(ChainReward.color), nameof(ChainReward.count))
                .DrawPropertyFields(pos.WithH(LineHeight).DivideArea(Padding, 0, MiniFieldWidth, TinyFieldWidth));
            EditorGUI.EndProperty();
        }
        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label) =>
            LineHeight + VerticalSpacing;
    }
}
#endif
#endregion
