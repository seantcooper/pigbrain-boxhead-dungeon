using UnityEngine;

namespace pigbrain.core.Graphics
{
    public class ModelImporter : ScriptableObject
    {
        [Header("Transform")]
        public Vector3 offset, scale = new(1, 1, 1);
        public Quaternion rotation = Quaternion.identity;

        [Header("Validation")]
        public bool removeNegativeScales;

    }
}

#region Editor
#if UNITY_EDITOR
namespace pigbrain.core.Graphics
{
    using pigbrain.core.Collections;
    using UnityEditor;
    using static UnityEditor.AssetDatabase;

    [CustomEditor(typeof(ModelImporter))]
    public class ModelImporter_Editor : Editor
    {
        ModelImporter importer => target as ModelImporter;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            base.OnInspectorGUI();

            using (var h = new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reimport"))
                    ReimportModel(importer);
                if (GUILayout.Button("Reimport All"))
                    ReimportAllModels();
            }
        }

        static void ReimportModel(ModelImporter importer) =>
            Reimport(GetAssetPath(importer));
        static void ReimportAllModels() =>
            FindAssets("t:ModelImporter").ForEach(guid => Reimport(GUIDToAssetPath(guid)));

        static void Reimport(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            var modelPath = path.Replace(".import.asset", ".fbx");
            if (!AssetPathExists(modelPath)) return;
            ImportAsset(modelPath, ImportAssetOptions.ForceUpdate);
        }
    }
}
#endif
#endregion

#if UNITY_EDITOR
namespace pigbrain.core.Graphics
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEditor;

    static class CreateModelImportSettings
    {
        static bool HasValidExtenstion(string p) => Path.GetExtension(p) switch
        { ".fbx" => true, ".skp" => true, ".obj" => true, _ => false };

        [MenuItem("Assets/Create/PigBrain/Importer/Model Settings", true)]
        static bool Validate() =>
            Selection.gameObjects.Length > 0 &&
            Selection.gameObjects.All(g => HasValidExtenstion(AssetDatabase.GetAssetPath(g))); //.EndsWith(".fbx"));

        [MenuItem("Assets/Create/PigBrain/Importer/Model Settings")]
        static void Create()
        {
            List<Object> objects = new();
            foreach (GameObject g in Selection.gameObjects)
            {
                var path = AssetDatabase.GetAssetPath(g);
                var dir = Path.GetDirectoryName(path);
                var name = Path.GetFileNameWithoutExtension(path);

                var assetPath = $"{dir}/{name}.import.asset";
                if (File.Exists(assetPath)) return;

                var so = ScriptableObject.CreateInstance<ModelImporter>();
                AssetDatabase.CreateAsset(so, assetPath);
                objects.Add(so);
            }
            AssetDatabase.SaveAssets();
            Selection.objects = objects.ToArray();
        }
    }

    class MeshTransformPostprocessor : AssetPostprocessor
    {
        void OnPostprocessModel(GameObject go)
        {
            string dir = Path.GetDirectoryName(assetPath);
            string name = Path.GetFileNameWithoutExtension(assetPath);
            string importPath = $"{dir}/{name}.import.asset";

            if (AssetDatabase.LoadAssetAtPath<ModelImporter>(importPath)
                is not ModelImporter settings) return;

            Debug.Log($"Modifiy mesh: {assetPath}");

            var mainAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);

            if (!(settings.offset == default && settings.rotation == Quaternion.identity && settings.scale == Vector3.one))
            {
                var matrix = Matrix4x4.TRS(settings.offset, settings.rotation, settings.scale);
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                {
                    var mesh = mf.sharedMesh;
                    if (!mesh)
                    {
                        Debug.Log("Mesh was not found!");
                        continue;
                    }
                    mesh.vertices = mesh.vertices.Select(v => matrix.MultiplyPoint3x4(v)).ToArray();
                    mesh.RecalculateBounds();
                    mesh.RecalculateNormals();
                }
            }

            if (settings.removeNegativeScales)
            {
                var signMap = new Dictionary<Transform, Vector3>();

                foreach (var t in go.GetComponentsInChildren<Transform>())
                {
                    var parentSign = t.parent && signMap.TryGetValue(t.parent, out var ps) ? ps : Vector3.one;

                    var ls = t.localScale;
                    var localSign = new Vector3(ls.x < 0 ? -1f : 1f, ls.y < 0 ? -1f : 1f, ls.z < 0 ? -1f : 1f);
                    var totalSign = new Vector3(parentSign.x * localSign.x, parentSign.y * localSign.y, parentSign.z * localSign.z);
                    signMap[t] = totalSign;

                    if (!t.TryGetComponent(out MeshFilter mf)) continue;
                    var mesh = mf.sharedMesh;
                    if (!mesh) continue;

                    // only fix if effective sign is negative anywhere
                    if (totalSign.x > 0 && totalSign.y > 0 && totalSign.z > 0) continue;

                    var flip = Matrix4x4.Scale(totalSign);

                    var verts = mesh.vertices;
                    for (int i = 0; i < verts.Length; i++)
                        verts[i] = flip.MultiplyPoint3x4(verts[i]);
                    mesh.vertices = verts;

                    if (totalSign.x * totalSign.y * totalSign.z < 0)
                    {
                        var tris = mesh.triangles;
                        for (int i = 0; i < tris.Length; i += 3)
                            (tris[i], tris[i + 1]) = (tris[i + 1], tris[i]);
                        mesh.triangles = tris;
                    }

                    mesh.RecalculateBounds();
                    mesh.RecalculateNormals();
                }

                // second pass: clean all scales
                foreach (var t in go.GetComponentsInChildren<Transform>())
                {
                    var ls = t.localScale;
                    if (ls.x < 0 || ls.y < 0 || ls.z < 0)
                    {
                        t.localScale = new Vector3(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z));
                    }
                }
            }
        }
    }
}
#endif