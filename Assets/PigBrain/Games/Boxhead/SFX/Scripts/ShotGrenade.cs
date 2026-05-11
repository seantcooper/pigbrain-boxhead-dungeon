using System.Collections;
using pigbrain.core.Collections;
using UnityEngine;

namespace pigbrain.game.Boxhead
{
    [RequireComponent(typeof(Rigidbody))]
    public class ShotGrenade : Shot
    {
        [Header("Grenade")]
        [SerializeField] float fuseDuration = 3;
        [SerializeField][HideInInspector] Rigidbody rb;

        protected override void OnValidate()
        {
            base.OnValidate();
            rb = GetComponent<Rigidbody>();
        }

        protected override IEnumerator ProjectileUpdate()
        {
            var force = transform.forward * speed;
            rb.AddForce(force, ForceMode.Impulse);
            yield return new WaitForSeconds(fuseDuration);
        }
    }
}
