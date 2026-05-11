using pigbrain.core.Collections;
using pigbrain.core.Inspector;
using pigbrain.core.Utility;
using pigbrain.core.UnityObject;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;

namespace pigbrain.core.Map
{
    [InlineButton(nameof(Generate))]
    public class QuadGrid : MonoBehaviour
    {
        [SerializeField] protected GameObject tile;
        [SerializeField] protected int width = 5;
        [SerializeField] protected int height = 5;
        [SerializeField] protected int cellSize = 1;

        protected virtual void Generate()
        {
            (Vector3 position, Quaternion rotation, Vector3 scale) local =
                (Vector3.zero, Quaternion.identity, Vector3.one);

            if (transform.Find("generated") is Transform container)
            {
                local = (container.localPosition, container.localRotation, container.localScale);
                DestroyImmediate(container.gameObject);
            }

            (container = new GameObject("generated").transform).parent = transform;
            container.SetLocalPositionAndRotation(local.position, local.rotation);
            container.localScale = local.scale;

            MapUtility.Range2d(new(width, height)).ForEach(i => CreateTile(container, i));
        }

        protected virtual void CreateTile(Transform container, int2 i)
        {
            var instance = tile.Instantiate(container);
            instance.transform.localPosition = new(i.x, i.y);
        }
    }
}