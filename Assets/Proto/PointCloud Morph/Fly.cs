using System.Linq;
using pigbrain.core.Geom;
using pigbrain.core.UnityObject;
using UnityEngine;

public class Fly : MonoBehaviour
{
    [SerializeField] PointCloud cloud;
    [SerializeField] GameObject prefab;
    [SerializeField] float speed = 5;
    [SerializeField] float turnSpeed = 180;
    [SerializeField] Vector2 randomSpeed = new(0.8f, 1.2f);

    struct Instance
    {
        public Transform transform;
        public float speed;
    }

    Instance[] instances;

    void Start()
    {
        instances = cloud.GetPoints()
            .Select(p => new Instance
            {
                transform = prefab.Instantiate(
                    p.position,
                    p.normal.GetRotation()).transform,
                speed = speed * Random.Range(randomSpeed.x, randomSpeed.y)
            })
            .ToArray();
    }

    void Update()
    {
        foreach (var instance in instances)
        {
            var dir = transform.position - instance.transform.position;
            if (dir.sqrMagnitude < 0.0001f) continue;

            var target = Quaternion.LookRotation(dir.normalized);
            instance.transform.rotation = Quaternion.RotateTowards(
                instance.transform.rotation,
                target,
                turnSpeed * Time.deltaTime);

            var move = instance.speed * Time.deltaTime;
            if (dir.magnitude <= move)
                instance.transform.position = transform.position;
            else
                instance.transform.position += instance.transform.forward * move;
        }
    }
}
