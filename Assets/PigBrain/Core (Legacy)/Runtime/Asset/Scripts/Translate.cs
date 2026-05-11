using UnityEngine;

public class Translate : MonoBehaviour
{
    [SerializeField] Vector3 translate;
    // Update is called once per frame
    void Update()
    {
        transform.position += translate * Time.deltaTime;
    }
}
