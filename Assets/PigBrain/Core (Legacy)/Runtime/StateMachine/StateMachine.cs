// using System;
// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine;

// namespace PigBrain.LegacyCore.FiniteStateMachine
// {
//     public class StateMachine : MonoBehaviour
//     {
//         [Header("Runtime")]
//         public List<State> states;
//         public State state;

//         void OnValidate() => states = GetComponents<State>().ToList();

//         internal virtual void Update() { }
//         internal virtual void Start()
//         {
//             state = null;
//             states = GetComponents<State>().ToList();
//             states.ForEach(s =>
//             {
//                 s.enabled = false;
//                 s.stateMachine = this;
//             });
//         }

//         internal virtual void ExitCurrentState()
//         {
//             state.Exit();
//             state = null;
//         }

//         public T GetState<T>() where T : State => (T)GetState(typeof(T));
//         public State GetState(Type type) => states.FirstOrDefault(s => s.GetType() == type);

//         public T SetState<T>(Action<T> initialize = null) where T : State
//         {
//             if (this.state) this.state.Exit();
//             T newState = GetState<T>();
//             this.state = newState;
//             if (newState)
//             {
//                 initialize?.Invoke(newState);
//                 this.state.Enter();
//             }
//             return (T)this.state;
//         }
//     }
// }
