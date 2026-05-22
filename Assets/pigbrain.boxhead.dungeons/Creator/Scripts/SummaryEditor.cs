using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace pigbrain.game.Boxhead.Creator
{
    public class SummaryEditor : CreatorEditor
    {
        [SerializeField] internal TMP_Text detail;

        public override byte[] Encode() => new byte[0];
        public override void Decode(byte[] bytes, ref int index) { }
        public override string[] summary => new string[0];

    }
}