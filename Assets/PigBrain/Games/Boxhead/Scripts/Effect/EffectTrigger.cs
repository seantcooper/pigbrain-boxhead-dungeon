using UnityEngine;
using System;
using pigbrain.game.Boxhead.Navigation;
using UnityEngine.Events;

namespace pigbrain.game.Boxhead
{
    public class EffectTrigger : MonoBehaviour, NavMapLayerFear.IThreat
    {
        [SerializeField] Damage damageMask = Damage.Enter;
        [SerializeField][Range(1, 100)] public float damage = 100;
        [SerializeField][Range(0, 1)] float threat = 0;
        float NavMapLayerFear.IThreat.threat => threat;

        [SerializeField] Events events;
        [Serializable] class Events { public UnityEvent onTriggerEnter, onTriggerStay, onTriggerExit; }

        // public IAffector affector;

        void OnEnable() => NavMap.TryGetLayer<NavMapLayerFear>().Add(transform, this);
        void OnDisable() => NavMap.TryGetLayer<NavMapLayerFear>().Remove(transform);

        void ApplyDamage(Collider other, Damage flag, UnityEvent ev, float scalar = 1)
        {
            if (!enabled || !damageMask.HasFlag(flag) || other.isTrigger) return;
            other.transform.TryApplyDamage(new Affector(transform, damage, transform.forward),
                damage * scalar, out Health _);
            ev?.Invoke();
        }

        void OnTriggerEnter(Collider other) =>
            ApplyDamage(other, Damage.Enter, events.onTriggerEnter);
        void OnTriggerStay(Collider other) =>
            ApplyDamage(other, Damage.Stay, events.onTriggerStay, Time.deltaTime);
        void OnTriggerExit(Collider other) =>
            ApplyDamage(other, Damage.Exit, events.onTriggerExit);

        [Flags]
        enum Damage
        {
            Enter = 1 << 0,
            Stay = 1 << 1,
            Exit = 1 << 2,
        }
    }
}