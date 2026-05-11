using System.Collections.Generic;
using System.IO;
using System.Linq;
using pigbrain.core.Project;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

// #region Test
// public class StripperTest
// {
//     [MenuItem("Tools/Test Stripper")]
//     static void Test()
//     {
//         Debug.Log("BuildStripper TEST START");
//         var bs = new BuildStripperExecution();
//         bs.OnPreprocessBuild(null);
//         bs.OnPostprocessBuild(null);
//         Debug.Log("BuildStripper TEST END");
//     }
// }
// #endregion

#region Stripper Data
public class BuildStripper : ScriptableObject
{
    public List<Element> elements = new();

    [System.Serializable]
    public class Element
    {
        public string key;
        public Object[] objects;
        public Element(Object stripper, Object[] objects)
        {
            key = GetKey(stripper);
            this.objects = objects;
        }
    }

    public static string GetKey(Object target)
    {
        string path = AssetDatabase.GetAssetPath(target);
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning($"Invalid stripper path {target}");
            return null;
        }

        string guid = AssetDatabase.AssetPathToGUID(path);

        string subPath = "";
        if (target is Component c)
        {
            for (var t = c.transform; t != null; t = t.parent)
                subPath = t.GetSiblingIndex() + "/" + subPath;
            subPath += c.GetType().FullName + "#"
                + System.Array.IndexOf(c.GetComponents(c.GetType()), c);
        }
        else subPath = target.GetType().FullName;
        return guid + "|" + subPath;
    }

    string path => $"{ProjectBuiilder.TempAssetPath}/BuildStripper.asset";
    public void Write()
    {
        ProjectBuiilder.CreateTempAssetPath();
        if (AssetDatabase.LoadAssetAtPath<BuildStripper>(path) != null)
            AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(this, path);
    }

    public Dictionary<string, Object[]> Read()
    {
        var existing = AssetDatabase.LoadAssetAtPath<BuildStripper>(path);
        EditorUtility.CopySerialized(existing, this);
        return elements.ToDictionary(e => e.key, e => e.objects);
    }

    public void Delete() => AssetDatabase.DeleteAsset(path);
}
#endregion

public class BuildStripperExecution : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    #region  Pre Process
    public int callbackOrder => -1000;
    public void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
    {
        // RemoveRuntimeDebuggin();
        var data = ScriptableObject.CreateInstance<BuildStripper>();
        Debug.Log("Pre build stripping!");
        foreach (var stripper in GetBuildStrippers())
        {
            data.elements.Add(
                new(stripper, (stripper as IBuildStripper).Prebuild().ToArray()));
            EditorUtility.SetDirty(stripper as Object);
        }
        data.Write();
        AssetDatabase.SaveAssets();
    }
    #endregion

    #region  Post Process
    public void OnPostprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
    {
        Debug.Log("Post build stripping!");
        var data = ScriptableObject.CreateInstance<BuildStripper>();
        var lookup = data.Read();
        if (lookup == null) return;
        foreach (var stripper in GetBuildStrippers())
        {
            var key = BuildStripper.GetKey(stripper);
            if (lookup.TryGetValue(key, out Object[] objects))
            {
                (stripper as IBuildStripper).Postbuild(objects);
                EditorUtility.SetDirty(stripper as Object);
            }
            else Debug.LogWarning($"Stripper failed {stripper}");
        }
        AssetDatabase.SaveAssets();
        data.Delete();
    }
    #endregion

    #region  Build Strippers
    static IEnumerable<Object> GetBuildStrippers()
    {
        // Prefabs (correct way, not MonoBehaviour assets)
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab) continue;

            foreach (var mb in prefab.GetComponentsInChildren<MonoBehaviour>(true))
                if (mb is IBuildStripper s)
                    yield return mb;
        }

        // ScriptableObjects
        foreach (var guid in AssetDatabase.FindAssets("t:ScriptableObject"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (so is IBuildStripper s)
                yield return so;
        }
    }
    #endregion

    #region Remove Debug
    public void RemoveRuntimeDebugging()
    {
        var path = "Library/PackageCache";

        if (!Directory.Exists(path)) return;
        foreach (var dir in Directory.GetDirectories(path, "com.unity.render-pipelines.core*"))
        {
            var debugPath = Path.Combine(dir, "Runtime/Debugging");
            if (!Directory.Exists(debugPath)) continue;
            var disabled = debugPath + "_STRIPPED";
            if (Directory.Exists(disabled)) Directory.Delete(disabled, true);
            Directory.Move(debugPath, disabled);
            Debug.Log("DebugUI stripped from SRP");
        }
    }
    #endregion
}

// Scene
// foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
//     if (mb is IBuildStripper s)
//         yield return s;

// public void RemoveRuntimeDebuggin()
// {
//     var path = "Library/PackageCache";

//     if (!Directory.Exists(path)) return;
//     foreach (var dir in Directory.GetDirectories(path, "com.unity.render-pipelines.core*"))
//     {
//         var debugPath = Path.Combine(dir, "Runtime/Debugging");
//         if (!Directory.Exists(debugPath)) continue;
//         var disabled = debugPath + "_STRIPPED";
//         if (Directory.Exists(disabled)) Directory.Delete(disabled, true);
//         Directory.Move(debugPath, disabled);
//         Debug.Log("DebugUI stripped from SRP");
//     }
// }
