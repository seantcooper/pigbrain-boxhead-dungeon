using UnityEngine;
using System.Collections;
using pigbrain.core.Collections;
using pigbrain.generated;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Geom;

namespace pigbrain.game.Boxhead
{
    [RequireComponent(typeof(LineRenderer))]
    public class ShotLaser : Shot
    {
        [Header("Laser")]
        [SerializeField] float length = 100;
        [SerializeField][Range(0, 1)] float tailSpeedRatio = 0.5f;
        [SerializeField][Range(0, 16)] int bounces = 4;
        [SerializeField] LineRenderer line;
        [SerializeField] bool fullDamage;
        Vector3[] buffer;

        protected override void OnValidate()
        {
            base.OnValidate();
            line = GetComponent<LineRenderer>();
        }

        #region Update
        protected override IEnumerator ProjectileUpdate()
        {
            var lines = BounceLine(transform.position, transform.forward.WithY(0), bounces).ToArray();
            if (lines.Length < 2) yield break;

            float head = 0f, tail = 0f;
            while (true)
            {
                float speed = this.speed * Time.deltaTime;
                float previousHead = head;
                head += speed;
                tail = Mathf.Max(tail + speed * tailSpeedRatio, head - length);

                // Damage only new part
                if (!fullDamage && head > previousHead)
                {
                    float i1 = GetIndex(lines, previousHead), i2 = GetIndex(lines, head);
                    Vector3 p1 = GetPosition(lines, i1), p2 = GetPosition(lines, i2);
                    Health.ApplyDamageRay(new Affector(transform), p1, p2, damage, damageMask);
                }

                int count = GetPositions(lines, tail, head);
                line.positionCount = count;
                line.SetPositions(buffer);

                if (fullDamage)
                {
                    for (int n = line.positionCount, i = 1; i < n; i++)
                        Health.ApplyDamageRay(new Affector(transform), buffer[i - 1], buffer[i], damage, damageMask);
                }

                if (tail >= lines[^1].d) break;
                yield return new WaitForNextUpdate();
            }
        }
        #endregion

        #region Get Position
        int GetPositions((Vector3 p, float d)[] lines, float d1, float d2)
        {
            float i1 = GetIndex(lines, d1), i2 = GetIndex(lines, d2);
            int start = (int)i1, end = (int)i2;
            int needed = 2 + Mathf.Max(0, end - start);

            if (buffer == null || buffer.Length < needed)
                buffer = new Vector3[needed];

            int count = 0;
            buffer[count++] = GetPosition(lines, i1);
            for (int i = start + 1; i <= end; i++)
                buffer[count++] = lines[i].p;
            buffer[count++] = GetPosition(lines, i2);
            return count;
        }

        static Vector3 GetPosition((Vector3 p, float d)[] lines, float index) =>
            Vector3.Lerp(lines[(int)index].p, lines[(int)index + 1].p, index % 1);

        static float GetIndex((Vector3 p, float d)[] lines, float d)
        {
            d = Mathf.Clamp(d, 0, lines[^1].d);
            for (int i = 0; i < lines.Length; i++)
                if (d < lines[i].d)
                    return i == 0 ? 0 : i - 1 + (d - lines[i - 1].d) / (lines[i].d - lines[i - 1].d);
            return lines.Length - 2 + 0.999f;
        }
        #endregion

        #region Bounce
        static IEnumerable<(Vector3 p, float d)> BounceLine(Vector3 position, Vector3 direction, int bounces)
        {
            float sum = 0f;
            yield return (position, 0f);

            for (int i = 0; i <= bounces; i++)
            {
                if (Physics.Raycast(new Ray(position, direction), out RaycastHit hit, 100, (int)LayerFlags.Wall))
                {
                    sum += (hit.point - position).magnitude;
                    yield return (hit.point, sum);

                    direction = Vector3.Reflect(direction, hit.normal).normalized;
                    position = hit.point + direction * 0.01f;
                }
                else yield break;
            }
        }
        #endregion
    }
}