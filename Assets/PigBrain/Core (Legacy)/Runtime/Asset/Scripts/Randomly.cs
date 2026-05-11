// using PigBrain.LegacyCore.Geom;
// using PigBrain.LegacyCore.Utility;
// using UnityEngine;

// public class Randomly : MonoBehaviour
// {
//     public int seed = 10001;
//     public bool addInstanceID = true;

//     Rnd rnd;
//     void Awake()
//     {
//         seed += addInstanceID ? GetInstanceID() : 0;
//         rnd = new Rnd(seed);
//     }

//     public int Range(int min, int max) => rnd.NextInt(min, max);
//     public int Range(MinMaxInt mm) => Range(mm.min, mm.max);

//     public float Range(float min, float max) => rnd.NextFloat(min, max);
//     public float Range(MinMaxFloat mm) => Range(mm.min, mm.max);
// }
