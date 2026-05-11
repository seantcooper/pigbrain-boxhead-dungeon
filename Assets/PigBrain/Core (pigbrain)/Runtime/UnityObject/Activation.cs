using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Collections;
using UnityEngine;


namespace pigbrain.core.UnityObject
{
    public class Activation : MonoBehaviour
    {
        [SerializeField] GameObject[] targets;
        [SerializeField] bool state = true;
        void Awake() => SetState();
        public bool active { get => state; set { state = value; SetState(); } }
        public void SetState()
        {
            foreach (var target in targets)
                target.SetActive(state);
        }
    }
}