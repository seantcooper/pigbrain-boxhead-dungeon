using pigbrain.core.UnityObject;
using pigbrain.generated.WeaponIconBase;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace pigbrain.generated.WeaponIconBase
{
    [Serializable]
    public class PrefabView : MonoBehaviour
    {
        public PrefabView prefabView;
        public CanvasGroup canvasGroup;
        public OutlineView outline;
        public LevelsView levels;
        public ContentView content;
        public ButtonsView buttons;

        [Serializable]
        public class OutlineView
        {
            public RectTransform transform;
            public Image image;
        }

        [Serializable]
        public class LevelsView
        {
            public RectTransform transform;
            public GroupIndex groupIndex;
            public Level1View level1;
            public Level2View level2;
            public Level3View level3;
            public Level4View level4;
            public Level5View level5;

            [Serializable]
            public class Level1View
            {
                public RectTransform transform;
                public Image image;
                public RectMask2D rectMask2D;
                public TextTMPView textTMP;

                [Serializable]
                public class TextTMPView
                {
                    public RectTransform transform;
                    public TextMeshProUGUI text;
                }
            }

            [Serializable]
            public class Level2View
            {
                public RectTransform transform;
                public Image image;
                public RectMask2D rectMask2D;
                public TextTMPView textTMP;

                [Serializable]
                public class TextTMPView
                {
                    public RectTransform transform;
                    public TextMeshProUGUI text;
                }
            }

            [Serializable]
            public class Level3View
            {
                public RectTransform transform;
                public Image image;
                public RectMask2D rectMask2D;
                public TextTMPView textTMP;

                [Serializable]
                public class TextTMPView
                {
                    public RectTransform transform;
                    public TextMeshProUGUI text;
                }
            }

            [Serializable]
            public class Level4View
            {
                public RectTransform transform;
                public Image image;
                public RectMask2D rectMask2D;
                public TextTMPView textTMP;

                [Serializable]
                public class TextTMPView
                {
                    public RectTransform transform;
                    public TextMeshProUGUI text;
                }
            }

            [Serializable]
            public class Level5View
            {
                public RectTransform transform;
                public Image image;
                public RectMask2D rectMask2D;
                public TextTMPView textTMP;

                [Serializable]
                public class TextTMPView
                {
                    public RectTransform transform;
                    public TextMeshProUGUI text;
                }
            }
        }

        [Serializable]
        public class ContentView
        {
            public RectTransform transform;
            public Image image;
            public ReloadView reload;
            public IconView icon;
            public TitleView title;
            public AmmoView ammo;
            public AmmoInfinityView ammoInfinity;

            [Serializable]
            public class ReloadView
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

            [Serializable]
            public class TitleView
            {
                public RectTransform transform;
                public TextMeshProUGUI text;
            }

            [Serializable]
            public class AmmoView
            {
                public RectTransform transform;
                public TextMeshProUGUI text;
            }

            [Serializable]
            public class AmmoInfinityView
            {
                public RectTransform transform;
                public Image image;
            }
        }

        [Serializable]
        public class ButtonsView
        {
            public RectTransform transform;
            public Image image;
            public ButtonAddView buttonAdd;
            public ButtonRemoveView buttonRemove;

            [Serializable]
            public class ButtonAddView
            {
                public RectTransform transform;
                public Image image;
                public Button button;
            }

            [Serializable]
            public class ButtonRemoveView
            {
                public RectTransform transform;
                public Image image;
                public Button button;
            }
        }
    }
}