using System;
using pigbrain.core.UnityObject;
using UnityEngine;

namespace pigbrain.game.Boxhead
{
    public interface IExplodeModel { GameObject GetPrefab(); }

    public class ExplodeModel : MonoBehaviour
    {
        [SerializeField] ExplodeModel prefab;

        readonly static Type[] ComponentTypes =
        {
            typeof(ExplodeModel),
            typeof(MeshFilter),
            typeof(MeshRenderer),
            typeof(Rigidbody),
            typeof(AltitudeDestroy),
        };

        public static GameObject CreateInstance(string name, Transform root, Vector3 origin,
            float force = 10f, float life = 1)
        {
            GameObject container;
            if (root.TryGetComponent(out IExplodeModel explodeModel))
            {
                var prefab = explodeModel.GetPrefab();
                if (!prefab) return null;
                container = prefab.Instantiate(root.position, root.rotation);
                foreach (Rigidbody rb in container.GetComponentsInChildren<Rigidbody>())
                    Impulse(rb, rb.transform.position - origin);
            }
            else
            {
                container = new GameObject($"{name} (ExplodeModel)");
                container.transform.parent = PoolGroup.GetGroupContainer("ExplodeModel");

                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
                {
                    var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                    if (!mesh.isReadable) continue;

                    var instance = new GameObject(renderer.name, ComponentTypes);
                    instance.transform.SetParent(container.transform);
                    instance.transform.SetPositionAndRotation(renderer.transform.position, renderer.transform.rotation);
                    instance.transform.localScale = renderer.transform.lossyScale;

                    instance.GetComponent<MeshRenderer>().sharedMaterial = renderer.sharedMaterial;
                    instance.GetComponent<MeshFilter>().sharedMesh = mesh;

                    var collider = instance.AddComponent<MeshCollider>();
                    collider.sharedMesh = mesh;
                    collider.convex = true;

                    Impulse(instance.GetComponent<Rigidbody>(), instance.transform.position - origin);
                }
            }

            Destroy(container, life + UnityEngine.Random.value * (life * 0.20f));
            return container;

            void Impulse(Rigidbody rb, Vector3 delta)
            {
                Vector3 impulse = (delta.normalized + Vector3.up).normalized * force;
                rb.AddForce(impulse, ForceMode.Impulse);
            }
        }
    }
}
