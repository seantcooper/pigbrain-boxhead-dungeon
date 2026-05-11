using System;
using System.Collections;
using pigbrain.core.UnityObject;
using UnityEngine;

namespace pigbrain.game.Boxhead.UI
{
    public class TutorialObject : MonoBehaviour
    {
        [SerializeField][Range(0, 30)] float duration = 10;
        [SerializeField] string id;
        public void SetID(string id) => this.id = id;
        void Start() => Destroy(gameObject, duration);
    }
}
