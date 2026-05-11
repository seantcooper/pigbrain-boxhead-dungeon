using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Analysis;
using pigbrain.core.Collections;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] AnimatorMesh animatorMesh;

    void OnValidate()
    {
        animator = GetComponent<Animator>();
        animatorMesh = GetComponent<AnimatorMesh>();
    }

    public void SetTrigger(string trigger)
    {
        if (animator) animator.SetTrigger(trigger);
        else animatorMesh.SetAnimation(trigger);
    }

    public void SetFloat(string key, float value)
    {
        if (!animator) return; // throw new NotImplementedException("Not supported!");
        animator.SetFloat(key, value);
    }

    // Speed / TimeScale
    public float speed
    {
        get => animator ? animator.speed : animatorMesh.speed;
        set
        {
            if (animator) animator.speed = value;
            else animatorMesh.speed = value;
        }
    }
}