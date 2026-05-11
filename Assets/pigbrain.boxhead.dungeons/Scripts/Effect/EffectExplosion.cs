using System.Collections;
using UnityEngine;
using static pigbrain.core.UnityPhysics.PhysicsUtility;
using pigbrain.core.Collections;
using System.Collections.Generic;
using System;
using pigbrain.core.Geom;
using pigbrain.core.AI;
using pigbrain.core.Audio;
using pigbrain.core.UnityObject;

namespace pigbrain.game.Boxhead
{
    public class EffectExplosion : MonoBehaviour, IDamage //, IOwner
    {
        [SerializeField] LayerMask layerMask;
        [SerializeField][Range(0.2f, 50)] float radius = 5;
        [SerializeField][Range(0.01f, 1)] float duration = 0.5f;
        [SerializeField][Range(1, 100)] float damage = 100;
        [SerializeField] bool explodeModel = true;
        [SerializeField] MassForce massForce;
        [SerializeField] ClipLink sound;

        float IDamage.damage { get => damage; set => damage = value; }

        void OnValidate()
        {
            massForce = GetComponent<MassForce>();
        }

        IEnumerator Start()
        {
            layerMask &= ~(1 << gameObject.layer);
            sound.Play(transform.position);

            HashSet<GameObject> used = new();
            float damage = Mathf.Pow(this.damage, transform.localScale.Min());
            float radius = transform.localScale.Min() * this.radius;
            float speed = radius / duration;

            if (massForce) CameraShake.Apply(transform.position, massForce.mass);

            for (float time = Time.time, f = 0; f < 1;)
            {
                f = Mathf.Clamp01((Time.time - time) / duration);
                float timeDamage = damage * Mathf.Min(1, (1 - f) * 2);
                // Health.ApplyDamageSphere(affector, transform.position, radius * f, damage, layerMask);

                int count = Physics.OverlapSphereNonAlloc(transform.position, radius * f, Colliders1000, layerMask,
                    QueryTriggerInteraction.Ignore);

                for (int i = 0; i < count; i++)
                {
                    var collider = Colliders1000[i];
                    var affected = collider.gameObject;

                    // xif (!effectOwnLayer && collider.gameObject.layer == gameObject.layer) continue;

                    if (used.Add(affected))
                    {
                        if (massForce && affected.TryGetComponent(out NavAgentForce agentForce))
                        {
                            Vector3 delta = collider.bounds.center - transform.position;
                            float scalar = Mathf.Clamp01(1 - delta.magnitude / radius);
                            agentForce.ApplyForce(massForce.mass, delta.normalized * scalar * speed);
                        }

                        if (affected.transform.TryApplyDamage(new(transform), timeDamage, out var health))
                        {
                            if (health.GetDamageMaterial() == Health.DamageMaterial.Shield)
                                used.Remove(affected);

                            if (explodeModel && health.isDead)
                            {
                                ExplodeModel.CreateInstance(affected.name, affected.transform, transform.position, 20, 2);
                                affected.Hide();
                                Destroy(affected, 0.25f);
                            }
                        }
                    }
                }
                yield return new WaitForNextUpdate();
            }
            Destroy(gameObject);
        }

        #region  Gizmos
        [SerializeField][HideInInspector] bool showGizmos = true;
        [ContextMenu("Show Gizmos")] void ToogleGizmos() => showGizmos = !showGizmos;

        void OnDrawGizmos()
        {
            // if (!showGizmos) return;

            Gizmos.DrawWireSphere(transform.position, radius);
        }
        #endregion
    }
}