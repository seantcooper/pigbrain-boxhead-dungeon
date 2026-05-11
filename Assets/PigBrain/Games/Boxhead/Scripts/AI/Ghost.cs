using System.Collections;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Collections;
using pigbrain.core.Geom;
using pigbrain.core.UnityObject;
using pigbrain.game.Boxhead.Environment;
using Unity.Mathematics;
using UnityEngine;
using static pigbrain.game.Boxhead.Environment.RoomBoundary;
using static pigbrain.game.Boxhead.Environment.RoomData;

namespace pigbrain.game.Boxhead
{
    public class Ghost : MonoBehaviour, IEnemyType, ISpawnOverride
    {
        [SerializeField] Enemy.Type type;
        public Enemy.Type GetEnemyType() => type;

        [SerializeField] float speed = 1;
        [SerializeField] int trainLength = 10;
        [SerializeField] float spacing = 0.5f;
        [SerializeField][Range(1, 4)] int turns = 1;
        [SerializeField] GameObject clone;
        // [SerializeField][Range(0, 90)] float lookClamp = 35;

        // Produced from the 
        SplineBezier spline;

        #region Update
        IEnumerator Start()
        {
            if (spline == null) yield break;
            //Setup
            float scale = clone.transform.localScale.Min();
            float trainDistance = scale + spacing;
            float distance = 0;
            var train = new (float distance, Transform transform)[trainLength];
            for (int i = 0; i < trainLength; distance += trainDistance, i++)
                train[i] = (distance, i == 0 ? clone.transform
                    : clone.Instantiate(transform).transform);

            // Run
            float startTime = Time.time;
            float duration = spline.length / speed;

            while (enabled)
            {
                float t = (Time.time - startTime) / duration;
                float d = spline.length * t;

                Vector3 postion = ActivePlayer.Position;

                var last = train.Last();

                foreach (var part in train)
                {
                    if (!part.transform) continue;
                    float along = d - part.distance;
                    part.transform.gameObject.SetActive(along > 0 && along < spline.length);
                    spline.SetPositionAndRotation(along, part.transform);

                    if (last == part)
                        part.transform.Rotate(new(0, 180, 0));

                    if (along >= spline.length)
                    {
                        Destroy(part.transform.gameObject);
                        if (part == train[^1])
                            Destroy(gameObject);
                    }
                }
                yield return new WaitForNextUpdate();
            }
        }
        #endregion

        #region Spawn Override
        GameObject ISpawnOverride.Spawn(SpawnerBurst burst, GameObject prefab)
        {
            if (!burst.TryGetComponentInParent(out RoomData tr)) return null;

            GameObject inst = prefab.Instantiate();

            Ghost ghost = inst.GetComponent<Ghost>();

            static bool Filter(Marker m) => !m.cellType.HasFlag(Cell.Type.Enter);
            var paths = tr.boundary.WallToWallLoops(turns, 3, Filter);
            var path = burst.rnd.Next(paths);

            List<float2> npath = new() { (float2)path[0] + 0.5f };
            for (int i = 1, n = path.Length - 1; i < n; i++)
            {
                float2 f0 = path[i - 1], f1 = path[i + 0], f2 = path[i + 1];
                float2 d10 = math.sign(f1 - f0), d12 = math.sign(f1 - f2);
                npath.Add((f1 - d10) + 0.5f);
                npath.Add((f1 - d12) + 0.5f);
            }
            npath.Add((float2)path[^1] + 0.5f);

            Gizmos.color = Color.white.WithA(0.3f);
            ghost.spline = new SplineBezier(npath.Select(i => tr.GetWorldPosition(i)).ToArray());

            return inst;
        }
        #endregion
    }
}