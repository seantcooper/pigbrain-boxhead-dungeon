using UnityEngine;
using UnityEngine.UI;

namespace pigbrain.game.Boxhead.Creator
{
    public class LengthEditor : CreatorEditor
    {
        public enum Indices : byte { Short, Medium, Long, Endless }
        public int roomCount => value switch
        {
            Indices.Short => 2,
            Indices.Medium => 5,
            Indices.Long => 10,
            Indices.Endless => 25,
            _ => 2
        };

        public override byte[] Encode() => new byte[] { (byte)value };
        public override void Decode(byte[] bytes, ref int index) => value = (Indices)bytes[index++];

        public override string[] summary => new string[] { "Length:", $"{DungeonCreator.Bullet}{value} ({roomCount})" };

        void OnEnable() => SetValue(value);

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