// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine;

// namespace PigBrain.LegacyCore.Game
// {
//     public class EffectEmissionFlash : MonoBehaviour
//     {
//         public Material material;
//         Material originalMaterial;
//         readonly List<ColorFlash> effects = new();
//         public void Add(Transform parent, Color color, float strength) =>
//             effects.Add(new ColorFlash() { strength = strength, color = color * 2 });

//         void Start()
//         {
//             if (!material)
//             {
//                 enabled = false;
//                 return;
//             }
//             originalMaterial = material;
//             ReplaceRendererMaterial(originalMaterial, material = new(originalMaterial));
//         }

//         void OnDestroy()
//         {
//             ReplaceRendererMaterial(material, originalMaterial);
//             Destroy(material);
//         }

//         void Update()
//         {
//             if (effects.Count == 0) return;
//             Color total = Color.black;
//             foreach (var effect in effects.ToArray())
//             {
//                 if (effect.Update()) effects.Remove(effect);
//                 else total += effect.color;
//             }
//             material.SetColor("_EmissionColor", total);
//             material.EnableKeyword("_EMISSION");
//         }

//         void ReplaceRendererMaterial(Material find, Material replace) =>
//             GetComponentsInChildren<Renderer>().ForEach(r => r.sharedMaterials =
//                 r.sharedMaterials.Select(m => m == find ? replace : m).ToArray());

//         public class ColorFlash
//         {
//             public Color color;
//             public float strength;
//             public bool Update()
//             {
//                 var last = color;
//                 color = Color.Lerp(color, Color.black, Time.deltaTime * 1 / strength);
//                 return color == Color.black && last == Color.black;
//             }
//         }
//     }
// }
