#pragma warning disable UDR0001
using System;
using System.Collections.Generic;
using System.Reflection;
using pigbrain.core.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using pigbrain.core.Geom;
using Object = UnityEngine.Object;

namespace pigbrain.core.Project
{
    #region HEADER DRAWER
    static class Header
    {
        static readonly Dictionary<string, Color> NSColors = new()
        {
            {"pigbrain.Game",new(0.3f, 0.6f, 1f)},
            {"pigbrain.Core",new(0.3f, 1f, 0.5f)},
            {"pigbrain.Generated",new(1f, 0.6f,0f)},
        };

        const float A = 0.2f;
        public static bool GetColor(System.Type t, out Color color, float a = 0.15f)
        {
            color = default;
            string ns = t.Namespace ?? "";
            foreach (var kvp in NSColors)
                if (ns.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    return (color = kvp.Value).a > 0;
            return false;
        }
    }

    [InitializeOnLoad]
    static class GameObjectHeader
    {
        static GameObjectHeader()
        {
            Editor.finishedDefaultHeaderGUI -= OnHeaderGUI;
            Editor.finishedDefaultHeaderGUI += OnHeaderGUI;
            ComponentHeader.OnComponentHeaderGUI += DrawHeader;

            static void DrawHeader(Rect rect, Object target)
            {
                if (target is not MonoBehaviour) return;
                if (Header.GetColor(target.GetType(), out Color color))
                    EditorGUI.DrawRect(rect.Shrink(2), color.WithA(0.2f));
            }
        }

        static void OnHeaderGUI(Editor editor)
        {
            if (editor != null && editor.target is GameObject go)
                DrawButton(go);
        }

        public static void DrawButton(GameObject prefab)
        {
            if (!FavouriteService.GetGUID(prefab, out string guid)) return;
            var isFav = FavouriteService.IsFavourite(guid);
            var newFav = GUILayout.Toggle(isFav, "★", "Button", GUILayout.Width(20));
            if (newFav != isFav) FavouriteService.Set(guid, newFav);
        }
    }
    #endregion

    #region Component Headers
    public static class ComponentHeader
    {
        public static event Action<Rect, Object> OnComponentHeaderGUI;

        [InitializeOnLoadMethod]
        public static void Init()
        {
            EditorApplication.update -= InvokeEvents;
            EditorApplication.update += InvokeEvents;
            Selection.selectionChanged -= ClearElementCallbacks;
            Selection.selectionChanged += ClearElementCallbacks;

            EditorApplication.hierarchyChanged -= ClearElementCallbacks;
            EditorApplication.hierarchyChanged += ClearElementCallbacks;

            EditorApplication.projectChanged -= ClearElementCallbacks;
            EditorApplication.projectChanged += ClearElementCallbacks;

            UnityEditor.SceneManagement.PrefabStage.prefabStageOpened -= OnPrefabStageChanged;
            UnityEditor.SceneManagement.PrefabStage.prefabStageOpened += OnPrefabStageChanged;
            UnityEditor.SceneManagement.PrefabStage.prefabStageClosing -= OnPrefabStageChanged;
            UnityEditor.SceneManagement.PrefabStage.prefabStageClosing += OnPrefabStageChanged;
        }

        static readonly Type EditorElement = typeof(EditorWindow).Assembly.GetType("UnityEditor.UIElements.EditorElement");
        static readonly FieldInfo EditorsElementField = typeof(EditorWindow).Assembly.GetType("UnityEditor.PropertyEditor").GetField("m_EditorsElement", ReflectionUtility.DefaultBindings);
        static readonly FieldInfo HeaderField = EditorElement.GetField("m_Header", ReflectionUtility.DefaultBindings);
        static readonly FieldInfo EditorTarget = EditorElement.GetField("m_EditorTarget", ReflectionUtility.DefaultBindings);

        static VisualElement EditorsElementCached;
        static VisualElement EditorsElement => EditorsElementCached
            ??= EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.InspectorWindow")) is EditorWindow window
            ? EditorsElementField.GetValue(window) as VisualElement
            : null;

        static readonly Dictionary<VisualElement, Action> HookedHeaders = new();
        private static void ClearElementCallbacks()
        {
            foreach (var kvp in HookedHeaders)
                if (kvp.Key is IMGUIContainer c)
                    c.onGUIHandler -= kvp.Value;

            HookedHeaders.Clear();
        }

        static void OnPrefabStageChanged(UnityEditor.SceneManagement.PrefabStage _) => ClearElementCallbacks();

        private static void InvokeEvents()
        {
            if (Application.isPlaying) return;
            if (EditorsElement == null) return;
            foreach (VisualElement element in EditorsElement.Children())
            {
                if (element.GetType() != EditorElement || EditorTarget.GetValue(element) is not Object target) continue;
                if (HeaderField.GetValue(element) is IMGUIContainer headerElement)
                {
                    if (!HookedHeaders.ContainsKey(headerElement))
                    {
                        var localTarget = target;

                        void MyLocalCallback()
                        {
                            var evt = Event.current;
                            if (evt == null) return;

                            if (evt.type != EventType.Repaint && evt.type != EventType.Layout) return;

                            Rect r = GUILayoutUtility.GetLastRect();
                            if (r.width <= 0 || r.height <= 0) return;

                            OnComponentHeaderGUI?.Invoke(r, localTarget);
                        }

                        HookedHeaders[headerElement] = MyLocalCallback;
                        headerElement.onGUIHandler += MyLocalCallback;
                    }
                }
            }
        }
    }
    #endregion
}
