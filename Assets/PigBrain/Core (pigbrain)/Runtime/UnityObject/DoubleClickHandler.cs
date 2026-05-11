using System;
using UnityEngine;

namespace pigbrain.core.UnityObject
{
    public class DoubleClickHandler : MonoBehaviour
    {
        public event Action OnDoubleClick;
        [SerializeField] float threshold = 0.3f;

        float lastClickTime;

        void OnMouseDown()
        {
            if (Time.time - lastClickTime < threshold)
            {
                OnDoubleClick?.Invoke();
                lastClickTime = 0; // reset
            }
            else
            {
                lastClickTime = Time.time;
            }
        }
    }
}
