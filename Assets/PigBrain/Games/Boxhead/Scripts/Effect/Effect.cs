using UnityEngine;
namespace pigbrain.game.Boxhead
{
    public class Effect : MonoBehaviour
    {
    }

    public interface IOwner
    {
        GameObject owner { get; set; }
    }

    public interface IEffectMask
    {
        LayerMask mask { get; set; }
    }

}