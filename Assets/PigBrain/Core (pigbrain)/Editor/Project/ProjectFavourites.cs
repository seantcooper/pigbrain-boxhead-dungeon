using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using pigbrain.core.Collections;
using System.Linq;

[Serializable]
public class ProjectFavourites : ScriptableObject
{
    public List<string> guids = new();
}
public static class FavouriteService
{
    const string Path = "Assets/Editor/ProjectFavourites.asset";
    static ProjectFavourites Data;
    static HashSet<string> Cache;
    public static event Action Changed;
    public static List<GameObject> Favourites;

    public static void AddChangeListener(Action changed)
    {
        Refresh();
        Changed += changed;
        Changed?.Invoke();
    }
    public static void RemoveChangeListener(Action changed) => Changed -= changed;

    public static bool HasFavourites()
    {
        if (Favourites == null) Refresh();
        return !Favourites.IsNullOrEmpty();
    }

    static void Ensure()
    {
        Data ??= AssetDatabase.LoadAssetAtPath<ProjectFavourites>(Path);
        if (!Data)
        {
            Data = ScriptableObject.CreateInstance<ProjectFavourites>();
            AssetDatabase.CreateAsset(Data, Path);
            AssetDatabase.SaveAssets();
        }
        Cache ??= new HashSet<string>(Data.guids);
    }

    public static bool IsFavourite(string guid)
    {
        Ensure();
        return Cache.Contains(guid);
    }

    public static bool GetGUID(GameObject gameObject, out string guid)
    {
        var path = AssetDatabase.GetAssetPath(gameObject);
        if (string.IsNullOrEmpty(path)) { guid = null; return false; }
        guid = AssetDatabase.AssetPathToGUID(path);
        return !string.IsNullOrEmpty(guid);
    }

    public static void Set(string guid, bool value)
    {
        Ensure();
        if (value) Cache.Add(guid);
        else Cache.Remove(guid);
        Data.guids = Cache.ToList();
        Refresh();
        EditorUtility.SetDirty(Data);
        AssetDatabase.SaveAssets();
        Changed?.Invoke();
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }

    public static void Refresh()
    {
        Ensure();
        Favourites = Cache
            .Select(g => AssetDatabase.GUIDToAssetPath(g))
            .Select(p => AssetDatabase.LoadAssetAtPath<GameObject>(p))
            .Where(p => p)
            .ToList();
    }
}
