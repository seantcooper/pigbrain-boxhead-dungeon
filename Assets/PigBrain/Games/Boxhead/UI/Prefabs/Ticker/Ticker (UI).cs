using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace pigbrain.Generated.TickerUI
{
    [Serializable]
    public class PrefabView : MonoBehaviour
    {
        public ImageIconView imageIcon;
        public TextTickerView textTicker;

        [Serializable]
        public class ImageIconView
        {
            public RectTransform transform;
            public Image image;
        }

        [Serializable]
        public class TextTickerView
        {
            public RectTransform transform;
            public TextMeshProUGUI text;
        }
    }
}