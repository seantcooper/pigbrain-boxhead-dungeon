// using System;
// using UnityEngine;

// namespace PigBrain.LegacyCore.FiniteStateMachine
// {
//     public class State : MonoBehaviour
//     {
//         internal StateMachine stateMachine;
//         protected float enterTime;
//         protected float time => Time.time - enterTime;
//         protected float deltaTime => Time.deltaTime;

//         State exitState;

//         public void ExitState() => stateMachine.ExitCurrentState();
//         // public void SetState<T>() where T : State => stateMachine.SetState<T>();

//         void Update() => Execute();

//         public virtual void Execute() { }
//         public virtual void Enter()
//         {
//             enabled = true;
//             enterTime = Time.time;
//         }

//         public virtual void Exit()
//         {
//             enabled = false;
//         }
//     }
// }

