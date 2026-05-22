using pigbrain.core.UnityObject;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace pigbrain.game.Boxhead.Creator
{
    public class Value : MonoBehaviour
    {
        [SerializeField] TMP_Text value;
        [SerializeField] Button neg;
        [SerializeField] Button pos;
        [SerializeField] string[] values;
    }
}