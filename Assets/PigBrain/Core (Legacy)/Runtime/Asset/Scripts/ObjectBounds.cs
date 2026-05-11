using UnityEngine;

public class ObjectBounds : MonoBehaviour
{
    public Vector3 center, size = Vector3.one;
    public Bounds bounds => new(center, size);
}
