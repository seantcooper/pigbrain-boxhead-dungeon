using System.Linq;
using pigbrain.core.Collections;
using pigbrain.core.Inspector;
using UnityEngine;

public class GizmoTransforms : MonoBehaviour
{
    public Camera worldCamera, uiCamera;
    public Transform[] transforms;

    void OnDrawGizmos()
    {
        if (transforms.IsNullOrEmpty()) return;

        Vector3 ConvertRT(RectTransform rt) => rt.position;
        Vector3[] positions = transforms.Select(t => t is RectTransform rt ? ConvertRT(rt) : t.position).ToArray();

        for (int i = 1; i < positions.Length; i++)
        {
            Vector3 t1 = positions[i - 1], t2 = positions[i];
            GizmosUtility.DrawLine(t1, t2, 0.25f);
        }
    }
}
