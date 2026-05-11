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

    }

    public static class CellObjectX
    {
        public static GameLayer GetUnityLayer(this CellObject.GeomType geomType) => geomType switch
        {
            CellObject.GeomType.Furniture => GameLayer.Furniture,
            CellObject.GeomType.Floor => GameLayer.Terrain,
            CellObject.GeomType.Wall => GameLayer.Wall,
            CellObject.GeomType.Door => GameLayer.Wall,
            CellObject.GeomType.Loot => GameLayer.Furniture,
            CellObject.GeomType.Corner => GameLayer.Wall,
            _ => throw new System.Exception($"Unknown GeomType {geomType}"),
        };
    }
}
