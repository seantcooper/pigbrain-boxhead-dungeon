using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using pigbrain.core.Analysis;
using pigbrain.core.Collections;
using pigbrain.core.Inspector;
using pigbrain.core.Utility;
using UnityEngine;

namespace pigbrain.game.Boxhead.FiniteStateMachine
{
    public abstract class FSM : MonoBehaviour
    {
        static bool IsQuitting;
        [Range(0, 0.5f)] public float response = 0.5f;
        [Range(0, 5)] public float timeScale = 1f;
        [SerializeField][ReadOnly] string currentState;
        [SerializeField][ReadOnly] int instanceID;
        [SerializeField][ReadOnly] internal bool markedAsDying;

        BaseState next;
        protected float startTime;
        protected BaseState startState;

        protected virtual void OnValidate() =>
            declaredStates.ForEach(s => { s.OnValidate(this); });

        public event Action onChangeState;

        protected void StartFSM(BaseState start)
        {
            startTime = Time.time;
            declaredStates.ForEach(s => { s.Awake(); });
            Interrupt(startState = start);
            SetTimeScale(timeScale);
        }

        void OnApplicationQuit() => IsQuitting = true;
        void OnDestroy() { if (!IsQuitting) currentScope?.Dispose(); }

        public virtual void SetTimeScale(float timeScale) =>
            this.timeScale = timeScale;

        #region Set
        BaseState CheckState(BaseState state)
        {
#if UNITY_EDITOR
            for (Type type = state.GetType(); type != null; type = type.BaseType)
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SubState<>))
                    throw new Exception($"'{state}' is a SubState and cannot be set!");
#endif
            return state;
        }

        public float stateTime => currentScope ? Time.time - currentScope.startTime : 0;
        public float time => Time.time - startTime;

        protected void Set(BaseState state) => next = CheckState(state);
        public bool TrySet(BaseState state)
        {
            CheckState(state);
            if (currentScope)
            {
                if (currentScope.state.locked) return false;
                if (currentScope.state == state) return false;
            }
            if (!state.CanSet()) return false;
            Set(state);
            return true;
        }

        internal void SetCurrentState(BaseState state)
        {
            this.LogMessage(currentState = $"{state}");
            onChangeState?.Invoke();
        }
        #endregion

        #region Interrupt
        public void Interrupt(BaseState state)
        {
            CheckState(state);
            if (currentScope && currentScope.state == state)
            {
                Debug.LogWarning($"Setting the same state '{state}'");
                return;
            }

            if (currentScope && currentScope.state.locked)
            {
                Debug.LogError($"Current Scope is locked '{currentScope.state}' set: '{state}'");
                return;
            }

            if (!state.CanSet())
            {
                Debug.LogWarning($"State can not be set '{state}'");
                return;
            }

            StopAllCoroutines();
            currentScope?.Dispose();
            currentScope = null;

            next = state;
            StartCoroutine(Run());
        }
        #endregion

        #region Run
        BaseState.Scope currentScope;
        IEnumerator Run()
        {
            while (next != null)
            {
                currentScope = new BaseState.Scope(next);

                next = null;

                SetCurrentState(currentScope.state);

                var (hold, run) = currentScope.state.Implements();
                if (hold) yield return currentScope.state.Hold().RunImmediate();

                // state run until next is set
                while (next == null)
                {
                    if (run) yield return currentScope.state.Run().RunImmediate();
                    if (next == null) yield return WaitForSeconds(response);
                }

                yield return null;
                currentScope.Dispose();
                currentScope = null;
            }

            if (next == null) throw new Exception("FSM no next state?");
        }
        #endregion

        #region Time Scale
        public IEnumerator WaitForSeconds(float time)
        {
            yield return new WaitForSeconds(time / timeScale);
        }
        #endregion

        #region State
        [Serializable]
        public class BaseState
        {
            [SerializeField][HideInInspector] protected FSM ufsm;
            public bool enabled = false;
            public bool hasStarted { get; internal set; }
            public virtual bool locked => false;
            public virtual bool hold => false;
            public virtual bool CanSet() => enabled;
            public virtual void Awake() { }
            public virtual void Start() { }
            public virtual void OnValidate(FSM fsm) => ufsm = fsm;
            public virtual void Enter() { }
            public virtual void Cancel() => throw new NotImplementedException("Cancel not implemented!");
            public virtual IEnumerator Hold() { yield return null; }
            public virtual IEnumerator Run() { yield return null; }
            public virtual void Exit() { }
            // public virtual void OnDestroy() { }

            public virtual void SetState(BaseState next) => ufsm.Set(next);
            public virtual bool TrySetState(BaseState next) => ufsm.TrySet(next);

            internal class Scope : IDisposable
            {
                public BaseState state;
                public float startTime;
                public Scope(BaseState state)
                {
                    this.state = state;
                    startTime = Time.time;
                    if (!state.hasStarted)
                    {
                        state.Start();
                        state.hasStarted = true;
                    }
                    state.Enter();
                }
                public void Dispose() =>
                    state?.Exit();

                public static implicit operator bool(Scope empty) => empty != null;
            }
            public override string ToString() => GetType().Name;

            static readonly Dictionary<Type, (bool hold, bool run)> CacheImplements = new();
            internal (bool hold, bool run) Implements()
            {
                var t = GetType();
                if (CacheImplements.TryGetValue(t, out var v)) return v;
                bool hold = t.GetMethod(nameof(Hold)).DeclaringType != typeof(BaseState);
                bool run = t.GetMethod(nameof(Run)).DeclaringType != typeof(BaseState);
                return CacheImplements[t] = (hold, run);
            }
        }

        [Serializable]
        public class State<T> : BaseState where T : FSM
        {
            [HideInInspector] public T fsm => (T)ufsm;
            public GameObject gameObject => fsm.gameObject;
            public Transform transform => fsm.transform;
        }

        [Serializable]
        public class SubState<T> : BaseState where T : FSM
        {
            [HideInInspector] public T fsm => (T)ufsm;
            public GameObject gameObject => fsm.gameObject;
            public Transform transform => fsm.transform;
            public override void SetState(BaseState next) =>
                throw new Exception("SubState are not set! Use yield return RunState()");
            public override bool TrySetState(BaseState next) =>
                throw new Exception("SubState are not set! Use yield return Use RunState()");
            public IEnumerator RunState()
            {
                using (Scope scope = new(this))
                {
                    fsm.SetCurrentState(this);
                    yield return scope.state.Hold().RunImmediate();
                    yield return Run();
                }
            }
            protected void Stop()
            {

            }
        }
        #endregion

        readonly static Type baseType = typeof(BaseState);
        static readonly Dictionary<Type, FieldInfo[]> declaredStateCache = new();
        Type fsmType;
        BaseState[] declaredStatesCached = null;
        BaseState[] declaredStates => declaredStatesCached ??=
            GetDeclaredStatesFields().Select(f => (BaseState)f.GetValue(this))
                .Where(s => s != null).ToArray();
        public BaseState[] GetDeclaredStates() => declaredStates;

        FieldInfo[] GetDeclaredStatesFields()
        {
            fsmType ??= GetType();
            if (!declaredStateCache.TryGetValue(fsmType, out FieldInfo[] fields))
                declaredStateCache.Add(fsmType, fields = GetType()
                    .GetFields(ReflectionUtility.DefaultBindings)
                    .Where(f => baseType.IsAssignableFrom(f.FieldType) && f.FieldType != baseType)
                    .ToArray());
            return fields;
        }
    }
}