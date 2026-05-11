// Assets/Editor/CreateStandardFolders.cs
using System.IO; // for Path
using UnityEditor;
using UnityEngine;

public static class CreateContentFolder
{
    // [MenuItem("Assets/Create/Create Content Folder", validate = true)]
    // private static bool CreateFolderValidate()
    // {
    //     return true;
    // }

    [MenuItem("Assets/Create/Create Content Folder", priority = -10000, validate = false)]
    private static void CreateFolderAction()
    {
        var guids = Selection.assetGUIDs;
        if (guids == null || guids.Length == 0) return;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var folder = AssetDatabase.IsValidFolder(path) ? path : Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(folder)) folder = "Assets";

            var name = Path.GetFileNameWithoutExtension(path);
            var target = $"{folder}/{name}";

            if (!AssetDatabase.IsValidFolder(target))
                AssetDatabase.CreateFolder(folder, name);
        }

        AssetDatabase.Refresh();
    }
}

public static class CreateStandardFolders
{
    // Adjust this list to your taste, order preserved                                                             // comment at end
    private static readonly string[] RootFolders =
    {
        "Scenes",
        "Scripts",
        "Materials",
        "Textures",
        "Models",
        "Prefabs",
        // "Audio",
        // "Animations",
        // "Shaders",
        // "UI",
        // "Editor",
        // "Plugins"
    }; // comment at end

    // Optional nested structure, key is parent folder relative to the chosen root                                 // comment at end
    private static readonly (string parent, string child)[] Nested =
    {
        // ("Scripts", "Runtime"),
        // ("Scripts", "Editor"),
        // ("Audio", "Music"),
        // ("Audio", "SFX")
    }; // comment at end

    [MenuItem("Assets/Create/Standard Folder Structure", priority = -10000)]
    private static void CreateUnderSelection()
    {
        var targets = Selection.assetGUIDs; // all selected assets                                                  // comment at end
        if (targets == null || targets.Length == 0)
        {
            // CreateAt(ProjectWindowUtil.GetActiveFolderPath()); // fallback to active folder or Assets               // comment at end
            return; // comment at end
        }

        foreach (var guid in targets)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid); // asset or folder path                                 // comment at end
            var targetPath = AssetDatabase.IsValidFolder(path) ? path : Path.GetDirectoryName(path); // ensure folder // comment at end
            if (string.IsNullOrEmpty(targetPath)) targetPath = "Assets"; // safety                                   // comment at end
            CreateAt(targetPath); // create structure here                                                          // comment at end
        }
    }

    // Enable only in Project window context                                                                        // comment at end
    [MenuItem("Assets/Create/Standard Folder Structure", validate = true)]
    private static bool CreateUnderSelection_Validate()
    {
        return true; // always available in Project window                                                          // comment at end
    }

    private static void CreateAt(string rootPath)
    {
        if (string.IsNullOrEmpty(rootPath)) rootPath = "Assets"; // safety                                          // comment at end

        Undo.IncrementCurrentGroup(); // start undo group                                                           // comment at end
        var undoGroup = Undo.GetCurrentGroup(); // id                                                               // comment at end

        foreach (var f in RootFolders)
            EnsureFolder(rootPath, f); // top level                                                                 // comment at end

        foreach (var pair in Nested)
            EnsureFolder(Path.Combine(rootPath, pair.parent).Replace("\\", "/"), pair.child); // nested             // comment at end

        AssetDatabase.Refresh(); // update Project view                                                             // comment at end
        Undo.CollapseUndoOperations(undoGroup); // single undo step                                                 // comment at end

        Debug.Log($"Standard folders created under: {rootPath}"); // message                                        // comment at end
    }

    // Creates child folder under parent if missing                                                                 // comment at end
    private static void EnsureFolder(string parent, string child)
    {
        var normalizedParent = parent.Replace("\\", "/"); // normalize                                              // comment at end
        var full = $"{normalizedParent}/{child}"; // full path                                                      // comment at end
        if (AssetDatabase.IsValidFolder(full)) return; // already exists                                            // comment at end

        // Ensure parent exists, but do not auto-create outside Assets                                              // comment at end
        if (!AssetDatabase.IsValidFolder(normalizedParent))
        {
            Debug.LogWarning($"Parent folder missing: {normalizedParent}"); // warn                                  // comment at end
            return; // comment at end
        }

        AssetDatabase.CreateFolder(normalizedParent, child); // create folder                                       // comment at end
    }
}
