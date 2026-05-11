using System;
using System.Linq;
using pigbrain.core.Collections;
using pigbrain.core.Inspector;
using pigbrain.game.Boxhead.Audio;
using pigbrain.game.Boxhead.UI;
using UnityEngine;
using static pigbrain.core.Geom.Rnd;

namespace pigbrain.game.Boxhead
{
    public abstract class Command : ScriptableObject
    {
        [Header("Command")]
        [SerializeField] protected ClipLink sound;
        [SerializeField][InlineScriptableObject] protected MessageTickerData message;
        [SerializeField][InlineScriptableObject] protected CommandFilter[] filters;

        public void Invoke(Transform target)
        {
            if (!OnInvoke(target)) return;
            sound.Play(target.position);
            message.Invoke(target);
        }
        protected abstract bool OnInvoke(Transform target);

        public virtual bool IsValid(Transform target) => true;

        public interface IPrefab { GameObject GetPrefab(); }
    }

    [Serializable]
    public class CommandContainer
    {
        [InlineScriptableObject] public Command[] commands;
        public bool IsValid(Transform target) =>
            commands.Where(c => c).Any(c => c.IsValid(target));
        public void Invoke(Transform target) =>
            commands.Where(c => c).ForEach(c => c.Invoke(target));
    }

    [Serializable]
    public class CommandWeightedContainer : DropBox<CommandWeighted> { }

    [Serializable]
    public class CommandWeighted : IWeightedObject
    {
        [DropBoxTarget] public Command command;
        public int weight = 1;
        object IWeightedObject.GetValue() => command;
        int IWeightedObject.GetWeight() => weight;
    }
}

#if UNITY_EDITOR
namespace pigbrain.game.Boxhead
{
    using UnityEditor;
    using UnityEngine;
    using static pigbrain.core.Inspector.InspectorUtility;

    [CustomPropertyDrawer(typeof(CommandWeighted), true)]
    public class CommandWeighted_Drawer : OneLiner_Drawer
    {
        public override string[] fields =>
            new[] { nameof(CommandWeighted.command), nameof(CommandWeighted.weight) };
        public override float[] sizes => new[] { 0, MiniFieldWidth };
    }

    [CustomPropertyDrawer(typeof(CommandContainer), true)]
    public class CommandContainerDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            EditorGUI.BeginProperty(pos, label, prop);
            var commands = prop.FindPropertyRelative("commands");
            EditorGUI.PropertyField(pos, commands, label, true);
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label) =>
            EditorGUI.GetPropertyHeight(prop.FindPropertyRelative("commands"), label, true);
    }
}
#endif
