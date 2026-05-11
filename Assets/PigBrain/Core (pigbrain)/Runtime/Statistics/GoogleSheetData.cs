using System;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Project;
using UnityEngine;
using UnityEngine.Events;

namespace pigbrain.core.Statistics
{
    [ProjectInterface.Control(ProjectInterface.Filter.Hierarchy)]
    public class GoogleSheetData : MonoBehaviour //, IProjectControl
    {
        [SerializeField] string lastRead = "";
        [SerializeField] string id = "1VC23MF7jl22sUPbB9ARQt-k1ZXgbA2oiwb4z0tm079U";
        [SerializeField][HideInInspector] internal string sheetName = "0";
        [SerializeField][HideInInspector] internal string sheetValue = "0";
        [SerializeField] internal SheetIdName[] sheetIdName;
        [SerializeField] Events events;
        [Serializable] class Events { public UnityEvent<GoogleSheetReader> onRead; }

        [ProjectInterface.Button("Read")]
        public void Read()
        {
            var reader = new GoogleSheetReader(id, sheetValue);
            reader.Read(() =>
            {
                events?.onRead?.Invoke(reader);
                lastRead = DateTime.Now.ToString();
            });
        }

        [ContextMenu("Read Sheet Names")]
        void GetSheets()
        {
            var reader = new GoogleSheetReader(id, "0");
            reader.Read(() =>
            {
                var sheetIdName = new List<SheetIdName>();
                foreach (var line in reader.lines)
                {
                    var o = new TypeInterface<SheetIdName>(new());
                    foreach (var g in line.groups)
                        o[g.key] = g.values.First();
                    sheetIdName.Add(o.target);
                }
                this.sheetIdName = sheetIdName.ToArray();
            });
        }

        [Serializable]
        internal class SheetIdName
        {
            public string name, id;
        }
    }
}

#if UNITY_EDITOR
#region Editor
namespace pigbrain.core.Statistics
{
    using System.Linq;
    using pigbrain.core.Collections;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(GoogleSheetData))]
    public class GoogleSheetData_Editor : Editor
    {
        GoogleSheetData data => (target as GoogleSheetData);
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            var sheetNameProp = serializedObject.FindProperty(nameof(GoogleSheetData.sheetName));
            var sheetValueProp = serializedObject.FindProperty(nameof(GoogleSheetData.sheetValue));

            bool read = false;
            if (!data.sheetIdName.IsNullOrEmpty())
            {
                var names = data.sheetIdName.Select(s => s.name).ToArray();
                var values = data.sheetIdName.Select(s => s.name).ToArray();
                int currentIndex = names.ToList().IndexOf(sheetNameProp.stringValue);

                EditorGUI.BeginChangeCheck();
                int newIndex = EditorGUILayout.Popup("Sheet", currentIndex, names);

                if (EditorGUI.EndChangeCheck())
                {
                    sheetNameProp.stringValue = data.sheetIdName[newIndex].name;
                    sheetValueProp.stringValue = data.sheetIdName[newIndex].id;
                    read = true;
                }
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(sheetNameProp);
                if (EditorGUI.EndChangeCheck())
                    sheetValueProp.stringValue = sheetNameProp.stringValue;
            }

            if (GUILayout.Button("Read Sheet")) read = true;

            serializedObject.ApplyModifiedProperties();

            if (read) data.Read();
        }
    }
}
#endregion
#endif