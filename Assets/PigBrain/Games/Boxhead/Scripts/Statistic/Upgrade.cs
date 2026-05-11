using System;
using System.Collections.Generic;
using UnityEngine;

namespace pigbrain.game.Boxhead.Statistic
{
    [CreateAssetMenu(menuName = "PigBrain/Stat Update")]
    public class Upgrade : ScriptableObject
    {
        [SerializeField] internal bool consume;
        [SerializeField] internal List<UStat> ustats;

        public static Upgrade CreateInstance(string name, bool consume, params UStat[] ustats)
        {
            var instance = CreateInstance<Upgrade>();
            instance.name = name;
            instance.consume = consume;
            instance.ustats = new List<UStat>(ustats);
            // instance.hideFlags = HideFlags.HideAndDontSave;
            return instance;
        }

        [Serializable]
        public class UStat
        {
            public Stat stat;
            public OP op;
            public float value;

            public UStat(Stat stat, OP op, float value)
            {
                this.stat = stat;
                this.op = op;
                this.value = value;
            }

            public float Apply(float input) => Apply(input, value, op);
            public static float Apply(float input, float value, OP op) => op switch
            {
                OP.Add => input + value,
                OP.Mul => input * value,
                OP.Sub => input - value,
                OP.Div => input / value,
                OP.Set => value,
                _ => throw new NotImplementedException(),
            };
            public enum OP
            {
                [InspectorName("+")] Add = 0,
                [InspectorName("×")] Mul = 1,
                [InspectorName("−")] Sub = 2,
                [InspectorName("÷")] Div = 3,
                [InspectorName("=")] Set = 4,
            }
        }
    }
}

#if UNITY_EDITOR
namespace pigbrain.game.Boxhead.Statistic
{
    using System.Linq;
    using UnityEditor;
    using static pigbrain.core.Inspector.InspectorUtility;

    [CustomPropertyDrawer(typeof(Upgrade.UStat))]
    public class UStat_PropertyDrawer : PropertyDrawer
    {
        static readonly string[] Props =
        { nameof(Upgrade.UStat.stat), nameof(Upgrade.UStat.op), nameof(Upgrade.UStat.value) };

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var areas = position.DivideArea(Padding, 0, TinyFieldWidth, MiniFieldWidth);
            var properties = property.GetProperties(Props).ToArray();
            properties.DrawPropertyFields(areas);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            FullLineHeight;
    }
}
#endif