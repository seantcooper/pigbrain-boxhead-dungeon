using System;
using System.Collections;
using System.Linq;
using pigbrain.core.Collections;
using pigbrain.core.Geom;
using pigbrain.core.Inspector;
using pigbrain.core.UnityObject;
using pigbrain.core.Utility;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace pigbrain.game.Boxhead
{
    [CreateAssetMenu(menuName = "PigBrain/Boxhead/Commands/Create")]
    public class CommandCreate : Command, Command.IPrefab
    {
        [Header("Create")]
        [SerializeField] Traits traits;
        // [SerializeField] internal GameObject prefab2;
        [SerializeField][FormerlySerializedAs("prefabObject")] internal PrefabObject prefab;
        // [SerializeField] internal PoolBehaviour poolprefab;
        [SerializeField] internal int count = 1;
        [SerializeField][Range(0, 2)] float baseOffset = 1;
        [SerializeField][Range(0, 2)] float frequency = 0;
        [SerializeField][Range(1, 50)] int burst = 1;

        [Flags]
        enum Traits
        {
            None = 0,
            Volumize = 1 << 0,
            PoolObject = 1 << 1,
            Other = 1 << 16,
        }

        [SerializeField][ToggleObject] Spread spread;
        [SerializeField][ToggleObject] Force force;
        Spawn[] spawners => new Spawn[] { spread, force };

        GameObject IPrefab.GetPrefab() => prefab;

        public void SetCount(int count) => this.count = count;

        protected override bool OnInvoke(Transform target)
        {
            if (!prefab || !target) return false;
            spawners.Where(s => s.enabled).ForEach(s => s.Start(this, target));
            ApplicationMonitor.Instance.StartCoroutine(Run(target));
            return true;
        }

        IEnumerator Run(Transform target)
        {
            if (count > 0)
            {
                if (traits.HasFlag(Traits.Volumize))
                {
                    var inst = Instantiate(target);
                    var scale = inst.transform.localScale.Min();
                    var volumized = scale * math.pow(count, 1f / 3f);
                    inst.transform.localScale *= volumized / scale;
                }
                else
                {
                    for (int i = 0; i < count;)
                    {
                        for (int b = burst; b > 0 && i < count; Instantiate(target), --b, i++) ;
                        if (frequency > 0.01f) yield return new WaitForSeconds(frequency);
                    }
                }
            }
            spawners.Where(s => s.enabled).ForEach(s => s.Stop(this));
        }

        event Action<GameObject, Transform> OnInstantiate;
        internal GameObject Instantiate(Transform target)
        {
            if (!prefab) return null;
            var inst = prefab.Instantiate(target.position.AddY(baseOffset), target.rotation);
            OnInstantiate?.Invoke(inst, target);
            return inst;
        }

        [Serializable]
        class Spread : Spawn
        {
            [SerializeField][Range(0, 5)] float radius;
            public override void Start(CommandCreate command, Transform target) =>
                command.OnInstantiate += OnInstantiate;
            public override void Stop(CommandCreate command) =>
                command.OnInstantiate -= OnInstantiate;
            void OnInstantiate(GameObject inst, Transform target) =>
                inst.transform.position += UnityEngine.Random.insideUnitSphere * radius;
        }

        [Serializable]
        class Force : Spawn
        {
            [SerializeField][Range(0, 100)] float force = 1;
            public override void Start(CommandCreate command, Transform target) =>
                command.OnInstantiate += OnInstantiate;
            public override void Stop(CommandCreate command) =>
                command.OnInstantiate -= OnInstantiate;

            void OnInstantiate(GameObject inst, Transform target) =>
                inst.GetComponent<Rigidbody>()
                    .AddForce((UnityEngine.Random.insideUnitSphere * force).AbsY(), ForceMode.Impulse);
        }

        [Serializable]
        class Spawn
        {
            [SerializeField] internal bool enabled = false;
            public virtual void Start(CommandCreate command, Transform target) { }
            public virtual void Stop(CommandCreate command) { }
            protected void StartCoroutine(IEnumerator run) =>
                ApplicationMonitor.Instance.StartCoroutine(run);
        }


    }
}
