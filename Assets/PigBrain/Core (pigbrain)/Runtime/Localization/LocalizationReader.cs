#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using pigbrain.core.Inspector;
using pigbrain.core.Project;
using pigbrain.core.Statistics;
using UnityEngine;

namespace pigbrain.core.Localization
{
    [Serializable]
    [InlineButton(nameof(Read), nameof(Clear), nameof(FindAll))]
    [CreateAssetMenu(menuName = "PigBrain/Boxhead/Localization/Reader")]
    [ProjectInterface.Control(ProjectInterface.Filter.Project)]
    public class LocalisationReader : ScriptableObject
    {
        [SerializeField] GoogleSheetReader reader;
        [SerializeField][InlineScriptableObject] List<LocalizationData> datas;

        [ProjectInterface.Button("Read")]
        public void Read() => reader.Read(OnRead, OnError);

        public void Clear()
        {
            foreach (var data in this.datas)
            {
                data.Clear();
                Write(data);
            }
        }

        public void FindAll()
        {
            datas.Clear();
            var guids = UnityEditor.AssetDatabase.FindAssets("t:LocalizationData");
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<LocalizationData>(path);
                if (asset) datas.Add(asset);
            }
            UnityEditor.EditorUtility.SetDirty(this);
        }

        void OnRead()
        {
            Clear();
            var lines = reader.lines;

            string commonPath = commonPath = System.IO.Path.GetDirectoryName(UnityEditor.AssetDatabase.GetAssetPath(this));
            List<LocalizationData> newDatas = new();
            foreach (var line in lines)
            {
                LocalizationData target = null;
                string name = line["name"].GetValue();
                string objectName = $"Localization ({name})";

                foreach (var group in line.groups)
                {
                    foreach (var data in datas)
                    {
                        if (data.name.Equals(objectName, StringComparison.OrdinalIgnoreCase))
                        {
                            target = data;
                            break;
                        }
                    }

                    if (!target)
                    {
                        var assetPath = $"{commonPath}/{objectName}.asset";
                        var existing = UnityEditor.AssetDatabase.LoadAssetAtPath<LocalizationData>(assetPath);
                        if (!existing)
                        {
                            newDatas.Add(target = LocalizationData.CreateInstance(LocalizationData.Language.English, objectName));
                            datas.Add(target);
                            UnityEditor.AssetDatabase.CreateAsset(target, assetPath);
                        }
                    }
                }

                if (!target)
                {
                    Debug.LogError($"No Target found for {objectName}");
                    continue;
                }

                foreach (var g in line.groups)
                {
                    if (g.key == "name") continue;
                    target.AddOrModify(g.key, g.GetValue());
                }
                Write(target);
            }

            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
        }

        void Write(LocalizationData target)
        {
            UnityEditor.EditorUtility.SetDirty(target);
            UnityEditor.AssetDatabase.SaveAssets();
        }

        void OnError()
        {

        }
    }
}
#endif
