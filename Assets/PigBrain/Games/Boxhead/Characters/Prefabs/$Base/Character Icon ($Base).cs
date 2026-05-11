using System;
using UnityEngine;
using UnityEngine.UI;

namespace pigbrain.Generated.CharacterIconBase
{
    [Serializable]
    public class PrefabView : MonoBehaviour
    {
        public BackgroundView background;
        public IconView icon;

        [Serializable]
        public class BackgroundView
        {
            public RectTransform transform;
            public Image image;
        }

        [Serializable]
        public class IconView
        {
            public RectTransform transform;
            public Image image;
        }
    }
}