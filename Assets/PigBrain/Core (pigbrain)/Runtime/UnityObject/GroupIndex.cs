using System;
using pigbrain.core.Collections;
using UnityEngine;

namespace pigbrain.core.UnityObject
{
    public class GroupIndex : MonoBehaviour
    {
        public GameObject[] gameObjects;
        [SerializeField] int index;

        void OnValidate()
        {
            SetIndex(index);
        }

        public void SetIndex(int index)
        {
            if (gameObjects.IsNullOrEmpty()) return;
            index = Math.Clamp(index, 0, gameObjects.Length - 1);
            for (int i = 0; i < gameObjects.Length; i++)
                gameObjects[i].SetActive(i == index);
        }
    }
}
