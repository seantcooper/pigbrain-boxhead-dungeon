#pragma warning disable UDR0001
using System.Linq;
using pigbrain.core.Inspector;
using UnityEngine;
using Unity.AI.Navigation;
using System.Collections.Generic;
using pigbrain.core.Collections;
using System;
using pigbrain.core.UnityObject;
using UnityEngine.AI;
using System.Collections;
using pigbrain.core.Analysis;
using pigbrain.core.Project;

namespace pigbrain.game.Boxhead.Navigation
{
    [RequireComponent(typeof(NavMeshSurface))]
    [InlineButton(nameof(Clear), nameof(Bake))]
    [DefaultExecutionOrder(-100)]
    [ProjectInterface.Control(ProjectInterface.Filter.Hierarchy, 60)]
    public class NavMap : MonoBehaviourSingleton<NavMap>
    {
        [HideInInspector][SerializeField] internal NavMeshSurface surface;
        [SerializeField][Range(1, 3)] float resolution = 1.5f;

        [Header("Generated")]
        [SerializeField][ReadOnly] string error;
        [SerializeField][ReadOnly] internal Bounds bounds;
        [SerializeField][ReadOnly] NavMapData data;

        public NavMapData GetData() => data;

        void OnValidate() { if (!surface) surface = GetComponent<NavMeshSurface>(); }

        protected override void Awake()
        {
            base.Awake();
            if (!surface) surface = GetComponent<NavMeshSurface>();
            if (data) Initialize();
        }
        void Initialize()
        {
            var layers = GetComponents<NavMapLayer>();
            foreach (var layer in layers)
                AddLayer(layer);
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            Clear();
        }

        [ProjectInterface.Button(nameof(Clear))]
        public void Clear()
        {
            data = null;
            surface.RemoveData();
            if (surface.navMeshData)
            {
#if UNITY_EDITOR
                DestroyImmediate(surface.navMeshData);
#else
                Destroy(surface.navMeshData);
#endif
                surface.navMeshData = null;
            }
        }

        #region Bake
        [ProjectInterface.Button(nameof(Bake))]
        public void Bake()
        {
            // using var _ = new ScopeBake(surface);
            Profiler.SampleAndLog("NavMesh", () => EditorBake());
            Profiler.SampleAndLog("NavMap", () => BakeInternal());
        }

        class Scope : IDisposable
        {
            readonly NavMeshObstacle[] obstacles;
            public Scope(Transform parent)
            {
                obstacles = parent.GetComponentsInChildren<NavMeshObstacle>();
                obstacles.ForEach(o => o.gameObject.SetActive(false));
            }
            void IDisposable.Dispose() => obstacles.ForEach(o => o.gameObject.SetActive(true));
        }

        #region └Editor Bake
        void EditorBake()
        {
            using var _ = new Scope(transform);
            Physics.SyncTransforms();
            BuildNavMeshSources();
            if (surface.navMeshData == null)
            {
                surface.navMeshData = new NavMeshData();
                NavMesh.AddNavMeshData(surface.navMeshData);
            }
            var settings = surface.GetBuildSettings();
            NavMeshBuilder.UpdateNavMeshData(surface.navMeshData, settings, GetNavMeshBuildSources(), bounds);
        }
        #endregion

        #region └Resync
        Coroutine resync;
        public void BakeResync()
        {
            IEnumerator Resync()
            {
                if (resync != null) yield break;
                yield return null;
                yield return RuntimeBakeResync();
                resync = null;
            }
            StartCoroutine(Resync());
        }

        public IEnumerator RuntimeBakeResync()
        {
            if (!surface || !surface.navMeshData) yield break;
            var p = Profiler.Start();
            yield return AsyncBake(GetNavMeshBuildSourcesResync());
            Profiler.StopAndLog(p, "NavMesh (Resync)");
        }
        #endregion

        #region └Runtime Bake
        public IEnumerator RuntimeBake()
        {
            using var _ = new Scope(transform);
            Physics.SyncTransforms();
            Debug.Log(">>>> Build Nav Mesh");
            {
                var p = Profiler.Start();
                BuildNavMeshSources();
                yield return AsyncBake(GetNavMeshBuildSources());
                Profiler.StopAndLog(p, "NavMesh");
            }

            yield return null;
            Debug.Log(">>>> Bake NavMap & Init layers");
            {
                var p = Profiler.Start();
                BakeInternal();
                Initialize();
                Profiler.StopAndLog(p, "NavMap");
            }
        }

