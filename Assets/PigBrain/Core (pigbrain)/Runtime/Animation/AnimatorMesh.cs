using System;
using System.Collections;
using pigbrain.core.Inspector;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class AnimatorMesh : MonoBehaviour
{
    [SerializeField] string startAnimation = "walk";
    [SerializeField] internal float speed;
    [SerializeField][InlineScriptableObject] AnimatorMeshData data;

    float startTime;
    int frameIndex = -1;
    AnimatorMeshData.Animation currentAnimation;


    [HideInInspector][SerializeField] MeshFilter filter;
    void OnValidate() => filter = GetComponent<MeshFilter>();

    void Start()
    {
        SetAnimation(startAnimation);
        StartCoroutine(Animate());
    }

    public void Move(float value) { }

    public void SetAnimation(string name)
    {
        var anim = data.GetAnimation(name);
        if (!anim)
        {
            Debug.LogError($"Animation {name} not found!");
            return;
        }

        Stop();

        startTime = Time.time;
        frameIndex = -1;
        currentAnimation = anim;

        Play();
    }

    public void Stop()
    {
        if (animating != null) StopCoroutine(animating);
        animating = null;
        currentAnimation = null;
    }

    void Play()
    {
        animating = StartCoroutine(Animate());
    }

    Coroutine animating;
    IEnumerator Animate()
    {
        while (enabled && currentAnimation)
        {
            float time = Time.time - startTime;
            int i = Mathf.FloorToInt(time * currentAnimation.fps * speed);
            SetFrameIndex(currentAnimation.loop
                ? i % currentAnimation.frames.Length
                : Math.Min(currentAnimation.frames.Length - 1, i));
            yield return null;
        }
        animating = null;
    }

    void SetFrameIndex(int index)
    {
        if (index == frameIndex) return;
        frameIndex = index;
        filter.sharedMesh = currentAnimation.frames[frameIndex];
    }
}