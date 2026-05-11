using UnityEngine;

namespace pigbrain.core.Inspector
{
    public class InlineScriptableObjectAttribute : PropertyAttribute
    {
        public readonly bool inlineEditor, zeroIndent;
        public InlineScriptableObjectAttribute(bool zeroIndent = false, bool inlineEditor = true)
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
    using System;
    using pigbrain.core.Utility;
    using System.IO;
    using System.Collections.Generic;

    public class InlineContextScope : IDisposable
    {
        public readonly Editor editor;
        public readonly SerializedObject parent;

        static readonly Stack<InlineContextScope> Stack = new();
        public static InlineContextScope Current => Stack.Count > 0 ? Stack.Peek() : null;

        public static bool TryGetParentContext(SerializedObject target, out SerializedObject parent)
        {
            if (Current != null && Current.editor.serializedObject == target)
            {
                parent = Current.parent;
                return true;
            }
            parent = null;
            return false;
        }

        public InlineContextScope(Editor editor, SerializedObject parent)
        {
            this.editor = editor;
            this.parent = parent;
            Stack.Push(this);
        }
        void IDisposable.Dispose() { if (Stack.Count > 0) Stack.Pop(); }
    }

    public class InlineEditor : Editor
    {
        SerializedObject parentContext;
        public bool TryGetParentContext(out SerializedObject parent)
        {
            parent = InlineContextScope.Current.parent == parentContext ? parentContext : null;
            return parent != null;
        }
        public virtual void SetContext(SerializedObject parent) => this.parentContext = parent;
        public virtual void InlineDraw(ref Rect rect) => this.DrawWithoutScript(ref rect);
        public virtual float GetInlineHeight() => this.GetHeightWithoutScript();
    }

    [CustomPropertyDrawer(typeof(InlineScriptableObjectAttribute))]
    public class InlineScriptableObjectAttribute_PropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect p, SerializedProperty prop, GUIContent label)
        {
            InlineScriptableObjectAttribute inline = attribute as InlineScriptableObjectAttribute;

            // Multi-object editing: disable inline editor to avoid shared reference issues
            // ADDED 2026/03/20
            if (prop.serializedObject.isEditingMultipleObjects)
            {
                var area = p.DivideArea(Padding, 0, 18).ToArray();
                EditorGUI.PropertyField(area[0], prop);
                DrawMenu(area[1], prop);
                return;
            }

            if (!inline.inlineEditor)
            {
                var area = p.DivideArea(Padding, 0, 18).ToArray();
                EditorGUI.PropertyField(area[0], prop);
                DrawMenu(area[1], prop);
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

            if (!obj) EditorGUI.PropertyField(areas[0], prop, GUIContent.none, includeChildren: true);
            else prop.objectReferenceValue = EditorGUI.ObjectField(areas[0], obj,
                prop.GetElementType(), true);

            DrawMenu(areas[1], prop);

            if (prop.isExpanded)
            {
                var editor = GetCachedEditor(obj);
                if (editor)
                {
                    using var _ = new EditorGUI.IndentLevelScope();
                    Rect rect = EditorGUI.IndentedRect(p).WithMinX(p.xMin).WithY(r.yMax + Padding);
                    using var scope = new InlineContextScope(editor, prop.serializedObject);
                    if (editor is InlineEditor inlineEditor)
                    {
                        inlineEditor.SetContext(prop.serializedObject);
                        inlineEditor.InlineDraw(ref rect);
                    }
                    else if (editor && editor.DrawWithoutScript(ref rect))
                        EditorUtility.SetDirty(obj);
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
        {
            InlineScriptableObjectAttribute inline = attribute as InlineScriptableObjectAttribute;
            if (!inline.inlineEditor) return base.GetPropertyHeight(prop, label);
            float height = LineHeight + Padding;
            if (prop.isExpanded && prop.objectReferenceValue is ScriptableObject obj)
            {
                var editor = GetCachedEditor(obj);
                using var scope = new InlineContextScope(editor, prop.serializedObject);
                if (editor is InlineEditor inlineEditor)
                {
                    inlineEditor.SetContext(prop.serializedObject);
                    height += inlineEditor.GetInlineHeight();
                }
                else if (editor) height += editor.GetHeightWithoutScript();
            }
            return height;
        }

        #region New & Clone

        void DrawMenu(Rect rect, SerializedProperty prop)
        {
            if (GUI.Button(rect, "⋮", Styles.ToolbarDropDown))
            {
                var menu = new GenericMenu();
                var fieldType = prop.GetFieldInfo().FieldType;

                if (fieldType.IsArray) fieldType = fieldType.GetElementType();
                else if (typeof(System.Collections.IList).IsAssignableFrom(fieldType)
                    && fieldType.IsGenericType) fieldType = fieldType.GetGenericArguments()[0];

                // Base type option
                menu.AddItem(new GUIContent($"New/{fieldType.Name.GetDisplayName()}"), false, () =>
                {
                    if (NewObject(fieldType) is ScriptableObject o)
                    {
                        prop.objectReferenceValue = o;
                        prop.serializedObject.ApplyModifiedProperties();
                    }
                });

                // Derived types
                var types = TypeCache.GetTypesDerivedFrom(fieldType)
                    .Where(t => !t.IsAbstract && typeof(ScriptableObject).IsAssignableFrom(t));

                foreach (var t in types)
                {
                    var local = t;
                    menu.AddItem(new GUIContent($"New/{t.Name.GetDisplayName()}"), false, () =>
                    {
                        if (NewObject(local) is ScriptableObject o)
                        {
                            prop.objectReferenceValue = o;
                            prop.serializedObject.ApplyModifiedProperties();
                        }
                    });
                }

                if (prop.objectReferenceValue)
                    menu.AddItem(new GUIContent("Clone"), false, () =>
                    {
                        if (CloneObject((ScriptableObject)prop.objectReferenceValue) is ScriptableObject c)
                        {
                            prop.objectReferenceValue = c;
                            prop.serializedObject.ApplyModifiedProperties();
                        }
                    });
                else
                    menu.AddDisabledItem(new GUIContent("Clone"));

                menu.DropDown(rect);
            }
        }

        const string PathKey = "InlineScriptableObject/Path";
        ScriptableObject NewObject(Type type)
        {
            Debug.Log($"NewObject {type}");
            string path = EditorUtility.SaveFilePanelInProject("Save Asset",
                $"new {type.Name.GetDisplayName()}", "asset", "Save", EditorPrefs.GetString(PathKey, ""));
            if (!string.IsNullOrEmpty(path))
            {
                EditorPrefs.SetString(PathKey, Path.GetDirectoryName(path));
                return Save(ScriptableObject.CreateInstance(type), path);
            }
            return null;
        }

        ScriptableObject CloneObject(ScriptableObject obj)
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Asset",
                $"{obj.name}", "asset", "Save", AssetDatabase.GetAssetPath(obj));
            if (!string.IsNullOrEmpty(path))
                return Save(UnityEngine.Object.Instantiate(obj), path);
            return null;
        }

        ScriptableObject Save(ScriptableObject o, string path)
        {
            AssetDatabase.CreateAsset(o, path);
            AssetDatabase.SaveAssets();
            return o;
        }
        #endregion

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