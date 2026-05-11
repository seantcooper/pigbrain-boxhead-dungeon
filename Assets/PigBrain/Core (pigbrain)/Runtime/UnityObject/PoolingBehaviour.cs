#pragma warning disable UDR0001
using System;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Inspector;
using UnityEngine;
using UnityEngine.AI;

namespace pigbrain.core.UnityObject
{
    public static class PoolingMessages
    {
        public const string Start = nameof(IPoolingStart.OnPoolingStart);
        public const string Destroy = nameof(IPoolingDestroy.OnPoolingDestroy);
        public const string Reset = nameof(IPoolingReset.OnPoolingReset);
    }

    public class PoolBehaviour : MonoBehaviour
    {
        protected virtual void PoolStart() => BroadcastMessage(PoolingMessages.Start, SendMessageOptions.DontRequireReceiver);
        protected virtual void PoolDestroy() => BroadcastMessage(PoolingMessages.Destroy, SendMessageOptions.DontRequireReceiver);
        protected virtual void PoolReset()
        {
            foreach (var o in objectResetters) o.Reset();
            foreach (var p in poolingReset) p.Reset();
            BroadcastMessage(PoolingMessages.Reset, SendMessageOptions.DontRequireReceiver);
            StopAllCoroutines();
            CancelInvoke();
            StopAllCoroutines();
        }

        protected List<ResetObject> objectResetters;
        protected IPooling[] poolingReset;

        protected virtual void Awake() => CreateResetObjects();
        void CreateResetObjects()
        {
            objectResetters = new List<ResetObject>();
            objectResetters.AddRange(GetComponentsInChildren<TrailRenderer>(true).Select(o => new ResetTrailRenderer(o)));
            objectResetters.AddRange(GetComponentsInChildren<ParticleSystem>(true).Select(o => new ResetParticleSystem(o)));
            objectResetters.AddRange(GetComponentsInChildren<NavMeshAgent>(true).Select(o => new ResetNavMeshAgent(o)));
            poolingReset =
                GetComponentsInChildren<MonoBehaviour>(true)
                .OfType<IPooling>()
                .ToArray();
        }
    }

    public interface IPooling { void Reset(); }
    public interface IPoolingStart { void OnPoolingStart(); }
    public interface IPoolingDestroy { void OnPoolingDestroy(); }
    public interface IPoolingReset { void OnPoolingReset(); }

    public class PoolBehaviour<T> : PoolBehaviour where T : PoolBehaviour
    {
        [Header("Pooling")]
        [SerializeField][ReadOnly] int containerID;
        protected virtual void OnValidate() => containerID = this.GetUniqueID();
        protected virtual T Create() => this.Instantiate() as T;

        public event Action<PoolBehaviour> onPutBack;

        protected T Get(Vector3 position, Quaternion rotation, Action<T> assignment = null) =>
            Get((inst) =>
            {
                inst.transform.SetPositionAndRotation(position, rotation);
                assignment?.Invoke(inst);
            });

        protected T Get(Action<T> assignment)
        {
            var container = GetContainer();
            if (container.Get(out T instance))
            {
                assignment?.Invoke(instance);
                instance.gameObject.SetActive(true);
                (instance as PoolBehaviour<T>).PoolReset();
            }
            else
            {
                instance = container.Add(Create());
                assignment?.Invoke(instance);
                (instance as PoolBehaviour<T>).containerID = containerID;
            }
            (instance as PoolBehaviour<T>).PoolStart();
            return instance;
        }

        public void Put(float duration) => Invoke(nameof(Put), duration);
        public void Put()
        {
            if (!gameObject.activeSelf) return;
            GetContainer().Put(this);
            PoolDestroy();
            gameObject.SetActive(false);
            PoolReset();
            onPutBack?.Invoke(this);
        }

        static PoolingContainer RootContainer => PoolingContainer.TryCreateInstance();

        PoolContainer GetContainer()
        {
            if (containerID == 0) containerID = this.GetUniqueID();
            if (RootContainer.pooling.TryGetValue(containerID, out PoolContainer container)) return container;
            return RootContainer.pooling[containerID] =
                new PoolContainer($"{name} ({typeof(T).Name})");
        }
    }

    #region Reset
    [Serializable]
    public class ResetObject
    {
        [NonSerialized] protected Component target;
        public ResetObject(Component target) => this.target = target;
        public void SetTarget(Component target) => this.target = target;
        public virtual void Reset() { }
    }

    [Serializable]
    public class ResetTrailRenderer : ResetObject
    {
        public ResetTrailRenderer(TrailRenderer target) : base(target)
        {
            this.target = target;
        }
        public override void Reset() => (target as TrailRenderer).Clear();
    }

    [Serializable]
    public class ResetParticleSystem : ResetObject
    {
        public ResetParticleSystem(ParticleSystem target) : base(target)
        {
            this.target = target;
        }
        public override void Reset()
        {
            var ps = target as ParticleSystem;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Clear(true);
            ps.Simulate(0f, true, true, true);
        }
    }

    [Serializable]
    public class ResetNavMeshAgent : ResetObject
    {
        public ResetNavMeshAgent(NavMeshAgent target) : base(target)
        {
            this.target = target;
        }
        public override void Reset()
        {
            var agent = target as NavMeshAgent;
            agent.Warp(agent.transform.position);
            // agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
    }

    // public interface IResetObject { void Reset(); }
    #endregion
}

#if UNITY_EDITOR
namespace pigbrain.core.UnityObject
{
    using pigbrain.core.Geom;
    using UnityEditor;
    using static pigbrain.core.Inspector.InspectorUtility;

    [CustomPropertyDrawer(typeof(ResetObject), true)]
    public class ResetObject_Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var target = property.FindPropertyRelative("target");
            EditorGUI.PropertyField(position.WithH(LineHeight), target, GUIContent.none);
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            FullLineHeight;
    }
}
#endif
