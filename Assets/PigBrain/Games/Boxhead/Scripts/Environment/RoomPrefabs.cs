using pigbrain.core.Inspector;
using TMPro;
using UnityEngine;
using static pigbrain.core.Geom.Rnd;

namespace pigbrain.game.Boxhead.Environment
{
    public class RoomPrefabs : ScriptableObject, IWeightedObject
    {
        public int set = 0;
        public int weight = 1;

        object IWeightedObject.GetValue() => this;
        int IWeightedObject.GetWeight() => weight;

        [SerializeField] internal GameObject doorSpawner;
        [SerializeField] internal GameObject voidBlock;
        [SerializeField] internal GameObject doorLabelExit;
        [SerializeField] internal GameObject doorLabelLoot;
        [SerializeField] internal GameObject doorLabelEntrance;
        [SerializeField][InlineScriptableObject(true)] internal PrefabList floor;
        [SerializeField][InlineScriptableObject(true)] internal PrefabList floorFurniture;
        [SerializeField][InlineScriptableObject(true)] internal PrefabList wall;
        [SerializeField][InlineScriptableObject(true)] internal PrefabList wallInterior;
        [SerializeField][InlineScriptableObject(true)] internal PrefabList wallDecoration;
        [SerializeField][InlineScriptableObject(true)] internal PrefabList wallFurniture;
        [SerializeField][InlineScriptableObject(true)] internal PrefabList corner;
        [SerializeField][InlineScriptableObject(true)] internal PrefabList door;
        [SerializeField][InlineScriptableObject(true)] internal PrefabList spawn;
        [SerializeField][InlineScriptableObject(true)] internal PrefabList trap;
        [SerializeField][InlineScriptableObject(true)] internal PrefabList loot;
    }
}
