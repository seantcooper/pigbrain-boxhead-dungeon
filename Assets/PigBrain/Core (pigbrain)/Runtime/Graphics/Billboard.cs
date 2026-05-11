using pigbrain.core.Geom;
using UnityEngine;

namespace pigbrain.core.Graphics
{
    [ExecuteAlways]
    public class Billboard : MonoBehaviour
    {
        Transform camTransformCache;
        Transform camTransform => camTransformCache ? camTransformCache
            : camTransformCache = Camera.main.transform;
        void LateUpdate()
        {
            transform.rotation = camTransform.rotation;
        }
    }
}