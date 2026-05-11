using UnityEngine;

// Identical to Inline Scriptable Object

namespace pigbrain.core.Inspector
{
    public class InlineComponentAttribute : PropertyAttribute
    {
        public readonly bool inlineEditor, zeroIndent;
        public InlineComponentAttribute(bool zeroIndent = false, bool inlineEditor = true)
        {
            this.zeroIndent = zeroIndent;
            this.inlineEditor = inlineEditor;
        }
    }
}

#if UNITY_EDITOR
namespace pigbrain.core.Inspector
{
    using UnityEditor;
    using pigbrain.core.Geom;
    using static pigbrain.core.Inspector.InspectorUtility;
    using System.Linq;
    using pigbrain.core.Utility;

    [CustomPropertyDrawer(typeof(InlineComponentAttribute))]
    public class InlineComponentAttribute_PropertyDrawer : PropertyDrawer
    {
        InlineComponentAttribute inline => attribute as InlineComponentAttribute;
        public override void OnGUI(Rect p, SerializedProperty prop, GUIContent label)
        {
            // InlineComponentAttribute inline = attribute as InlineComponentAttribute;
            if (!inline.inlineEditor)
            {
                var area = p.DivideArea(Padding, 0, 18).ToArray();
                EditorGUI.PropertyField(area[0], prop);
                return;
            }

            Rect r = default;
            var obj = prop.objectReferenceValue;
            if (!obj) prop.isExpanded = false;

            if (prop.IsArrayElement())
            {
                r = p.WithH(LineHeight).WithW(0);
                if (obj) prop.isExpanded = EditorGUI.Foldout(r.WithW(IndentSize), prop.isExpanded, GUIContent.none, true);
            }
            else
            {
                r = p.WithH(LineHeight).WithW(EditorGUIUtility.labelWidth - TotalIndentSize);
                if (obj) prop.isExpanded = EditorGUI.Foldout(r, prop.isExpanded, label, true);
                else EditorGUI.PrefixLabel(r, label);
            }

            Rect prect = r.WithX(r.xMax + Padding).WithW(p.width - r.width - Padding);
            var areas = prect.DivideArea(Padding, 0, 18).ToArray();

            if (obj) prop.objectReferenceValue = EditorGUI.ObjectField(areas[0], obj, obj.GetType(), true);
            else EditorGUI.PropertyField(areas[0], prop, GUIContent.none, includeChildren: true);

            if (prop.isExpanded)
            {
                var editor = GetCachedEditor(obj);
                if (editor)
                {
                    using var _ = new EditorGUI.IndentLevelScope();
                    Rect rect = EditorGUI.IndentedRect(p).WithMinX(p.xMin).WithY(r.yMax + Padding);
                    if (editor.DrawWithoutScript(ref rect)) EditorUtility.SetDirty(obj);
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!inline.inlineEditor) return base.GetPropertyHeight(property, label);
            float height = LineHeight + Padding;
            if (property.isExpanded && property.objectReferenceValue is ScriptableObject obj)
                height += Editor.CreateEditor(obj).GetHeightWithoutScript();
            return height;
        }

        #region Editor Cache
        (Editor editor, UnityEngine.Object target) cache;

        Editor GetCachedEditor(UnityEngine.Object obj)
        {
            if (cache.target != obj)
            {
                if (cache.editor) UnityEngine.Object.DestroyImmediate(cache.editor);
                cache = new(null, null);
            }

            var editorType = obj.FindEditorTypeFor();
            if (editorType == null)
            {
                Debug.Log($"No custom editor for {obj}!");
            }
            Editor.CreateCachedEditor(obj, editorType, ref cache.editor);
            return cache.editor;
        }
        #endregion
    }
}
#endif