using System;
using UnityEngine;

namespace PigBrain.LegacyCore.Utility
{
    [AttributeUsage(AttributeTargets.Field)]
    public class BoolButtonAttribute : PropertyAttribute
    {
        public string method, label;
        public BoolButtonAttribute(string method = null, string label = null)
        {
            this.method = method;
            this.label = label;
        }
    }
}

#if UNITY_EDITOR
namespace PigBrain.LegacyCore.Utility
{
    using System.Linq;
    using System.Reflection;
    using UnityEditor;
    [CustomPropertyDrawer(typeof(BoolButtonAttribute))]
    public class BoolButtonPropertyDrawer : PropertyDrawer
    {
        Color hilightColor = new Color(0.39f, 0.9f, 0.51f);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attribute = this.attribute as BoolButtonAttribute;
            using (new GUIColor())
            {
                EditorGUI.BeginProperty(position, label, property);
                if (property.boolValue) GUI.color = hilightColor;

                string displayLabel = string.IsNullOrEmpty(attribute.label) ? property.displayName.Replace("_", " ") : attribute.label;

                if (attribute.method == null)
                {
                    if (GUI.Button(position, displayLabel))
                        property.boolValue = !property.boolValue;
                }
                else
                {
                    if (GUI.Button(position, displayLabel))
                    {
                        var target = property.serializedObject.targetObject != null ?
                            property.serializedObject.targetObject : throw new Exception($"{attribute.method} Target is null!");

                        Undo.RecordObject(target, $"Invoke {displayLabel}");
                        property.boolValue = false;

                        // Debug.Log($"property.propertyPath = {property.propertyPath}");
                        var propertyPath = string.Join(".", property.propertyPath.Split('.').SkipLast(1));

                        var method = target.GetType().GetMethod(attribute.method,
                            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                                ?? throw new Exception($"{attribute.method} Method is null!");

                        method?.Invoke(target, null);
                        EditorUtility.SetDirty(target);
                    }
                }
                EditorGUI.EndProperty();
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            base.GetPropertyHeight(property, label);
    }

    public class GUIColor : IDisposable
    {
        Color _color;
        public GUIColor() => _color = GUI.color;
        public void Dispose() => GUI.color = _color;
    }
}
#endif