// using System;
// using DG.Tweening;
// using UnityEngine;

// namespace PigBrain.LegacyCore.UI
// {
//     public class UITransitionAnchor : UITransition
//     {
//         const float OffScreen = 600;
//         [SerializeField] Transition transition;
//         Vector2 anchorPositionDefault;

//         public override void Awake()
//         {
//             base.Awake();
//             anchorPositionDefault = rectTransform.anchoredPosition;
//         }

//         Vector2 anchorPositionOffScreen => transition switch
//         {
//             Transition.Left => anchorPositionDefault + Vector2.left * OffScreen,
//             Transition.Right => anchorPositionDefault + Vector2.right * OffScreen,
//             Transition.Top => anchorPositionDefault + Vector2.up * OffScreen,
//             Transition.Bottom => anchorPositionDefault + Vector2.down * OffScreen,
//             _ => throw new Exception($"Unknown transition {transition}"),
//         };

//         protected override void DoTransition(Transform parent, Direction direction, float duration = Duration, Action onComplete = null)
//         {
//             Ease ease = direction == Direction.Out ? Ease.OutQuad : Ease.InQuad;

//             (Vector2 start, Vector2 end) = direction == Direction.Out ? (anchorPositionDefault, anchorPositionOffScreen)
//                 : (anchorPositionOffScreen, anchorPositionDefault);

//             rectTransform.anchoredPosition = start;
//             rectTransform.DOAnchorPos(end, duration)
//                 .SetEase(ease)
//                 .SetUpdate(true)
//                 .OnComplete(() => onComplete?.Invoke());
//         }

//         protected enum Transition
//         { Left, Right, Top, Bottom, }
//     }
// }

