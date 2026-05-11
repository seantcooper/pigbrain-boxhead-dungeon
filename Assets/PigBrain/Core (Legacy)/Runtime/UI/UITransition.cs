// using System;
// using PigBrain.LegacyCore.Utility;
// using DG.Tweening;
// using UnityEngine;

// namespace PigBrain.LegacyCore.UI
// {
//     public class UITransition : MonoBehaviour
//     {
//         protected const float Duration = 0.25f;

//         protected RectTransform rectTransform;

//         public virtual void Awake()
//         {
//             rectTransform = GetComponent<RectTransform>();
//         }

//         protected virtual void DoTransition(Transform parent, Direction direction, float duration = Duration, Action onComplete = null) { }

//         public static void TransitionIn(Transform parent, float duration = Duration, Action onStart = null, Action onComplete = null) =>
//             DOAnchorTransitions(parent, duration, Direction.In, Ease.InQuad, onStart, onComplete);

//         public static void TransitionOut(Transform parent, float duration = Duration, Action onStart = null, Action onComplete = null) =>
//             DOAnchorTransitions(parent, duration, Direction.Out, Ease.OutQuad, onStart, onComplete);

//         static void DOAnchorTransitions(Transform parent, float duration, Direction direction, Ease ease, Action onStart = null, Action onComplete = null)
//         {
//             onStart?.Invoke();

//             UITransitionAnchor[] transitions = parent.GetComponentsInChildren<UITransitionAnchor>();
//             if (transitions == null || transitions.Length == 0)
//             {
//                 onComplete?.Invoke();
//                 return;
//             }

//             int completed = transitions.Length;
//             transitions.ForEach(t => t.DoTransition(parent, direction, duration,
//                 () => { if (--completed == 0) onComplete?.Invoke(); }));
//         }

//         protected enum Direction { In, Out }

//     }
// }

