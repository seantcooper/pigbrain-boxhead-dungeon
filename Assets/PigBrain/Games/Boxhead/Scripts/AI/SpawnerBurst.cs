using pigbrain.core.Inspector;
using pigbrain.core.UnityObject;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using pigbrain.core.Geom;
using System;
using pigbrain.core.Collections;
using pigbrain.game.Boxhead.Statistic;

namespace pigbrain.game.Boxhead
{
    public class SpawnerBurst : MonoBehaviour
    {
        [Range(0.0001f, 60)] public float interval = 1;
        [Range(0.0001f, 1)] public float minInterval = 0.1f;
        [Range(1, 50)] public int count = 10;
        [Range(1, 1000)] public int total = 10;
        public GameObject prefab;
        public int level;
        public bool ignoreClearCondition = false;

        [Header("Debug")]
        [ReadOnly] public int spawned = 0;
        [ReadOnly] public int remaining = 0;
        [ReadOnly] public int population;
        [SerializeField][ReadOnly] protected bool active;
        [SerializeField][ReadOnly] protected bool clear;

        public event Action<SpawnerBurst> onComplete;
        Spawner spawner;
        public Rnd rnd => spawner.rnd;

        public float totalTime => Mathf.Ceil((float)total / count) * interval;

        void OnDisable()
        {
            DestroyPopulation();
            StopAllCoroutines();
        }

        #region Start / Stop
        internal void StartBurst(Spawner parent, Queue<(Vector3 position, Quaternion rotation)> points) =>
            StartCoroutine(Run(parent, points));

        internal void StopBurst()
        {
            StopAllCoroutines();
            StartCoroutine(WaitForPopulation(() =>
            {
                onComplete?.Invoke(this);
                spawner = null;
            }));
        }
        #endregion

        #region Update
        internal IEnumerator Run(Spawner parent, Queue<(Vector3 position, Quaternion rotation)> points)
        {
            populationItems.Clear();
            spawner = parent;
            StatsController statsController = prefab.GetComponent<StatsController>();

            foreach (var li in prefab.GetComponentsInChildren<ILevelIndex>())
                li.SetIndex(level - 1);

            for (spawned = 0, remaining = total; spawned < total;)
            {
                yield return new WaitForSeconds(interval);
                yield return new Accumulator(minInterval, Mathf.Min(count, remaining), Spawn);
                remaining = total - spawned;
                yield return WaitForPopulation();
            }

            onComplete?.Invoke(this);

            void Spawn()
            {
                GameObject inst = null;
                if (!prefab.TryGetComponent(out ISpawnOverride so))
                {
                    var (p, r) = points.Dequeue();
                    inst = prefab.Instantiate(i => i.transform.SetPositionAndRotation(p, r));
                }
                else inst = so.Spawn(this, prefab);

                if (inst)
                {
                    parent.OnSpawn(inst);
                    populationItems.Add(inst);
                }
                spawned++;
            }
        }
        #endregion

        #region Population
        readonly HashSet<GameObject> populationItems = new();

        IEnumerator WaitForPopulation(Action onComplete = null)
        {
            yield return new WaitUntil(() =>
            {
                populationItems.RemoveWhere(go => !go);
                return populationItems.Count == 0;
            });
            onComplete?.Invoke();
        }

        void DestroyPopulation()
        {
            populationItems.ForEach(g => Destroy(g));
            populationItems.Clear();
        }
        #endregion
    }
}