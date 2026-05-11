using System;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Analysis;
using pigbrain.core.Collections;
using UnityEngine;

public class AnimatorMeshData : ScriptableObject
{
    [SerializeField] GameObject model;
    [SerializeField] Animation[] animations;

    Dictionary<string, Animation> lookup;

    void Initialize()
    {
        if (lookup != null) return;
        Bake();
        lookup = animations
            .Where(a => !string.IsNullOrEmpty(a.name))
            .ToDictionary(k => NameKey(k.name), v => v);
    }

    string NameKey(string name) => name.ToLower();

    public Animation GetAnimation(string name)
    {
        Initialize();
        string key = NameKey(name);
        if (key == "move") key = "walk";
        return lookup[key];
    }

    void Bake()
    {
        if (!model || animations == null || animations.Length == 0) return;

        var instance = Instantiate(model);
        if (!instance.TryGetComponent<Animator>(out _))
            instance.AddComponent<Animator>();
        instance.hideFlags = HideFlags.HideAndDontSave;

        var root = instance.transform;
        var filters = instance.GetComponentsInChildren<MeshFilter>();
        var combines = new List<CombineInstance>();

        var p1 = Profiler.Start();
        foreach (var anim in animations)
        {
            if (!anim.clip || anim.fps <= 0) continue;

            var frames = Mathf.CeilToInt(anim.clip.length * anim.fps);
            anim.frames = new Mesh[frames];

            for (int i = 0; i < frames; i++)
            {
                var time = Mathf.Min(anim.clip.length - (1f / anim.fps),
                    i / (float)anim.fps);
                anim.clip.SampleAnimation(instance, time);

                combines.Clear();

                foreach (var filter in filters)
                {
                    if (!filter.sharedMesh || !filter.TryGetComponent<MeshRenderer>(out var r) || !r.enabled) continue;
                    combines.Add(new CombineInstance
                    {
                        mesh = filter.sharedMesh,
                        transform = root.worldToLocalMatrix * filter.transform.localToWorldMatrix
                    });
                }

                var mesh = new Mesh { name = $"{anim.clip.name}_{i:000}" };
                mesh.CombineMeshes(combines.ToArray(), true, true);
                mesh.tangents = null;
                mesh.RecalculateBounds();
                anim.frames[i] = mesh;
            }
            // VisualizeFrames(anim.frames, material);
        }
        DestroyImmediate(instance);
        Profiler.StopAndLog(p1, $"{name}::Bake");
    }

    [Serializable]
    public class Animation
    {
        public string name;
        public int fps = 15;
        public AnimationClip clip;
        public bool loop = true;
        [NonSerialized] public Mesh[] frames;

        public long GetTotalMemory()
        {
            long memory = 0;
            if (!frames.IsNullOrEmpty())
                foreach (var frame in frames)
                    memory += GetMeshMemory(frame);
            return memory;
        }
        public static implicit operator bool(Animation empty) => empty != null;
    }

    static long GetMeshMemory(Mesh mesh)
    {
        if (!mesh) return 0;

        var vc = mesh.vertexCount;
        var tc = mesh.triangles.Length;

        long size = 0;
        size += vc * 12L * 2; // vertices/normals
        size += tc * 4L;  // indices
        if (mesh.uv.Length > 0) size += vc * 8L;

        return size;
    }

}

// #if UNITY_EDITOR
//                 AssetDatabase.AddObjectToAsset(mesh, this);
// #endif

// #if UNITY_EDITOR
//         EditorUtility.SetDirty(this);
//         AssetDatabase.SaveAssets();
// #endif
