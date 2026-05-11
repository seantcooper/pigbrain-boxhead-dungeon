namespace pigbrain.game.Boxhead.Statistic
{
    public enum Stat
    {
        Nothing = -1,
        None = 0,
        Money = 1,
        ComboIndex = 2,
        Exp = 3,

        Weapon_Level = 24,
        Weapon_MinRange = 25,
        Weapon_MaxRange = 26,
        Weapon_FireRate = 27,
        Weapon_Ammo = 28,
        Weapon_Instances = 29,
        Weapon_Price = 30,
        Weapon_ShotSpeed = 31,
        Weapon_ShotDamage = 32,
        Weapon_ShotBounce = 33,

        AI_Level = 50,
        AI_Speed = 51,
        AI_TimeScale = 52,
        AI_Prize = 53,
        AI_Health = 54,
        AI_Turns = 55,
        AI_Threat = 56,
        AI_Grow = 57,
        AI_MinRange = 58,
        AI_MaxRange = 59,
        AI_Length = 60,
        AI_Exp = 61,
        AI_Attack = 62,

        Track_EnemyKills = 100,
        Track_MaxComboIndex = 101,
        Track_ComboIndex = 102,
        Track_Distance = 103,
        Track_TimeAlive = 104,
        Track_EnemyActive = 105,
        Track_MaxChainKills = 106,

        Track_Zombie_Appeared = 120,
        Track_Zombie_Killed,
        Track_Zombie_Total,
        Track_Runner_Appeared,
        Track_Runner_Killed,
        Track_Runner_Total,
        Track_Terror_Appeared,
        Track_Terror_Killed,
        Track_Terror_Total,
        Track_Ghost_Appeared,
        Track_Ghost_Killed,
        Track_Ghost_Total,
        Track_Devil_Appeared,
        Track_Devil_Killed,
        Track_Devil_Total,

        Track_Soldier_Appeared = 160,
        Track_Soldier_Killed,
        Track_Solider_Total,
        Track_Granny_Appeared,
        Track_Granny_Killed,
        Track_Granny_Total,
        Track_Bambo_Appeared,
        Track_Bambo_Killed,
        Track_Bambo_Total,

        Track_Tutorial_Complete = 180,
        Track_Tutorial_
    }
}

#region Editor
#if UNITY_EDITOR
namespace pigbrain.game.Boxhead.Statistic
{
    using pigbrain.core.Geom;
    using UnityEngine;
    using UnityEditor;
    using static pigbrain.core.Inspector.InspectorUtility;
    using pigbrain.core.Inspector;

    [CustomPropertyDrawer(typeof(Stat))]
    public class StatDrawer : EnumMenuDrawer<Stat> { }

    [CustomPropertyDrawer(typeof(Stats.Value), true)]
    public class Stats_Value_Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            EditorGUI.BeginProperty(pos, label, prop);
            bool duplicated = false;
            var parent = prop.GetParent();

            var stat = prop.FindPropertyRelative(nameof(Stats.Value.stat));
            var value = prop.FindPropertyRelative(nameof(Stats.Value.value));

            if (parent != null && parent.isArray)
            {
                foreach (var element in parent.GetArrayProperties())
                {
                    if (element.propertyPath == prop.propertyPath) break;
                    var stat2 = element.FindPropertyRelative(nameof(Stats.Value.stat));
                    if (stat.enumValueIndex == stat2.enumValueIndex)
                    {
                        duplicated = true;
                        break;
                    }
                }
            }
            var area = pos.WithH(LineHeight);
            new[] { stat, value }.DrawPropertyFields(area.DivideArea(Padding, 0, MiniFieldWidth));

            if (duplicated) EditorGUI.DrawRect(area, new(1, 0, 0, 0.33f));
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label) =>
            FullLineHeight;
    }
}
#endif
#endregion