        IEnumerator AsyncBake(List<NavMeshBuildSource> buildSources)
        {
            if (surface.navMeshData == null)
            {
                surface.navMeshData = new NavMeshData();
                NavMesh.AddNavMeshData(surface.navMeshData);
            }

            var settings = surface.GetBuildSettings();
            var op = NavMeshBuilder.UpdateNavMeshDataAsync(
                surface.navMeshData, settings, buildSources, bounds);
            yield return new WaitUntil(() => op.isDone);
        }
        #endregion

        #region └Build Sources
        readonly HashSet<NavMapDynamicModifier> dynamicModifiers = new();
        readonly Dictionary<Collider, NavMeshBuildSource> navMeshBuildSources = new();

        internal List<Collider> GetBuildSourceColliders() => navMeshBuildSources.Keys.ToList();
        List<NavMeshBuildSource> GetNavMeshBuildSources() => navMeshBuildSources.Values.ToList();
        void BuildNavMeshSources()
        {
            navMeshBuildSources.Clear();

            dynamicModifiers.ForEach(d => d.colliders.Clear());
            dynamicModifiers.Clear();

            var includedLayers = surface.layerMask;

            foreach (var collider in surface.GetComponentsInChildren<Collider>(true))
            {
                if (((1 << collider.gameObject.layer) & includedLayers) == 0) continue;
                if (!collider.enabled || !collider.gameObject.activeInHierarchy || collider.isTrigger) continue;

                var mod = collider.GetComponentInParent<NavMeshModifier>(true);
                if (mod && mod.ignoreFromBuild) continue;

                if (collider.GetComponentInParent<NavMeshObstacle>()) continue;

                var area = mod && mod.overrideArea && mod.isActiveAndEnabled ? mod.area : surface.defaultArea;

                var dynamicModifier = collider.GetComponentInParent<NavMapDynamicModifier>(true);
                if (dynamicModifier)
                {
                    dynamicModifiers.Add(dynamicModifier);
                    dynamicModifier.colliders.Add(collider);
                }

                // Collate bounds
                if (navMeshBuildSources.Count == 0) bounds = collider.bounds;
                else bounds.Encapsulate(collider.bounds);

                if (collider is MeshCollider mc)
                {
                    if (mc.sharedMesh == null) continue;
                    navMeshBuildSources.Add(collider, new NavMeshBuildSource
                    {
                        shape = NavMeshBuildSourceShape.Mesh,
                        sourceObject = mc.sharedMesh,
                        transform = mc.transform.localToWorldMatrix,
                        area = area
                    });
                }
                else if (collider is BoxCollider bc)
                {
                    navMeshBuildSources.Add(collider, new NavMeshBuildSource
                    {
                        shape = NavMeshBuildSourceShape.Box,
                        size = bc.size,
                        transform = bc.transform.localToWorldMatrix * Matrix4x4.TRS(bc.center, Quaternion.identity, Vector3.one),
                        area = area
                    });
                }
                else if (collider is CapsuleCollider cc)
                {
                    Quaternion rot = Quaternion.identity;
                    navMeshBuildSources.Add(collider, new NavMeshBuildSource
                    {
                        shape = NavMeshBuildSourceShape.Capsule,
                        size = new Vector3(cc.radius * 2f, cc.height, cc.radius * 2f),
                        transform = cc.transform.localToWorldMatrix * Matrix4x4.TRS(cc.center, rot, Vector3.one),
                        area = area
                    });
                }
                else throw new Exception($"Collider '{collider.GetType()}' is not supported");
            }
        }
        #endregion
        #region └Filter rebuild
        List<NavMeshBuildSource> GetNavMeshBuildSourcesResync()
        {
            // cleanup null keys (destroyed colliders)
            foreach (var k in navMeshBuildSources.Keys.Where(k => !k).ToList())
                navMeshBuildSources.Remove(k);

            // cleanup destroyed dynamic modifiers
            dynamicModifiers.RemoveWhere(dm => !dm);

            // remove colliders belonging to disabled dynamic modifiers
            var colliders = navMeshBuildSources.Keys.ToHashSet();
            foreach (var dm in dynamicModifiers)
                if (!dm.isActiveAndEnabled)
                    dm.colliders.ForEach(c => colliders.Remove(c));

            // Rebuild the sources
            return colliders
                .Where(c => c)
                .Select(c => navMeshBuildSources[c])
                .ToList();
        }
        #endregion
        #endregion


