using UnityEngine;
namespace pigbrain.core.UnityObject
{
    public class AssetIdentity : MonoBehaviour
    {
        [SerializeField] string guid;

        // public GameObject prefab => Catalog.Get(this);
        public string GetGuid() => guid;

#if UNITY_EDITOR
        void OnValidate()
        {
            if (!Application.isPlaying
                && UnityEditor.AssetDatabase.GetAssetPath(this) is string path
                && !string.IsNullOrEmpty(path)
                && UnityEditor.AssetDatabase.AssetPathToGUID(path) is string guid
                && this.guid != guid)
            {
                this.guid = guid;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }

    public class IdentityScriptableObject : ScriptableObject
    {
        [SerializeField] string guid;
        public string GetGuid() => guid;

#if UNITY_EDITOR
        void OnValidate()
        {
            if (!Application.isPlaying
                && UnityEditor.AssetDatabase.GetAssetPath(this) is string path
                && !string.IsNullOrEmpty(path)
                && UnityEditor.AssetDatabase.AssetPathToGUID(path) is string guid
                && this.guid != guid)
            {
                this.guid = guid;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}