#pragma warning disable UDR0001
using System;
using UnityEngine;

namespace pigbrain.core.Inspector
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct)]
    public class ToggleObjectAttribute : PropertyAttribute
    {
        internal readonly string header;
        internal readonly string group;
        public ToggleObjectAttribute(string header = null, string group = null)
        {
            this.header = header;
            this.group = group;
        }
    }
}

#if UNITY_EDITOR
namespace pigbrain.core.Inspector
{
    using UnityEditor;
    using System.Reflection;
    using static UnityEditor.EditorGUI;
    using pigbrain.core.Geom;
    using static pigbrain.core.Inspector.InspectorUtility;

    [CustomPropertyDrawer(typeof(ToggleObjectAttribute), true)]
    public class ToggleObjectAttribute_PropertyDrawer : PropertyDrawer
    {
        new ToggleObjectAttribute attribute => base.attribute as ToggleObjectAttribute;
        bool hasHeader => !string.IsNullOrEmpty(attribute.header);

        const float Indent = 15, RIndent = 2;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (hasHeader)
            {
                GUI.Label(position.WithH(HeaderHeight), attribute.header, EditorStyles.boldLabel);
                position = position.AddY(HeaderHeight + 2);
            }

            if (property.isExpanded)
                DrawRect(position.AddMinX(Indent).AddMaxX(-RIndent).AddMinY(LineHeight), new(1, 1, 1, 0.05f));

            BeginProperty(position, label, property);
            SerializedProperty enabledProp = property.FindPropertyRelative("enabled");

            Rect p = position.WithH(LineHeight);

            if (enabledProp != null)
            {
                Rect enableRect = p.AddX(3).WithW(LineHeight);

                BeginChangeCheck();
                PropertyField(enableRect, enabledProp, GUIContent.none);
                if (EndChangeCheck() && enabledProp.boolValue)
                    DisableOthers(property);
                if (GUI.Button(p, label, ButtonLabel1)) property.isExpanded = !property.isExpanded;
                Toggle(enableRect, enabledProp.boolValue);
            }
            else
            {
                if (GUI.Button(p, label, ButtonLabel2)) property.isExpanded = !property.isExpanded;
            }

            if (property.isExpanded)
            {
                using (new IndentLevelScope(indentLevel + 1))
                    property.IterateToEndDraw(p.AddMinX(Indent).AddMaxX(-(RIndent + 2)).AddY(FullLineHeight), (r, p) =>
                        PropertyField(r, p, true), "enabled");
            }

            EndProperty();
        }

        void DisableOthers(SerializedProperty property)
        {
            if (string.IsNullOrEmpty(attribute.group)) return;

            var so = property.serializedObject;
            var target = so.targetObject;
            var t = target.GetType();

            foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var a = f.GetCustomAttribute<ToggleObjectAttribute>();
                if (a == null || a.group != attribute.group || f.Name == fieldInfo.Name) continue;
                var sp = so.FindProperty(f.Name);
                var ep = sp?.FindPropertyRelative("enabled");
                if (ep != null) ep.boolValue = false;
            }
            so.ApplyModifiedProperties();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            (hasHeader ? HeaderHeight + 2 : 0) + LineHeight
                + (property.isExpanded ? property.IterateToEndHeight("enabled") + 2 : 0);

        #region Styles
        static GUIStyle buttonLabel1;
        static GUIStyle ButtonLabel1 => buttonLabel1 ??= new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(22, 10, 2, 2)
        };
        static GUIStyle buttonLabel2;
        static GUIStyle ButtonLabel2 => buttonLabel2 ??= new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(2, 10, 2, 2)
        };

        #endregion
    }



}
#endif