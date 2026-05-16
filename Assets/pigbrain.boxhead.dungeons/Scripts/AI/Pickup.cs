#pragma warning disable UDR0001
using System;
using System.Collections;
using System.Linq;
using pigbrain.core.Collections;
using pigbrain.core.Inspector;
using pigbrain.core.Audio;
using Unity.Mathematics;
using UnityEngine;
using pigbrain.core.UnityObject;
using pigbrain.core.Geom;

namespace pigbrain.game.Boxhead
{
    public class Pickup : MonoBehaviour
    {
        [Header("Pickup")]
        [SerializeField] Traits traits;
        [SerializeField] CommandContainer commands;
        [SerializeField] ClipLink pickupClip;
        [SerializeField] float expiresAfter = 0;
        [SerializeField][ReadOnly] Vector3 velocity;

        Collider trigger;
        float startTime, endTime; // Start

        public static event Action<Pickup> OnCreated;
        public static event Action<Pickup, Transform> OnPickup;
        public static int PickupCount;
        AssetIdentity id;

        // requires restoring
        bool wasMerged, triggered;
        Command[] backupCommands;

        void Awake()
        {
            trigger = GetComponents<Collider>().FirstOrDefault(c => c.isTrigger);
            if (!trigger) Debug.LogError("Pickups require a Collider as a Trigger");
        }

        void Start()
        {
            id = GetComponent<AssetIdentity>();
            startTime = Time.time;
            Expiration();
            OnCreated?.Invoke(this);
        }

        void Remove()
        {
            if (traits.HasFlag(Traits.Disable)) gameObject.SetActive(false);
            else Destroy(gameObject);
        }

        void OnTriggerEnter(Collider other) => OnTrigger(other);
        void OnTriggerStay(Collider other) => OnTrigger(other);
        void OnTrigger(Collider other)
        {
            if (triggered) return;
            if (!traits.HasFlag(Traits.Trigger) && other.isTrigger) return;
            if (traits.HasFlag(Traits.Delay) && Time.time - startTime < 0.5f) return;

            MoveToMerge(other);

            if (!traits.HasFlag(Traits.AllowAny) && !(other.TryGetComponent<IPickup>(out var pickupBy) && pickupBy.OnPickup(this))) return;
            trigger.enabled = false;
            triggered = true;
            pickupClip.Play(transform.position);
            commands?.Invoke(other.transform);
            OnPickup?.Invoke(this, other.transform);
            PickupCount++;
            if (traits.HasFlag(Traits.MoveTo)) StartCoroutine(MoveToTarget(other));
            else Remove();
        }

        void MoveToMerge(Collider other)
        {
            if (!traits.HasFlag(Traits.Merge)) return;
            if (other.gameObject.layer != gameObject.layer) return;
            if (!id.Equals(other)) return;
            if (!TryGetComponent(out Rigidbody rb)) return;

            Vector3 dir = (other.bounds.center - transform.position).normalized;
            rb.linearVelocity += dir * 5f;

            var distsq = (transform.position - other.transform.position).sqrMagnitude;
            float s1 = transform.localScale.Min(), s2 = other.transform.localScale.Min();
            float d = (s1 + s2) * 0.7f;
            if (distsq > d * d) return;

            var otherPickup = other.GetComponent<Pickup>();
            if (otherPickup.wasMerged || wasMerged) return;

            if (expiresAfter >= 0) endTime = Mathf.Max(endTime, otherPickup.endTime);

            backupCommands ??= commands.commands.ToArray();
            commands.Combine(otherPickup.commands);

            var scale = Mathf.Pow((s1 * s1 * s1) + (s2 * s2 * s2), 1f / 3f);
            transform.localScale = Vector3.one * scale;

            otherPickup.wasMerged = true;
            rb.linearVelocity *= 0.1f;

            otherPickup.Remove();
        }

        void Expiration()
        {
            if (expiresAfter <= 0) return;
            endTime = startTime + expiresAfter;
            IEnumerator Disappear()
            {
                const float DisappearTime = 0.5f;
                yield return new WaitUntil(() => Time.time >= endTime - DisappearTime);

                Vector3 start = transform.localScale, end = (float3)0.001f;
                yield return new OverTime(DisappearTime, (t) => transform.localScale = Vector3.Lerp(start, end, t));

                Remove();
            }
            StartCoroutine(Disappear());
        }

        IEnumerator MoveToTarget(Collider collider)
        {
            Vector3 startPosition = transform.position;
            if (this.TryGetComponent(out Rigidbody rb)) rb.isKinematic = true;
            for (float time = Time.time, t = 0; t < 1; t = (Time.time - time) / 0.2f)
            {
                if (!collider) break;
                transform.position = Vector3.Lerp(startPosition, collider.bounds.center, t);
                yield return new WaitForNextUpdate();
            }
            Remove();
        }
    }

    public interface IPickup
    {
        bool OnPickup(Pickup pickup);
    }

    [Flags]
    enum Traits
    {
        AllowAny = 1 << 0,
        MoveTo = 1 << 1,
        Trigger = 1 << 2,
        Delay = 1 << 3,
        Merge = 1 << 4,
        Disable = 1 << 5,
        [InspectorName("")] Other = 1 << 8,
    }
}
