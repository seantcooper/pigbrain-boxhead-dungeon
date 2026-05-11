using System;
using System.Collections.Generic;
using System.Linq;
using PigBrain.Generated;
using UnityEngine;

namespace PigBrain.LegacyCore.Utility
{
    [AttributeUsage(AttributeTargets.Field)]
    public class LayerAttribute : PropertyAttribute
    {
        public LayerAttribute() { }
    }

    public static class LayerExtensions
    {
        public static LayerMask GetLayerMask(this GameObject gameObject) => 1 << (int)gameObject.layer;
        public static LayerMask GetInverseLayerMask(this GameObject gameObject) => ~gameObject.GetLayerMask();
    }

    public static class TagExtensions
    {
        public static IEnumerable<T> GetComponentsInChildren<T>(this Transform transform, GameTag tag)
        {
            foreach (Transform tranform in transform.GetComponentsInChildren<Transform>(true))
                if (tranform.CompareTag(GameTags.ReverseLookup[tag]) && tranform.TryGetComponent(out T component))
                    yield return component;
        }
        public static IEnumerable<Transform> GetTransformsInChildren(this Transform transform, GameTag tag)
        {
            var stag = $"{tag}";
            foreach (Transform child in transform.GetComponentsInChildren<Transform>(true))
                if (child.CompareTag(stag))
                    yield return child;
        }

        public static IEnumerable<GameTag> AsTags(this GameTagMask mask) =>
            Enum.GetValues(typeof(GameTag)).Cast<GameTag>().Where(t => mask.HasFlag((GameTagMask)(1 << ((int)t))));

        public static bool Contains(this GameTagMask mask, GameTag tag) =>
            mask.AsTags().Any(t => t == tag);

        static GameTag ToGameTag(string tag) => GameTags.Lookup.TryGetValue(tag, out GameTag gameTag) ? gameTag : GameTag.Untagged;

        public static GameTag GetGameTag(this GameObject unityObject) => ToGameTag(unityObject.tag);
        public static GameTag GetGameTag(this Component unityObject) => ToGameTag(unityObject.tag);

        public static bool CompareTag(this Component component, GameTag tag) => component.gameObject.CompareTag(tag);
        public static bool CompareTag(this GameObject gameObject, GameTag tag) => gameObject.CompareTag(GameTags.ReverseLookup[tag]);


    }
}

#if UNITY_EDITOR
namespace PigBrain.LegacyCore.Utility
{
    using System.Linq;
    using UnityEditor;

    [CustomPropertyDrawer(typeof(LayerAttribute))]
    public class LayerDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.Integer)
            {
                var layerNames = Enumerable.Range(0, 32).Select(i => LayerMask.LayerToName(i)).ToArray();
                property.intValue = EditorGUI.Popup(position, label.text, property.intValue, layerNames);
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Use [Layer] with int.");
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            base.GetPropertyHeight(property, label);
    }

}
#endif

#if UNITY_EDITOR
namespace PigBrain.LegacyCore.Utility
{
    using UnityEditor;
    using UnityEngine;
    using System.IO;
    using System.Text;

