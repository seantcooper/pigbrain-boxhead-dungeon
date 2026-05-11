using System.Linq;
using PigBrain.Generated;
using UnityEngine;

[CreateAssetMenu(menuName = "PigBrain/Core/Inventory")]
public class Inventory : ScriptableObject
{
    public Transform[] inventory;
    public Transform Get(GameTag tag) => inventory.FirstOrDefault(t => t.CompareTag($"{tag}"));
}
