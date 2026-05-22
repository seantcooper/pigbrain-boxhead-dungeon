using UnityEngine;
using UnityEngine.UI;

namespace pigbrain.game.Boxhead.Creator
{
    public class ThreatEditor : CreatorEditor
    {
        public enum Indices { Recruit, Soldier, Veteren, Nightmare, Insane }
        void OnEnable() => SetValue(value);
        public override byte[] Encode() => new byte[] { (byte)value };
        public override void Decode(byte[] bytes, ref int index) => value = (Indices)bytes[index++];

        public override string[] summary => new string[] { "Threat:", $"{DungeonCreator.Bullet}{value}" };

        [SerializeField] Indices value;
        protected override void OnToggleChanged(Toggle toggle, bool state)
        { if (toggle.isOn) SetValue(GetIndex<Indices>(GetBracketName(toggle.name))); }

        public Indices GetValue() => value;
        public void SetValue(Indices index)
        {
            value = index;
            toggles[(int)value].isOn = true;
            Refresh();
        }

    }
}