    class TagManagerPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
                                           string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var asset in importedAssets)
                if (asset == "ProjectSettings/TagManager.asset")
                    TagsAndLayersGenerator.GenerateTagsAndLayers();
        }
    }

    public static class TagsAndLayersGenerator
    {
        const string GeneratedPath = "Assets/pigbrain/Generated";

        [MenuItem("Tools/pigbrain/Generate/Generate Tags & Layers")]
        public static void GenerateTagsAndLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            GenerateLayers(tagManager);
            GenerateTags(tagManager);

            AssetDatabase.Refresh();
            Debug.Log("Generated GameLayers.cs & GameTags.cs");
        }

        static void GenerateLayers(SerializedObject tagManager)
        {
            var layersProp = tagManager.FindProperty("layers");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// AUTO-GENERATED");
            sb.AppendLine();
            sb.AppendLine("namespace PigBrain.Generated");
            sb.AppendLine("{");
            sb.AppendLine("    public enum GameLayer");
            sb.AppendLine("    {");

            StringBuilder sbFlags = new StringBuilder();
            sbFlags.AppendLine("// AUTO-GENERATED");
            sbFlags.AppendLine();
            sbFlags.AppendLine("namespace PigBrain.Generated");
            sbFlags.AppendLine("{");
            sbFlags.AppendLine("    [System.Flags]");
            sbFlags.AppendLine("    public enum GameLayerFlags");
            sbFlags.AppendLine("    {");
            sbFlags.AppendLine("        None = 0,");

            for (int i = 0; i < layersProp.arraySize; i++)
            {
                var sp = layersProp.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(sp.stringValue))
                {
                    string safeName = SanitizeName(sp.stringValue);
                    sb.AppendLine($"        {safeName} = {i},");
                    sbFlags.AppendLine($"        {safeName} = 1 << {i},");
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            sbFlags.AppendLine("        All = ~0");
            sbFlags.AppendLine("    }");
            sbFlags.AppendLine("}");

            Directory.CreateDirectory(GeneratedPath);
            File.WriteAllText(Path.Combine(GeneratedPath, "GameLayers.cs"), sb.ToString() + "\n\n" + sbFlags.ToString());
        }

        static void GenerateTags(SerializedObject tagManager)
        {
            var tagsProp = tagManager.FindProperty("tags");

            StringBuilder sbHandles = new StringBuilder();
            sbHandles.AppendLine("// AUTO-GENERATED");
            sbHandles.AppendLine();
            sbHandles.AppendLine("namespace PigBrain.Generated");
            sbHandles.AppendLine("{");
            sbHandles.AppendLine("    public enum GameTag");
            sbHandles.AppendLine("    {");
            sbHandles.AppendLine("        Untagged = -1,");

            StringBuilder sbFlags = new StringBuilder();
            sbFlags.AppendLine("// AUTO-GENERATED");
            sbFlags.AppendLine();
            sbFlags.AppendLine("namespace PigBrain.Generated");
            sbFlags.AppendLine("{");
            sbFlags.AppendLine("    [System.Flags]");
            sbFlags.AppendLine("    public enum GameTagMask");
            sbFlags.AppendLine("    {");
            sbFlags.AppendLine("        Untagged = 0,");

            StringBuilder sbStatic = new StringBuilder();
            sbStatic.AppendLine("// AUTO-GENERATED");
            sbStatic.AppendLine();
            sbStatic.AppendLine("namespace PigBrain.Generated");
            sbStatic.AppendLine("{");
            sbStatic.AppendLine("    public static class GameTags");
            sbStatic.AppendLine("    {");
            sbStatic.AppendLine("        public static readonly string[] AllTags = new string[]");
            sbStatic.AppendLine("        {");
            sbStatic.AppendLine("            \"Untagged\",");

            StringBuilder sbLookup = new StringBuilder();
            sbLookup.AppendLine("        public static readonly System.Collections.Generic.Dictionary<string, GameTag> Lookup =");
            sbLookup.AppendLine("            new System.Collections.Generic.Dictionary<string, GameTag>()");
            sbLookup.AppendLine("            {");
            sbLookup.AppendLine("                { \"Untagged\", GameTag.Untagged },");

            StringBuilder sbReverseLookup = new StringBuilder();
            sbReverseLookup.AppendLine("        public static readonly System.Collections.Generic.Dictionary<GameTag, string> ReverseLookup =");
            sbReverseLookup.AppendLine("            new System.Collections.Generic.Dictionary<GameTag, string>()");
            sbReverseLookup.AppendLine("            {");
            sbReverseLookup.AppendLine("                { GameTag.Untagged, \"Untagged\" },");

            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                var sp = tagsProp.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(sp.stringValue))
                {
                    string safeName = SanitizeName(sp.stringValue);

                    sbHandles.AppendLine($"        {safeName} = {i},");
                    sbFlags.AppendLine($"        {safeName} = 1 << {i},");
                    sbStatic.AppendLine($"            \"{sp.stringValue}\",");

                    sbLookup.AppendLine($"                {{ \"{sp.stringValue}\", GameTag.{safeName} }},");
                    sbReverseLookup.AppendLine($"                {{ GameTag.{safeName}, \"{sp.stringValue}\" }},");
                }
            }

            sbHandles.AppendLine("    }");
            sbHandles.AppendLine("}");

            sbFlags.AppendLine("        All = ~0");
            sbFlags.AppendLine("    }");
            sbFlags.AppendLine("}");

            sbStatic.AppendLine("        };");
            sbLookup.AppendLine("            };");
            sbReverseLookup.AppendLine("            };");

            sbStatic.AppendLine();
            sbStatic.AppendLine(sbLookup.ToString());
            sbStatic.AppendLine();
            sbStatic.AppendLine(sbReverseLookup.ToString());
            sbStatic.AppendLine("    }");
            sbStatic.AppendLine("}");

            Directory.CreateDirectory(GeneratedPath);
            File.WriteAllText(Path.Combine(GeneratedPath, "GameTags.cs"),
                sbHandles.ToString() + "\n\n" + sbFlags.ToString() + "\n\n" + sbStatic.ToString());
        }

        static string SanitizeName(string name)
        {
            var sb = new StringBuilder();
            foreach (char c in name)
                if (char.IsLetterOrDigit(c) || c == '_') sb.Append(c); else sb.Append('_');
            return sb.ToString();
        }
    }
}
#endif