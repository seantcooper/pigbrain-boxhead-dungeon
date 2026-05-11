using UnityEngine;
using pigbrain.core.Geom;

namespace pigbrain.core.Inspector
{
    public class HeaderLineAttribute : PropertyAttribute
    {
        public readonly string title;
        public readonly Color color;

        public HeaderLineAttribute(string title, string color = null)
        {
            this.title = title;
            ColorUtility.TryParseHtmlString(string.IsNullOrEmpty(color) ? ColorHex.grey : color, out this.color);
            this.color = this.color.WithA(0.33f);
        }
    }
}

#if UNITY_EDITOR
namespace pigbrain.core.Inspector
{
    using UnityEditor;
    using static pigbrain.core.Inspector.InspectorUtility;

    [CustomPropertyDrawer(typeof(HeaderLineAttribute))]
    public class HeaderLine_Drawer : DecoratorDrawer
    {
        public override void OnGUI(Rect position)
        {
            var attr = (HeaderLineAttribute)attribute;
            DrawHeader(position.WithH(HeaderHeight), attr.title, attr.color);
        }

        public override float GetHeight() => FullHeaderHeight;
    }
}
#endif