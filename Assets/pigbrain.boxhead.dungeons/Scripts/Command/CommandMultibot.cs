using pigbrain.core.Geom;
using pigbrain.core.UnityObject;
using pigbrain.game.Boxhead.Environment;
using UnityEngine;

namespace pigbrain.game.Boxhead
{
    [CreateAssetMenu(menuName = "PigBrain/Boxhead/Commands/Custom/Multibot")]
    public class CommandMultibot : Command
    {
        [Header("Multibot")]
        [SerializeField] uint seed = 0;
        [SerializeField] internal Player prefab;
        [SerializeField] internal int count = 5;

        void OnValidate()
        {
            if (seed == 0) seed = (uint)Random.Range(int.MinValue, int.MaxValue);
        }

        protected override bool OnInvoke(Transform target)
        {
            Room playersRoom = ActivePlayer.Instance.room;
            Rnd rnd = new(seed);
            for (int i = 0; i < count; i++)
                prefab.Instantiate(playersRoom.data.GetRandomPosition(rnd));
            return true;
        }
    }
}
