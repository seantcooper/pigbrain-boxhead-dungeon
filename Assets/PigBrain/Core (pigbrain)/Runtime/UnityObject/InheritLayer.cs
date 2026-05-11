using UnityEngine;

public class InheritLayer : MonoBehaviour
{
    [SerializeField] GameObject owner;

    public void Apply(Component owner) => Apply(owner.gameObject);
    public void Apply(GameObject owner)
    {
        this.owner = owner;
        gameObject.layer = owner.layer;
    }
}
