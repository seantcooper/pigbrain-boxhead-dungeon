using System.Collections.Generic;
using System.Linq;
using pigbrain.core.UnityObject;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace pigbrain.game.Boxhead.Creator
{
    public class Cell : MonoBehaviour
    {
        [SerializeField] Image icon;

        public Cell CreateInstance(Row row, int vcount = 0)
        {
            Cell cell = this.PrefabInstantiate(row.container.transform);
            var values = cell.values.ToArray();
            values[0].SetActive(vcount > 0);
            values[1].SetActive(vcount > 1);
            return cell;
        }

        public float GetHeight()
        {
            var layout = GetComponent<VerticalLayoutGroup>();
            if (!layout) return ((RectTransform)transform).rect.height;

            float height = layout.padding.top + layout.padding.bottom;
            int active = 0;

            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i) as RectTransform;
                if (!child || !child.gameObject.activeSelf) continue;

                var element = child.GetComponent<LayoutElement>();
                float childHeight = element && element.preferredHeight >= 0
                    ? element.preferredHeight
                    : child.rect.height;

                height += childHeight;
                active++;
            }

            if (active > 1) height += layout.spacing * (active - 1);
            return height;
        }

        public Value[] values => transform.GetComponentsInChildren<Value>();
    }
}