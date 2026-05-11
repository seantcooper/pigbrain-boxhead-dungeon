using System;
using TMPro;
using UnityEngine;

namespace pigbrain.Generated.TickerWorld
{
    [Serializable]
    public class PrefabView : MonoBehaviour
    {
        public TextTMPView textTMP;

        [Serializable]
        public class TextTMPView
        {
            public RectTransform transform;
            public MeshRenderer meshRenderer;
            public TextMeshPro text;
        }
    }
}