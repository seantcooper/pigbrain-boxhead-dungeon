using System;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace pigbrain.game.Boxhead.Creator
{
    public class EnemyEditor : CreatorEditor
    {
        public enum Indices : byte { Zombie, Runner, Devil, Terror, Ghost }
        [SerializeField] List<Indices> values = Enum.GetValues(typeof(Indices)).Cast<Indices>().ToList();
        protected override void OnToggleChanged(Toggle toggle, bool state)
        {
            Indices index = GetIndex<Indices>(GetBracketName(toggle.name));
            if (toggle.isOn && values.IndexOf(index) > -1) values.Remove(index);
            if (!toggle.isOn && values.IndexOf(index) == -1) values.Add(index);
            Refresh();
        }

        public override string[] summary => values.Select(v => $" + {v}").Prepend("Enemies:").ToArray();

        public override byte[] Encode() => new[]{
            values.Aggregate((byte)0, (a, v) => (byte)(a | (1 << (int)v)))};

        public override void Decode(byte[] bytes, ref int index)
        {
            values.Clear();
            var bits = bytes[index++];
            foreach (Indices value in Enum.GetValues(typeof(Indices)))
                if ((bits & (1 << (int)value)) != 0)
                    values.Add(value);
            SetValue(values.ToArray());
        }

        void OnEnable()
        {
            SetValue(values.ToArray());
        }

        public Indices[] GetValue() => values.ToArray();
        public void SetValue(params Indices[] index)
        {
            values = index.ToList();
            toggles.ForEach((t, i) => t.isOn = !values.Contains((Indices)i));
            Refresh();
        }
    }
}