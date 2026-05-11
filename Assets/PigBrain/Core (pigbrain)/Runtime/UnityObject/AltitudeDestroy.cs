using System.Collections;
using UnityEngine;

public class AltitudeDestroy : MonoBehaviour
{
    private static readonly WaitForSeconds WaitForSeconds0_5 = new WaitForSeconds(0.5f);
    [SerializeField] float y = -30;
    IEnumerator Start()
    {
        while (true)
        {
            if (transform.position.y < y) Destroy(gameObject);
            yield return WaitForSeconds0_5;
        }
    }
}
