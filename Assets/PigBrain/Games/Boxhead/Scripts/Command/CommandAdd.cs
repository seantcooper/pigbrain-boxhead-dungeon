using pigbrain.core.UnityObject;
using UnityEngine;

namespace pigbrain.game.Boxhead
{
    [CreateAssetMenu(menuName = "PigBrain/Boxhead/Commands/Add", order = -100000)]
    /// <summary>
    /// Adds a gameobject to the target:
    /// e.g. add a weapon to a character
    /// </summary>
    public class CommandAdd : Command, Command.IPrefab
    {
        [Header("Add")]
        [SerializeField] internal GameObject prefab;

        GameObject IPrefab.GetPrefab() => prefab;

        public override bool IsValid(Transform target)
        {
            if (!prefab) return false;
            foreach (Transform child in target)
                if (child.name == prefab.name)
                    return false;
            return base.IsValid(target);
        }

        protected override bool OnInvoke(Transform target)
        {
            prefab.Instantiate(target).transform.SetLocalPositionAndRotation(default, Quaternion.identity);
            return prefab;
        }
    }
}
