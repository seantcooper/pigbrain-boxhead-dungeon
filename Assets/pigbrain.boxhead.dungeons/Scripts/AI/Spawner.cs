using pigbrain.core.Collections;
using pigbrain.core.Inspector;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using pigbrain.core.Geom;
using System.Linq;
using System;

namespace pigbrain.game.Boxhead
{
    public class Spawner : MonoBehaviour
    {
        [SerializeField] uint seed = 10001;
        [Range(0, 5)] public float delay = 1;
        // public bool startAutomatically;

        [FlattenObject] public SpawnerShape[] shapes;
        [FlattenObject] public SpawnerBurst[] bursts;

        [Header("Debug")]
        [ReadOnly] public int total;
        [ReadOnly] public int population;
        [ReadOnly] public int spawned;
        [ReadOnly] public int remaining;

        public event Action onComplete;

        internal Rnd rnd;

        void OnValidate() => bursts = GetComponents<SpawnerBurst>();
        void OnDisable()
        {
            onComplete = null;
            StopAllCoroutines();
        }

        public void Activate()
        {
            gameObject.SetActive(true);
            if (bursts.IsNullOrEmpty()) bursts = GetComponents<SpawnerBurst>();
            Debug.Log("Spawner Activated!");
            StartCoroutine(Run());
            StartCoroutine(Population());
        }

        #region Update
        IEnumerator Population()
        {
            while (true)
            {
                population = this.bursts.Sum(b => b.population);
                yield return 0.1f;
            }
        }

        IEnumerator Run()
        {
            yield return new WaitForNextUpdate();
            yield return new WaitForSeconds(delay);
            rnd ??= new Rnd(seed);

            HashSet<SpawnerBurst> runtimeBursts = bursts.Where(b => b.enabled && b.total > 0).ToHashSet();
            total = runtimeBursts.Sum(b => b.total);
            if (total == 0) yield break;

            Queue<(Vector3 position, Quaternion rotation)> points = new(GetPoints(total));

            HashSet<SpawnerBurst> waitingBursts = runtimeBursts.Where(b => !b.ignoreClearCondition).ToHashSet();
            runtimeBursts.ForEach(b => b.onComplete += OnBurstComplete);

            void OnBurstComplete(SpawnerBurst burst)
            {
                runtimeBursts.Remove(burst);
                burst.onComplete -= OnBurstComplete;
                waitingBursts.Remove(burst);
            }

            runtimeBursts.ForEach(b => b.StartBurst(this, points));

            spawned = remaining = 0;
            while (waitingBursts.Count > 0) yield return new WaitForNextUpdate();
            runtimeBursts.ForEach(b => b.StopBurst());
            while (runtimeBursts.Count > 0) yield return new WaitForNextUpdate();

            onComplete?.Invoke();
            gameObject.SetActive(false);
        }

        void StartBursts()
        {

        }
        #endregion

        internal void OnSpawn(GameObject gameObject)
        {
            spawned = bursts.Sum(b => b.spawned);
            remaining = bursts.Sum(b => b.remaining);
        }

        #region Points
        internal IEnumerable<(Vector3 position, Quaternion rotation)> GetPoints(int count) => GetPoints(rnd, count);
        internal IEnumerable<(Vector3 position, Quaternion rotation)> GetPoints(Rnd rnd, int count)
        {
            float totalArea = shapes.Sum(s => s.area);
            int[] distribution = shapes.Select(s => Mathf.CeilToInt(count * s.area / totalArea)).ToArray();
            return shapes.SelectMany((s, i) => Enumerable.Range(0, distribution[i])
                .Select(j => (s.transform.TransformPoint(rnd.NextInsideBox(Vector3.one)), s.transform.rotation)))
                    .OrderBy(r => rnd.NextFloat());
        }
        #endregion

        #region Gizmos
        void OnDrawGizmosSelected()
        {
            if (!gameObject.activeSelf) return;
            var rnd = new Rnd(seed);
            Gizmos.color = Color.red;
            var points = GetPoints(rnd, bursts.Sum(b => b.total));
            points.ForEach(p => Gizmos.DrawCube(p.position, new(0.5f, 0.5f, 0.5f)));
        }
        #endregion
    }

    public interface ISpawnOverride
    {
        GameObject Spawn(SpawnerBurst burst, GameObject prefab);
    }

}