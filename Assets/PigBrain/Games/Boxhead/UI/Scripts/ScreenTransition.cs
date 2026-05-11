using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Collections;
using pigbrain.core.Inspector;
using pigbrain.core.UnityObject;
using UnityEngine;

namespace pigbrain.game.Boxhead.UI
{
    public class ScreenTransition : MonoBehaviour
    {
        [SerializeField][ToggleObject] TransitionState enter = new(Direction.Enter);
        [SerializeField][ToggleObject] TransitionState exit = new(Direction.Exit);

        public bool transitioning { get; private set; }

        #region Interface
        Vector2? anchoredPositionCache;
        Vector2 defaultAnchor => anchoredPositionCache ??= rectTransform.anchoredPosition;
        Vector3? localScaleCache;
        Vector3 defaultScale => localScaleCache ??= rectTransform.localScale;

        CanvasGroup canvasGroup;
        RectTransform rectTransform => (RectTransform)transform;

        TransitionState GetState(Direction d) => d == Direction.Enter ? this.enter : this.exit;

        public void Transition(Direction direction, Action complete = null)
        {
            TransitionState state = GetState(direction);
            if (state.direction == Direction.Enter)
            {
                transitioning = true;
                gameObject.SetActive(true);
                StartCoroutine(Run(state, false, () => complete?.Invoke()));
            }
            else if (isActiveAndEnabled)
                StartCoroutine(Run(state, false, () => { gameObject.SetActive(false); complete?.Invoke(); }));

            RunChildren(state);
        }
        #endregion

        #region  Children
        void RunChildren(TransitionState state)
        {
            if (state.traits.HasFlag(Traits.OverrideChildren)) return;
            GetComponentsInChildren<ScreenTransition>()
                .Where(c => c != this)
                .ForEach(c => c.TransitionChild(state.direction));
        }

        IEnumerator Run(TransitionState state, bool asChild = false, Action complete = null)
        {
            if (!canvasGroup) gameObject.TryAddComponent(out canvasGroup);
            ResetTransition();

            var block = asChild ? null : canvasGroup.BlockInput();
            GetTransitions(state).ForEach(e => StartCoroutine(e));
            yield return new WaitForSecondsRealtime(state.duration);
            block?.Dispose();

            canvasGroup.alpha = 1;
            Destroy(canvasGroup);
            transitioning = false;
            complete?.Invoke();
        }

        void TransitionChild(Direction direction) =>
           StartCoroutine(Run(GetState(direction), true));
        #endregion

        #region Transitions
        void ResetTransition()
        {
            rectTransform.anchoredPosition = defaultAnchor;
            rectTransform.localScale = defaultScale;
        }
        IEnumerator Transition_None(TransitionState state) { yield break; }
        IEnumerator Transition_SlideLeft(TransitionState state)
        { yield return PositionLerp(state, new(-UnityEngine.Screen.width, 0)); }
        IEnumerator Transition_SlideRight(TransitionState state)
        { yield return PositionLerp(state, new(+UnityEngine.Screen.width, 0)); }
        IEnumerator Transition_SlideTop(TransitionState state)
        { yield return PositionLerp(state, new(0, +UnityEngine.Screen.height)); }
        IEnumerator Transition_SlideBottom(TransitionState state)
        { yield return PositionLerp(state, new(0, -UnityEngine.Screen.height)); }
        IEnumerator PositionLerp(TransitionState state, Vector2 offset)
        {
            yield return new OverTimeUnscaled(state.duration, (t) => rectTransform.anchoredPosition
                = Vector2.Lerp(defaultAnchor + offset, defaultAnchor, state.Ease(t)));
        }
        IEnumerator Transition_Zoom(TransitionState state)
        {
            yield return new OverTimeUnscaled(state.duration, (t) =>
                rectTransform.localScale = Vector3.Lerp(defaultScale * 50, defaultScale, state.Ease(t)));
        }
        IEnumerator Transition_Fade(TransitionState state)
        {
            yield return new OverTimeUnscaled(state.duration, (t) =>
            { if (canvasGroup) canvasGroup.alpha = Mathf.Lerp(0f, 1f, state.Ease(t)); });
        }

        IEnumerator Transition_Hold(TransitionState state)
        {
            yield return new OverTimeUnscaled(state.duration, (t) => { });
        }
        #endregion

        #region State
        [Serializable]
        class TransitionState
        {
            [ReadOnly] public Direction direction;
            [Range(0, 2)] public float duration = 0.5f;
            public Type type;
            public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
            public Traits traits;
            public TransitionState(Direction direction) => this.direction = direction;
            public float Ease(float t)
            {
                var f = ease.Evaluate(Mathf.Clamp01(t));
                return direction == Direction.Enter ? f : 1 - f; // reverse to go backwards
            }
        }
        #endregion

        #region Types
        IEnumerable<IEnumerator> GetTransitions(TransitionState state)
        {
            return Enum.GetValues(typeof(Type)).Cast<Type>()
                .Where(t => t != Type.None && state.type.HasFlag(t))
                .Select(t => GetTransition(t, state));
        }

        IEnumerator GetTransition(Type isolatedType, TransitionState state)
        {
            switch (isolatedType)
            {
                default: return Transition_None(state);
                case Type.SlideLeft: return Transition_SlideLeft(state);
                case Type.SlideRight: return Transition_SlideRight(state);
                case Type.SlideTop: return Transition_SlideTop(state);
                case Type.SlideBottom: return Transition_SlideBottom(state);
                case Type.Fade: return Transition_Fade(state);
                case Type.Zoom: return Transition_Zoom(state);
                case Type.Hold: return Transition_Hold(state);
            }
        }

        [Flags]
        enum Type
        {
            None = 0,
            SlideLeft = 1 << 0,
            SlideRight = 1 << 1,
            SlideTop = 1 << 2,
            SlideBottom = 1 << 3,
            Fade = 1 << 4,
            Zoom = 1 << 5,
            Hold = 1 << 6,
        }

        [Flags]
        enum Traits
        {
            None = 0,
            OverrideChildren = 1 << 0,
        }
        public enum Direction { Enter = 0, Exit = 1, }
        #endregion

    }
}