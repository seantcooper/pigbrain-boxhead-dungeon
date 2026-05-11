using UnityEditor;
using UnityEngine;
using System.Linq;
using System.IO;
using static pigbrain.core.Inspector.InspectorUtility;
using System;
using static pigbrain.core.Project.TaskList;

namespace pigbrain.core.Project
{
    public class TaskListWindow : EditorWindow
    {
        TaskList data;
        const string AssetPath = "Assets/pigbrain/Generated/Tasks.asset";
        string newName = "";
        Vector2 scroll;

        [MenuItem("Tools/pigbrain/Tasks")]
        static void Open() => GetWindow<TaskListWindow>("Tasks");

        void OnEnable()
        {
            data = AssetDatabase.LoadAssetAtPath<TaskList>(AssetPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<TaskList>();
                var dir = Path.GetDirectoryName(AssetPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                AssetDatabase.CreateAsset(data, AssetPath);
                AssetDatabase.SaveAssets();
            }
        }

        const float Scale = 1.1f;

        void Sort()
        {
            data.tasks = data.tasks.OrderBy(t => (int)t.status).ToList();
            EditorUtility.SetDirty(data);
        }

        void OnGUI()
        {
            var oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * Scale);
            float scaledWidth = position.width / Scale;
            EditorGUILayout.BeginVertical(GUILayout.Width(scaledWidth));

            bool addPressed = Event.current.type == EventType.KeyDown &&
                              (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);
            newName = EditorGUILayout.TextField(newName);
            bool addClicked = GUILayout.Button("Add");

            if ((addClicked || addPressed) && !string.IsNullOrEmpty(newName))
            {
                var ntask = new Task { name = newName, duration = 1, status = Task.Status.ToDo };

                newName = "";
                SetDirty(true, () => data.tasks.Add(ntask));
                GUI.FocusControl(null);
                Event.current.Use();
            }
            GUILayout.Space(4);

            float scaledHeight = position.height / Scale;

            // Reserve space used before scroll (input + spacing)
            float headerHeight = LineHeight * 3;
            // scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(scaledHeight - headerHeight));

            using (var scrollView = new EditorGUILayout.ScrollViewScope(scroll, GUILayout.Height(scaledHeight - headerHeight)))
            {
                scroll = scrollView.scrollPosition;

                Task.Status status = Task.Status.Doing;
                foreach (var t in data.tasks)
                {
                    if (status != t.status) { LayoutLine(4); status = t.status; }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var newStatus = (Task.Status)EditorGUILayout.EnumPopup(t.status, GUILayout.Width(80));
                        SetDirty(newStatus != t.status, () => t.status = newStatus);

                        using (new EditorGUI.DisabledScope(t.status == Task.Status.Done || t.status == Task.Status.Hold))
                        {
                            float newDuration = EditorGUILayout.FloatField(t.duration, GUILayout.Width(TinyFieldWidth));
                            SetDirty(newDuration != t.duration, () => t.duration = newDuration);

                            string newName = EditorGUILayout.TextField(t.name);
                            SetDirty(newName != t.name, () => t.name = newName);
                        }
                    }
                }
                GUILayout.Space(10);
            }
            EditorGUILayout.EndVertical();
            GUI.matrix = oldMatrix;
        }

        void SetDirty(bool condition, Action assignment)
        {
            if (condition)
            {
                assignment();
                Sort();
                EditorUtility.SetDirty(data);
            }
        }
    }
}
