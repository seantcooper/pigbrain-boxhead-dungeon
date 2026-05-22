using UnityEngine;
using UnityEngine.UI;

namespace pigbrain.game.Boxhead.Creator
{
    public class SquadEditor : CreatorEditor
    {
        [SerializeField] Indices value;

        public enum Indices { Solo, Duo, Squad, Army }

        public int soldierCount => value switch
        {
            Indices.Solo => 0,
            Indices.Duo => 2,
            Indices.Squad => 6,
            Indices.Army => 15,
            _ => 0
        };

        void OnEnable() => SetValue(value);
        public override byte[] Encode() => new byte[] { (byte)value };
        public override void Decode(byte[] bytes, ref int index) => value = (Indices)bytes[index++];

        public override string[] summary => new string[] { "Squad:", $"{DungeonCreator.Bullet}{value} ({soldierCount})" };

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