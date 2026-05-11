using UnityEngine;
using System.Linq;
using pigbrain.core.Geom;
using System.Collections;
using pigbrain.core.Collections;

namespace pigbrain.game.Boxhead
{
    public class ShotMove : Shot, IShield
    {
        [Header("Move")]
        [SerializeField][Range(0.01f, 5)] float radius = 1;
        [SerializeField] Traits traits;

        bool hitShield = false;

        public float OnShield(Vector3 force)
        {
            if (!traits.HasFlag(Traits.UseShield)) return 0;
            hitShield = true;
            return effect && effect.TryGetComponent(out IDamage d) ? d.damage : 0;
        }

        protected override void ProjectileStart()
        {
            base.ProjectileStart();
            hitShield = false;
        }

        #region Update
        protected override IEnumerator ProjectileUpdate()
        {
            var speed = transform.localScale.Min() * this.speed;
            float radius = transform.localScale.Min() * this.radius;
            float distance = target ? (target.center - transform.position).WithY(0).magnitude : 0;
            while (enabled)
            {
                float frameSpeed = speed * Time.deltaTime;
                var direction = frameSpeed * transform.forward;
                if (Physics.SphereCast(transform.position, radius, direction,
                    out var hit, direction.magnitude, damageMask, QueryTriggerInteraction.Ignore))
                {
                    transform.position = hit.point;
                    yield break;
                }

                transform.position += direction;

                if (target)
                {
                    distance -= frameSpeed;
                    if (traits.HasFlag(Traits.TravelTargetDistance) && distance <= 0) yield break;
                }

                if (hitShield)
                {
                    hitShield = false;
                    yield break;
                }

                yield return new WaitForNextUpdate();
            }
        }
        #endregion

        enum Traits
        {
            None = 0,
            TravelTargetDistance = 1 << 0,
            Another = 1 << 1,
            UseShield = 1 << 2
        }
    }
}
