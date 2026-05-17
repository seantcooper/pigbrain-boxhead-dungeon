using System.Linq;
using pigbrain.generated;
using Unity.Mathematics;
using UnityEngine;

namespace pigbrain.game.Boxhead.Environment
{
    public class CellObject : MonoBehaviour
    {
        public RoomData.Cell.Type cellType;
        public GeomType geomType;
        public int2 worldPositionKey;
        public bool tracking;

        public bool Is(RoomData.Cell.Type cellType, GeomType geomType) =>
            this.cellType.HasFlag(cellType) && this.geomType == geomType;

        public enum GeomType
        {
            Floor,
            Wall,
            Furniture,
            Door,
            Loot,
            Label,
            Corner,
        }

        public static int2 GetWorldPositionKey(Vector3 position) =>
            new(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z));

        public CellObject GetLabel() => GetComponentsInChildren<CellObject>(true).FirstOrDefault(c => c.geomType == GeomType.Label);

    }

    public static class CellObjectX
    {
        public static Layer GetUnityLayer(this CellObject.GeomType geomType) => geomType switch
        {
            CellObject.GeomType.Furniture => Layer.Furniture,
            CellObject.GeomType.Floor => Layer.Terrain,
            CellObject.GeomType.Wall => Layer.Wall,
            CellObject.GeomType.Door => Layer.Wall,
            CellObject.GeomType.Loot => Layer.Furniture,
            CellObject.GeomType.Corner => Layer.Wall,
            _ => throw new System.Exception($"Unknown GeomType {geomType}"),
        };
    }
}
