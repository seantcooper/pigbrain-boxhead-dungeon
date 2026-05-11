using UnityEngine;

namespace pigbrain.core.UnityObject
{
    [RequireComponent(typeof(Rigidbody))]
    public class ApplyForce : MonoBehaviour
    {
        [SerializeField] Vector3 force = Vector3.forward;
        [SerializeField] ForceMode mode = ForceMode.Impulse;
        [SerializeField] Frequency frequency = Frequency.Start;
        [SerializeField] Rigidbody rb;

        void OnValidate()
        {
            if (!rb) rb = GetComponent<Rigidbody>();
        }

        void Start()
        {
            if (!rb) rb = GetComponent<Rigidbody>();
            if (frequency == Frequency.Start)
            {
                rb.AddForce(force, mode);
                enabled = false;
            }
        }

        void FixedUpdate()
        {
            rb.AddForce(force, mode);
        }

        enum Frequency
        {
            Start,
            EveryFixed,
        }
    }
}