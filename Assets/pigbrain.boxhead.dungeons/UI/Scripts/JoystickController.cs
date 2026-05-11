using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;
using pigbrain.core.Collections;
using System.Collections;

namespace pigbrain.game.Boxhead.UI
{
    public class JoystickController : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] float inactiveAlpha = 0.2f;
        [SerializeField] RectTransform positional;
        [SerializeField] OnScreenStick stick;

        void OnEnable()
        {
            Debug.Log("OnEnable");
            positional.gameObject.SetActive(false);
        }

        void OnDisable()
        {
            Debug.Log("OnDisable");
            positional.gameObject.SetActive(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!positional.gameObject.activeSelf)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(positional.parent as RectTransform,
                    eventData.position, eventData.pressEventCamera, out var localPoint);
                positional.localPosition = localPoint;
                positional.gameObject.SetActive(true);
            }
            eventData.pointerDrag = stick.gameObject;
            ExecuteEvents.Execute(stick.gameObject, eventData, ExecuteEvents.pointerDownHandler);
        }

        void Update()
        {
            if (positional.gameObject.activeSelf && !Pointer.current.press.isPressed)
                positional.gameObject.SetActive(false);
        }
    }
}