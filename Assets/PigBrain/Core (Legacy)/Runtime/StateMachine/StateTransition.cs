// using System;
// using UnityEngine;

// namespace PigBrain.Core.FiniteStateMachine
// {
//     public class StateTransition
//     {
//         private readonly StateMachine machine;

//         public StateTransition(StateMachine machine)
//         {
//             this.machine = machine;
//         }

//         public StateTransition Then(Action continuation)
//         {
//             continuation?.Invoke();
//             return this;
//         }

//         public StateTransition ThenSet<T>() where T : State
//         {
//             machine.SetState<T>();
//             return this;
//         }
//     }
// }