using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace pigbrain.core.Analytics
{
    public class SupaBaseQuery : Query
    {
        [SerializeField] string project = "lqaohusoznctdexowfxd";
        [SerializeField] string table = "Statistics";
        [SerializeField] string query = "event_type=eq.Session";
        [SerializeField] string key = "YOUR_ANON_KEY";

        [SerializeField][SerializeReference] List<AnalyticsPayload> results;

        string url => $"https://{project}.supabase.co/rest/v1/{table}?select={fields}&{query}";
        string fields => string.Join(",", typeof(StatRow).GetFields().Select(f => $"{f.Name}::text"));

        [System.Serializable]
        public class StatRow
        {
            public string session_id;
            public string event_type;
            public string created_at;
            public string data;

            public AnalyticsPayload Parse()
            {
                var dataType = AnalyticsManager.GetContext<AnalyticsContext>().payloadType
                    .GetNestedTypes(BindingFlags.Public)
                    .FirstOrDefault(t => t.Name == event_type);
                Debug.Log($"{dataType} '{data}'");
                var payload = (AnalyticsPayload)JsonUtility.FromJson(data, dataType);
                return payload;
            }
        }

        [System.Serializable]
        class Wrapper { public StatRow[] items; }

        public override void Fetch()
        {
#if UNITY_EDITOR
            Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutineOwnerless(Get());
#endif
        }

        IEnumerator Get()
        {
            Debug.Log("FETCH >>>>>>>>>>>>>");
            Debug.Log(url);
            var req = UnityWebRequest.Get(url);
            req.SetRequestHeader("apikey", key);
            req.SetRequestHeader("Authorization", "Bearer " + key);

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(req.error);
                yield break;
            }

            Debug.Log(req.downloadHandler.text);

            var json = "{\"items\":" + req.downloadHandler.text + "}";
            var rows = JsonUtility.FromJson<Wrapper>(json).items;

            results = new();
            foreach (var r in rows)
            {
                results.Add(r.Parse());
            }

            Debug.Log("/FETCH >>>>>>>>>>>>>");
        }
    }
}
