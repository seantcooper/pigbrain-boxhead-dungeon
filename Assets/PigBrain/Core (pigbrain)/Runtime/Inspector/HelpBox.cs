using System;
using UnityEngine;

namespace pigbrain.core.Inspector
{
    [AttributeUsage(AttributeTargets.Field)]
    public class HelpBoxAttribute : PropertyAttribute
    {
        public string text;
        public Type type;
        public HelpBoxAttribute(string text, Type type = Type.Info)
        {
            this.text = text;
            this.type = type;
        }
        public enum Type { None, Info, Warning, Error }
    }
}

#if UNITY_EDITOR
namespace pigbrain.core.Inspector
{
    using UnityEditor;
    [CustomPropertyDrawer(typeof(HelpBoxAttribute))]
    public class HelpBoxDecoratorDrawer : DecoratorDrawer
    {

        new HelpBoxAttribute attribute => (HelpBoxAttribute)this.attribute;
        static readonly GUIStyle Style = GetStyle();
        public override float GetHeight()
        {
            var height = Style.CalcHeight(new GUIContent(attribute.text), EditorGUIUtility.currentViewWidth) + 8;
            var size = Style.CalcSize(new GUIContent(attribute.text));
            return height;
        }

        public override void OnGUI(Rect position)
        {
            if (attribute == null) return;
            // EditorGUI.HelpBox(position, Attribute.description, attribute.type);
            GUI.Label(position, attribute.text, Style);
        }

        static GUIStyle GetStyle()
        {
            return new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Italic,
                fontSize = 12,
                normal = { textColor = Color.grey },
                wordWrap = true,
                padding = new RectOffset(0, 0, 0, 4),
                alignment = TextAnchor.LowerLeft,
            };
        }
    }
}
#endif