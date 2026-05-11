using UnityEngine;

public class WidenCulling : MonoBehaviour
{
    private Camera cam;
    private Matrix4x4 originalMatrix;
    private bool modified = false;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            originalMatrix = cam.cullingMatrix;
            SetInfiniteCulling();
        }
    }

    void OnDisable()
    {
        if (cam != null)
        {
            cam.cullingMatrix = originalMatrix;
        }
    }

    void SetInfiniteCulling()
    {
        // Create a giant orthographic projection covering a huge space
        Matrix4x4 infiniteOrtho = Matrix4x4.Ortho(-10000, 10000, -10000, 10000, -10000, 10000);
        cam.cullingMatrix = infiniteOrtho * cam.worldToCameraMatrix;
        modified = true;
    }

    void LateUpdate()
    {
        // In case the camera moves/rotates, update culling matrix
        if (cam != null && modified)
        {
            SetInfiniteCulling();
        }
    }
}
