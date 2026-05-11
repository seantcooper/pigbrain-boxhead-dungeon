using System;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Geom;
using pigbrain.core.UnityObject;
using pigbrain.generated;
using UnityEngine;

namespace pigbrain.game.Boxhead
{
    public class PickupSwitch : MonoBehaviour
    {
        [SerializeField] LayerMask usageMask;
        [SerializeField] LayerMask positionMask;
        [SerializeField][Range(0, 2)] float radius = 0.7f;
        [SerializeField][Range(0, 10)] float placementDistance = 2;
        [SerializeField][Range(0, 10)] float separationDistance = 5;

        public event Action OnSwitch;
        Transform owner, player;
        float ownerRadius, playerRadius;
        PadSwitch moneyDrop;

        void Start()
        {
            if (TryGetComponent(out moneyDrop))
            {
                moneyDrop.OnFillComplete += () =>
                {
                    OnSwitch?.Invoke();
                    DestroySelf();
                };
            }
        }

        void DestroySelf()
        {
            Debug.Log("Destroy Switch!");
            Destroy(gameObject);
        }

        public PickupSwitch CreateInstance(Transform owner, float ownerRadius, Transform player, float playerRadius)
        {
            PickupSwitch inst = this.Instantiate();
            inst.owner = owner;
            inst.ownerRadius = ownerRadius;
            inst.player = player;
            inst.playerRadius = playerRadius;
            inst.SetPlacementPosition2();
            return inst;
        }

        void SetPlacementPosition2()
        {
            Vector3 turretPos = owner.position;
            Vector3 playerPos = player.transform.position;
            Vector3 camPos = Camera.main.transform.position;

            var positions = GetPositionsAround(turretPos, (turretPos - playerPos).GetAngle(), ownerRadius + playerRadius, radius)
                .Where(p => TestPoint(p))
                .ToList();

            bool TestPoint(Vector3 p)
            {
                if (Physics.CheckSphere(p, radius, positionMask, QueryTriggerInteraction.Ignore))
                    return false;

                if (Physics.Raycast(p + Vector3.up * 1, Vector3.down, out RaycastHit hit, 2, (int)GameLayerFlags.Terrain))
                {
                    return Mathf.Abs(hit.point.y - turretPos.y) < 0.5f;
                    // return false;
                }
                return true;
            }

            if (positions.Count == 0)
            {
                // Debug.LogError("No positions found");
                Destroy(gameObject);
                return;
            }

            float maxPlayerDist = (playerRadius + radius), maxPlayerDistSq = maxPlayerDist * maxPlayerDist;

            float DistCamera(Vector3 p) => (camPos - p).sqrMagnitude;
            float DistPlayer(Vector3 p) => (playerPos - p).sqrMagnitude;

            var nearPlayer = positions.Where(p => DistPlayer(p) <= maxPlayerDistSq).ToList();

            if (nearPlayer.Count > 0) Debug.Log("Near Player!");
            else Debug.Log("Near Camera!");

            transform.position = nearPlayer.Count > 0
                ? nearPlayer.OrderBy(DistCamera).First()
                : positions.OrderBy(DistPlayer).First();
        }

        IEnumerable<Vector3> GetPositionsAround(Vector3 p, float startAngle, float radius, float stepRadius)
        {
            for (float step = stepRadius / (2f * Mathf.PI * radius) * 360, s = step; s < 180; s += step / 4)
            {
                yield return p - Quaternion.Euler(0, startAngle + s, 0) * Vector3.forward * radius;
                yield return p - Quaternion.Euler(0, startAngle - s, 0) * Vector3.forward * radius;
            }
        }

        void SetPlacementPosition()
        {
            if (!owner || !player) return;

            Vector3 turretPos = owner.position;
            Vector3 playerPos = player.transform.position;

            // strict direction turret -> player (flattened)
            Vector3 toPlayer = playerPos - turretPos;
            toPlayer.y = 0;
            if (toPlayer.sqrMagnitude < 0.0001f) toPlayer = Vector3.forward;

            Vector3 dir = toPlayer.normalized;

            // radii
            float turretRadius = ownerRadius;

            float playerDist = toPlayer.magnitude;

            // place strictly between turret and player, respecting both radii
            float minDistFromTurret = turretRadius + placementDistance;
            float maxDistFromTurret = Mathf.Max(minDistFromTurret, playerDist - playerRadius - 0.25f);

            // choose midpoint-biased placement (more stable than safeDistance)
            float targetDist = Mathf.Min(minDistFromTurret + placementDistance, maxDistFromTurret);

            Vector3 position = turretPos + dir * targetDist;

            // extra safety: push away from player if still overlapping
            Vector3 toPlayerCheck = position - playerPos;
            toPlayerCheck.y = 0;
            float minPlayerDist = playerRadius * 1.1f;
            if (toPlayerCheck.sqrMagnitude < minPlayerDist * minPlayerDist)
            {
                Vector3 pushDir = toPlayerCheck.sqrMagnitude > 0.0001f ? toPlayerCheck.normalized : -dir;
                position = playerPos + pushDir * minPlayerDist;
            }

            bool CheckSphere(Vector3 position) =>
                Physics.CheckSphere(position, playerRadius, positionMask, QueryTriggerInteraction.Ignore);

            // avoid collisions with positionMask
            if (CheckSphere(position))
            {
                bool found = false;
                float step = 0.5f;
                Vector3 right = Vector3.Cross(Vector3.up, dir);

                for (int i = 0; i < 8 && !found; i++)
                {
                    float dist = targetDist + step * i;

                    // forward
                    Vector3 test = turretPos + dir * dist;
                    if (!CheckSphere(test)) { position = test; found = true; break; }

                    // sideways
                    Vector3[] offsets = { right, -right };
                    foreach (var o in offsets)
                    {
                        test = turretPos + (dir + o * 0.5f).normalized * dist;
                        if (!CheckSphere(test)) { position = test; found = true; break; }
                    }
                }

                if (!found)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            transform.position = position;
        }

        void Update()
        {
            if (!owner || !player) return;
            float dsq = separationDistance * separationDistance;
            if ((transform.position - owner.position).sqrMagnitude > dsq ||
                (transform.position - player.transform.position).sqrMagnitude > dsq)
                DestroySelf();
        }
    }

}
