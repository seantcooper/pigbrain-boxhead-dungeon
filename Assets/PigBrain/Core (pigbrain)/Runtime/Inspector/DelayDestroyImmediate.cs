using UnityEngine;

[ExecuteAlways]
public class DelayDestroyImmediate : MonoBehaviour
{
    void Update() => DestroyImmediate(gameObject);
}
