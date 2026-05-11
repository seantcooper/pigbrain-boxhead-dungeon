using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace pigbrain.generated.ItemCardbase
{
    [Serializable]
    public class PrefabView : MonoBehaviour
    {
        public BackgroundView background;
        public ContentView content;

        [Serializable]
        public class BackgroundView
        {
            public RectTransform transform;
            public Image image;
        }

        [Serializable]
        public class ContentView
        {
            public RectTransform transform;
            public Image image;
            public PanelIconView panelIcon;
            public PanelTitleView panelTitle;
            public PanelStatsView panelStats;
            public PanelDescriptionView panelDescription;

            [Serializable]
            public class PanelIconView
            {
                public RectTransform transform;
                public Image image;
            }

            [Serializable]
            public class PanelTitleView
            {
                public RectTransform transform;
                public Image image;
                public TextTitleView textTitle;

                [Serializable]
                public class TextTitleView
                {
                    public RectTransform transform;
                    public TextMeshProUGUI text;
                }
            }

            [Serializable]
            public class PanelStatsView
            {
                public RectTransform transform;
                public Image image;
                public TextStatsView textStats;

                [Serializable]
                public class TextStatsView
                {
                    public RectTransform transform;
                    public TextMeshProUGUI text;
                }
            }

            [Serializable]
            public class PanelDescriptionView
            {
                public RectTransform transform;
                public Image image;
                public TextDescriptionView textDescription;

                [Serializable]
                public class TextDescriptionView
                {
                    public RectTransform transform;
                    public TextMeshProUGUI text;
                }
            }
        }
    }
}