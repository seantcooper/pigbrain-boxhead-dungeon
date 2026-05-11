// using pigbrain.Core.Inspector;
// using pigbrain.Core.Utility;
// using TMPro;
// using UnityEngine;

// namespace pigbrain.Core.Statistics
// {
//     [RequireComponent(typeof(TextMeshProUGUI))]
//     public class StatsValueTMP : MonoBehaviour
//     {
//         [SerializeField][ReadOnly(ReadOnlyState.Runtime)] StatType type = StatType.Session;
//         [SerializeField][ReadOnly(ReadOnlyState.Runtime)] string propertyName = "Path/Field";
//         [SerializeField] string text;

//         TextMeshProUGUI tmp;
//         Value stat => GameStats.Get(type)[propertyName];

//         void OnValidate()
//         {
//             if (Application.isPlaying)
//                 OnValueChanged();
//         }

//         void Start()
//         {
//             tmp = GetComponent<TextMeshProUGUI>();
//             if (string.IsNullOrEmpty(text)) text = tmp.text;
//             stat.OnChange += (s) => OnValueChanged();
//             OnValueChanged();
//         }

//         void OnValueChanged()
//         {
//             if (!tmp) return;
//             // Debug.Log($"OnValueChanged {stat}");
//             tmp.text = text.Replace("{v}", $"{stat.Value}");
//         }
//     }
// }
