using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Collections;
using pigbrain.core.Geom;
using pigbrain.core.Inspector;
using UnityEngine;

namespace pigbrain.game.Boxhead.Audio
{
    public class AudioClips : ScriptableObject, IEnumerable<AudioClips.Clip>
    {
        public ClipContainer clips;

        [Serializable]
        public class ClipContainer : DropBox<Clip> { }

        [Serializable]
        public class Clip
        {
            [SerializeField][HideInInspector] internal string name;
            [SerializeField] public Trait traits;
            [DropBoxTarget] public AudioClip clip;
            [MinMaxRange(0.5f, 2f)] public MinMaxFloat volume = new(0.9f, 1.1f);
            [MinMaxRange(0.5f, 2f)] public MinMaxFloat pitch = new(0.9f, 1.1f);
            [Range(0, 1f)] public float delay = 0;
            [Range(0, 1f)] public float spatial = 1;
            public int count = 1;

            public bool loop => traits.HasFlag(Trait.Loop);
            public bool important => traits.HasFlag(Trait.Important);
            public float length => clip ? clip.length : 0;

            internal void OnValidate()
            {
                name = clip ? clip.name : "<null>";
                if (volume.min == volume.max && volume.max == 0) volume = new(0.9f, 1.1f);
                if (pitch.min == pitch.max && pitch.max == 0) pitch = new(0.9f, 1.1f);
                if (count == 0) count = 1;
            }
            public static implicit operator bool(Clip clip) => clip != null && clip.clip;

            [Flags]
            public enum Trait
            {
                None = 0,
                Loop = 1 << 0,
                Important = 1 << 1,
                Other = 1 << 2,
            }
        }

        IEnumerator<Clip> IEnumerable<Clip>.GetEnumerator() =>
            (clips?.items ?? Enumerable.Empty<Clip>()).Where(p => p.clip).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            ((IEnumerable<Clip>)this).GetEnumerator();

        void OnValidate() => clips.items.ForEach(clip => clip.OnValidate());

#if UNITY_EDITOR
        [ContextMenu("Reorder List")]
        void ReorderList()
        {
            clips.items.ForEach(c => c.OnValidate());
            clips.items = clips.items.OrderBy(c => c.name).ToArray();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}

#region Editor
#if UNITY_EDITOR
namespace pigbrain.game.Boxhead.Audio
{
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;
    using static pigbrain.core.Inspector.InspectorUtility;

    [CustomPropertyDrawer(typeof(AudioClips.Clip))]
    public class AudioClipsClipDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect rect, SerializedProperty prop, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, prop);
            Rect main = rect.WithH(LineHeight);

            if (GUI.Button(main.AddMinX(main.width - MiniFieldWidth), "play"))
            {
                var clip = prop.FindPropertyRelative(nameof(AudioClips.Clip.clip));
                var data = (AudioClips.Clip)prop.boxedValue;

                if (clip.objectReferenceValue is AudioClip c)
                    AudioManager.PreviewClip(c, data.volume.Rnd(), data.pitch.Rnd(), data.loop);
            }

            EditorGUI.PropertyField(main, prop, label);
            GUI.Label(main.AddMinX(main.width - MiniFieldWidth), "play", GUI.skin.button);
            if (prop.isExpanded)
            {
                prop.IterateToEndDraw(rect.AddY(FullLineHeight),
                    (r, p) => EditorGUI.PropertyField(r, p));
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label) =>
            prop.isExpanded ? prop.IterateToEndHeight() + FullLineHeight : FullLineHeight;

    }
}
#endif
#endregion