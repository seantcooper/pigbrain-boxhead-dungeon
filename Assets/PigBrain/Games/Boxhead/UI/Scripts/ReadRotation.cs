using UnityEngine;

public class ReadRotation : MonoBehaviour
{
    void OnValidate() => Correct();
    void Start() => Correct();

    void Correct()
    {
        var cam = Camera.main; if (!cam) return;
        var t = transform;

        if (Vector3.Dot(t.up, cam.transform.up) < 0)
        {
            var e = t.eulerAngles;
            e.y += 180f;
            t.eulerAngles = e;
        }
    }
}
