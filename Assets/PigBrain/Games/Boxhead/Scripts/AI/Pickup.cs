using System;
using System.Collections;
using System.Linq;
using pigbrain.core.Collections;
using pigbrain.core.Inspector;
using pigbrain.core.UnityObject;
using pigbrain.game.Boxhead.Audio;
using Unity.Mathematics;
using UnityEngine;

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

        Collider trigger; // awake
        bool triggered; // Start 
        float startTime; // Start

        public static event Action<Pickup> OnCreated;

        void Awake()
        {
            trigger = GetComponents<Collider>().FirstOrDefault(c => c.isTrigger);
            if (!trigger) Debug.LogError("Pickups require a Collider as a Trigger");
        }

        void Start()
        {
            startTime = Time.time;

            if (expiresAfter > 0)
            {
                IEnumerator Disappear()
                {
                    const float DisappearTime = 0.5f;
                    float expires = Mathf.Max(expiresAfter - DisappearTime, 0);
                    float transition = DisappearTime;
                    yield return new WaitForSeconds(expires);
                    Vector3 start = transform.localScale, end = (float3)0.001f;
                    yield return new OverTime(transition, (t) => transform.localScale = Vector3.Lerp(start, end, t));
                    Destroy(gameObject);
                }
                StartCoroutine(Disappear());
            }
            OnCreated?.Invoke(this);
        }

        void OnTriggerEnter(Collider other) => OnTrigger(other);
        void OnTriggerStay(Collider other) => OnTrigger(other);
        void OnTrigger(Collider other)
        {
            if (triggered) return;
            if (traits.HasFlag(Traits.Delay) && Time.time - startTime < 0.5f) return;
            if (!traits.HasFlag(Traits.Trigger) && other.isTrigger) return;
            if (!traits.HasFlag(Traits.AllowAny) && !(other.TryGetComponent<IPickup>(out var pickupBy) && pickupBy.OnPickup(this))) return;
            trigger.enabled = false;
            triggered = true;
            pickupClip.Play(transform.position);
            commands?.Invoke(other.transform);
            if (traits.HasFlag(Traits.MoveTo)) StartCoroutine(MoveToTarget(other));
            else Destroy(gameObject);
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
            Destroy(gameObject);
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
        [InspectorName("")] Other = 1 << 8,
    }
}
