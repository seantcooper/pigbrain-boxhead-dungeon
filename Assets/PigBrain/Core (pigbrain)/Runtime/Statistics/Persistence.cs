using System;
using System.Collections;
using System.Collections.Generic;
using pigbrain.core.Collections;
using pigbrain.core.Utility;
using UnityEngine;

namespace pigbrain.core.Statistics
{
    public static class Persistence
    {
        readonly static Values PValues = new();
        readonly static HashSet<SaveData> datas = new();

        #region Read / Write
        public static void Read()
        {
            Debug.Log("Persistence Read");
            RegisterInactive();
            PValues.Read();
            foreach (var data in datas)
                if (PValues.TryGetValue(data.property.type, data.key, out object value))
                    data.property.value = value;
            StartPolling();
        }

        public static void Write()
        {
            Debug.Log("Persistence Write");
            PValues.Write();
        }
        #endregion

        #region Types
        public static bool HasKey(string key) =>
            PValues.TryFind(key, out var _);
        public static string GetString(string key, string defaultValue = "") =>
            PValues.GetValue(key, defaultValue);
        public static void SetString(string key, string value) =>
            PValues.SetValue(key, value);
        public static int GetInt(string key, int defaultValue = default) =>
            PValues.GetValue(key, defaultValue);
        public static void SetInt(string key, int value) =>
            PValues.SetValue(key, value);
        public static float GetFloat(string key, float defaultValue = default) =>
            PValues.GetValue(key, defaultValue);
        public static void SetString(string key, float value) =>
            PValues.SetValue(key, value);
        public static bool GetBool(string key, bool defaultValue = default) =>
            PValues.GetValue(key, defaultValue);
        public static void SetBool(string key, bool value) =>
            PValues.SetValue(key, value);
        #endregion

        #region Polling
        static void StartPolling() => ApplicationMonitor.Instance.StartCoroutine(Polling());
        static void RegisterInactive() => Resources.FindObjectsOfTypeAll<SaveData>().ForEach(sd => sd.Register());

        public static void Register(SaveData data) => datas.Add(data);
        public static void Unregister(SaveData data) => datas.Remove(data);

        static IEnumerator Polling()
        {
            while (true)
            {
                bool isDirty = false;
                foreach (var data in datas)
                {
                    string key = data.key;
                    object value = data.property.value;
                    Values.Entry entry;
                    if (PValues.TryFind(key, out entry))
                    {
                        if (!entry.GetValue(data.property.type).Equals(value))
                            entry.SetValue(value);
                    }
                    else PValues.Add(entry = new Values.Entry(key, value));
                    if (entry.isDirty) isDirty = true;
                }
                if (isDirty) Write();
                yield return null;
            }
        }
        #endregion

        [Serializable]
        public class Values
        {
            const string SaveKey = "Persistence.Values";

            [SerializeField] List<Entry> values = new();

            #region Entry
            [Serializable]
            internal class Entry
            {
                public string key;
                [SerializeField] string value;
                public bool isDirty { get; internal set; }

                public Entry(string key, object value)
                {
                    this.key = key;
                    this.value = $"{value}";
                    isDirty = true;
                }
                public void SetValue(object value)
                {
                    this.value = value.ToString();
                    isDirty = true;
                }

                public T GetValue<T>() => (T)Parse(typeof(T), value);
                public object GetValue(Type type) => Parse(type, value);

                object Parse(Type type, string value) => type switch
                {
                    var t when t == typeof(string) => value,
                    var t when t == typeof(bool) => bool.Parse(value),
                    var t when t == typeof(int) => int.Parse(value),
                    var t when t == typeof(long) => long.Parse(value),
                    var t when t == typeof(float) => float.Parse(value),
                    _ => throw new Exception($"Type '{type}' on key '{key}' is unsupported!")
                };
            }
            #endregion

            #region Find
            internal bool TryFind(string key, out Entry entry)
            {
                entry = values.Find(v => v.key == key) is Entry e ? e : null;
                return entry != null;
            }
            #endregion

            #region Generic
            internal bool TryGetValue(Type type, string key, out object value)
            {
                bool found = TryFind(key, out var e);
                value = found ? e.GetValue(type) : null;
                return found;
            }

            internal T GetValue<T>(string key, T value = default) =>
                TryFind(key, out var e) ? e.GetValue<T>() : default;

            internal void SetValue<T>(string key, T value)
            {
                if (TryFind(key, out var e)) e.SetValue(value);
                else Add(new Entry(key, value));
            }
            internal void Add(Entry entry) => values.Add(entry);
            #endregion

            #region Read / Write
            public void Write()
            {
                PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(this));
                values.ForEach(e => e.isDirty = false);
            }
            public void Read() => JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(SaveKey, ""), this);
            #endregion
        }
    }
}