        #region └Bake Internal
        void BakeInternal()
        {
            data = ScriptableObject.CreateInstance<NavMapData>();
            data.Build(this, resolution);
        }
        #endregion

        #region Gizmos
        [ContextMenu("Show Gizmos")]
        void ToogleGizmos() => showGizmos = !showGizmos;
        [SerializeField][HideInInspector] internal bool showGizmos;
        void OnDrawGizmos() { if (showGizmos) DrawGizmos(); }
        void DrawGizmos()
        {
            if (!data || data.map == null) return;

            var cam = Camera.current;
            if (!cam) return;

            var planes = GeometryUtility.CalculateFrustumPlanes(cam);
            bool Visible(Vector3 p) => GeometryUtility
                .TestPlanesAABB(planes, new Bounds(p, Vector3.one * 0.5f));

            if (data.sampler != null)
            {
                Gizmos.color = Color.black;
                foreach (var (i1, i2) in data.sampler.edges)
                {
                    Vector3 v1 = data.sampler.vertices[i1];
                    Vector3 v2 = data.sampler.vertices[i2];
                    GizmosUtility.DrawLine(v1, v2, 0.02f);
                }
            }

            foreach (var cell in data.map.Values)
            {
                if (cell.nodes.Count > 2)
                {
                    Gizmos.color = Color.cyan;
                    foreach (var node in cell.nodes)
                        Gizmos.DrawCube(node.position, Vector3.one);
                }

                foreach (var node in cell.nodes)
                {
                    if (!Visible(node.position)) continue;
                    Vector3 Wobble() => Vector3.zero; //UnityEngine.Random.insideUnitSphere * 0.1f

                    Gizmos.color = Color.green;
                    Gizmos.DrawCube(node.position + Wobble(), Vector3.one * 0.2f);

                    Gizmos.color = Color.red;
                    foreach (var neighbor in node.connectors)
                        GizmosUtility.DrawLine(node.position + Wobble(), neighbor.position + Wobble(), 0.02f);

                }
            }
        }

        // #region Gizmos
        // void OnDrawGizmos()
        // {
        //     // SampleNavMesh();
        //     if (sampleData == null) return;

        //     Gizmos.color = Color.red.WithA(0.5f);

        //     float s1 = sampleData.spacing / 4;
        //     Vector3 s3 = (float3)sampleData.spacing / 4;

        //     Gizmos.color = Color.black;
        //     foreach (var (i1, i2) in NavMeshTriangleSampleData.Edges)
        //     {
        //         Vector3 v1 = NavMeshTriangleSampleData.Vertices[i1];
        //         Vector3 v2 = NavMeshTriangleSampleData.Vertices[i2];
        //         GizmosUtility.DrawLine(v1, v2, s1 / 8);
        //     }

        //     Vector3 Wobble(Vector3 v) => v;
        //     // v + UnityEngine.Random.insideUnitSphere * spacing / 4;

        //     Gizmos.color = Color.green.WithA(0.5f);
        //     foreach (var s in sampleData.map.Values)
        //         foreach (var n in s.nodes)
        //             foreach (var c in n.connectors)
        //                 GizmosUtility.DrawLine(Wobble(n.world), Wobble(c.world), s1 / 4);

        //     Gizmos.color = Color.red.WithA(0.5f);
        //     foreach (var s in sampleData.map.Values)
        //         foreach (var n in s.nodes)
        //             Gizmos.DrawCube(n.world, s3);
        // }
        //     #endregion

        #endregion

        #region Layer
        readonly Dictionary<Type, NavMapLayer> layers = new();
        void AddLayer(NavMapLayer layer)
        {
            var key = layer.GetType();

            if (layers.ContainsKey(key))
                Debug.LogWarning($"Layer '{key.Name}'object has already been added!");

            layers[key] = layer;
            layer.OnAddedToMap(this);
        }

        // public T GetLayer<T>() where T : NavMapLayer => (T)layers[typeof(T)];
        public static bool TryGetLayer<T>(out T layer) where T : NavMapLayer =>
            layer = Instance.layers.TryGetValue(typeof(T), out NavMapLayer llayer) ? (T)llayer : null;
        public static T TryGetLayer<T>() where T : NavMapLayer =>
            Instance && Instance.layers.TryGetValue(typeof(T), out NavMapLayer llayer) ? (T)llayer : null;
        #endregion
    }
}
