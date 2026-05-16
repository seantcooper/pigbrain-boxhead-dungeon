using System;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Collections;
using pigbrain.core.Inspector;
using pigbrain.core.Statistics;
using pigbrain.core.UnityObject;
using pigbrain.game.Boxhead.Environment;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static pigbrain.game.Boxhead.UI.Console;

namespace pigbrain.game.Boxhead.UI
{
    public class ButtonTextAlpha : MonoBehaviour
    {
        [SerializeField] Selectable selectable;
        [SerializeField] TMP_Text text;

        void OnEnable() => UpdateAlpha();
        void OnCanvasGroupChanged() => UpdateAlpha();

        void Update()
        {
            if (!Application.isPlaying) UpdateAlpha();
        }

        void UpdateAlpha()
        {
            var c = text.color;
            c.a = selectable.targetGraphic.color.a;
            text.color = c;
        }
    }
}