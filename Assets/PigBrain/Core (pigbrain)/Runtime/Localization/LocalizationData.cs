using System;
using System.Collections.Generic;
using pigbrain.core.Inspector;
using UnityEngine;

namespace pigbrain.core.Localization
{
    [CreateAssetMenu(menuName = "PigBrain/Boxhead/Localization/Data")]
    public class LocalizationData : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField] Language language = Language.English;
        [SerializeField, Row()] List<KeyValue> keyValues;
        [SerializeField] internal Dictionary<string, string> kvLookup;

        public static LocalizationData CreateInstance(Language language, string name)
        {
            var inst = CreateInstance<LocalizationData>();
            inst.name = name;
            inst.language = language;
            return inst;
        }

#if UNITY_EDITOR
        public void Clear()
        {
            keyValues = new();
        }

        public void AddOrModify(string key, string value)
        {
            keyValues ??= new();
            for (int i = 0; i < keyValues.Count; i++)
            {
                if (keyValues[i].key == key)
                {
                    if (string.IsNullOrEmpty(value)) keyValues.RemoveAt(i);
                    else keyValues[i].value = value;
                    return;
                }
            }
            if (!string.IsNullOrEmpty(value))
                keyValues.Add(new KeyValue(key, value));
        }
#endif

        [Serializable]
        internal class KeyValue
        {
            [Row.Width(100)] public string key;
            [Row.Width(0)] public string value;
            public KeyValue(string key, string value)
            {
                this.key = key;
                this.value = value;
            }
        }

        public string this[string key] => kvLookup.TryGetValue(key, out string value) ? value : "null";

        public enum Language
        {
            English,
            Spanish,
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() { }
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            kvLookup = new Dictionary<string, string>();
            foreach (var kv in keyValues)
            {
                if (string.IsNullOrEmpty(kv.key)) continue;
                kvLookup[kv.key] = kv.value;
            }
        }
    }
}

