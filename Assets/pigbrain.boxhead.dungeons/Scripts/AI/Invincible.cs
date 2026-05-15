using System;
using System.Collections;
using pigbrain.core.UnityObject;
using UnityEngine;

namespace pigbrain.game.Boxhead
{
    [RequireComponent(typeof(Health))]
    public class Invincible : MonoBehaviour
    {
        [SerializeField][Range(0.1f, 120)] float duration = 5;
        [SerializeField] PrefabObject effect;

        public void Activate(float duration)
        {
            enabled = false;
            this.duration = duration;
            enabled = true;
        }

        void OnEnable() => StartCoroutine(Run());
        void OnDisable()
        {
            GetComponent<Health>().onDamage -= OnDamage;
            StopAllCoroutines();
        }

        IEnumerator Run()
        {
            GetComponent<Health>().onDamage += OnDamage;
            // using var _ = new LifetimeScope(effect.Instantiate(transform));
            effect.Instantiate(transform);
            yield return new WaitForSeconds(duration);
            enabled = false;
        }

        void OnDamage(Health.Damage damage) =>
            damage.Cancel();
    }
}