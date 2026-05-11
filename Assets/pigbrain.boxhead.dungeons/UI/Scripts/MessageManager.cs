// using pigbrain.core.UnityObject;
// using UnityEngine;

// namespace pigbrain.game.Boxhead.UI
// {
//     public class MessageManager : MonoBehaviourSingleton<MessageManager>
//     {
//         public static TMP_Message CreateUIMessage(TMP_Message prefab, object message)
//         {
//             TMP_Message inst = null;
//             if (prefab)
//             {
//                 inst = prefab.CreateInstance(message);
//                 inst.transform.SetParent(Instance.transform);
//                 inst.transform.SetLocalPositionAndRotation(default, Quaternion.identity);
//             }
//             return inst;
//         }

//         void OnTransformChildrenChanged()
//         {
//             Debug.Log("OnTransformChildrenChanged!");
//         }
//     }
// }
