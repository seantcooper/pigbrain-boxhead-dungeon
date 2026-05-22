using System.Collections.Generic;
using System.Linq;
using pigbrain.core.UnityObject;
using TMPro;
using UnityEngine;

namespace pigbrain.game.Boxhead.Creator
{
    public class Row : MonoBehaviour
    {
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text subtitle;
        [SerializeField] internal RectTransform container;

        public Row CreateInstance(Transform container, string title, string subtitle)
        {
            Row row = this.PrefabInstantiate(container);
            row.title.text = title;
            row.subtitle.text = subtitle;
            return row;
        }

        public void Resize()
        {
            if (container.childCount == 0) return;
            float height = cells.Max(c => c.GetHeight());
            ((RectTransform)transform).SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical, height);
        }

        public Cell[] cells => container.GetComponentsInChildren<Cell>();

    }
}