using pigbrain.core.Geom;
using Unity.Mathematics;
using UnityEngine;

public class MapGrid
{
}

public static class MapCellExtensions
{
    public static int2 CellIndex(this Vector3 p, float unit = 1) =>
        new(Mathf.FloorToInt(p.x / unit), Mathf.FloorToInt(p.z / unit));

    public static Vector3 CellPosition(this Vector3 p, float unit = 1) =>
        new(Mathf.FloorToInt(p.x / unit) * unit, p.y, Mathf.FloorToInt(p.z / unit) * unit);

    public static Vector3 CellCenter(this Vector3 p, float unit = 1) =>
        CellPosition(p) + (Vector3.one * unit / 2f).WithY(0);
}