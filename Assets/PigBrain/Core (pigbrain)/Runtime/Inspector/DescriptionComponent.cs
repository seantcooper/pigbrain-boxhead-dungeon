using System;
using UnityEngine;

namespace pigbrain.core.Inspector
{
    [AddComponentMenu("Pigbrain/Description")]
    public class DescriptionComponent : MonoBehaviour
    {
        public string text;
        [SerializeField, HideInInspector] public Color color = new(0.25f, 0.25f, 0.25f, 1f);
    }
}

#if UNITY_EDITOR
namespace pigbrain.core.Inspector
{
    using UnityEditor;
    using static pigbrain.core.Inspector.InspectorUtility;


    [CustomEditor(typeof(DescriptionComponent))]
    sealed class DescriptionComponentEditor : Editor
    {
        DescriptionComponent description => target as DescriptionComponent;

        void OnEnable()
        {
            if (description.color.a <= 0f)
                description.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        }

        protected override void OnHeaderGUI()
        {
            var rect = GUILayoutUtility.GetRect(
                0,
                LineHeight * 1.6f,
                GUILayout.ExpandWidth(true));

            if (Event.current.type != EventType.Repaint)
                return;

            // Background
            EditorGUI.DrawRect(rect, description.color);

            // Text
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white },
                padding = new RectOffset(8, 8, 0, 0)
            };

            EditorGUI.LabelField(rect, description.text, style);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            description.text = EditorGUILayout.TextArea(description.text);
            serializedObject.ApplyModifiedProperties();
            if (GUI.changed) Repaint();
        }
    }
}
#endif
