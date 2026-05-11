using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace pigbrain.core.Analytics
{
    public class SupaBase : Post
    {
        [SerializeField] string projectid = "lqaohusoznctdexowfxd";
        [SerializeField] string tableid = "statistics";
        [SerializeField] string apiKey = "sb_publishable_s2BMJXT57xLhJeBRcOP--A_Nq4_G_L4";

        string url => $"https://{projectid}.supabase.co/rest/v1/{tableid}";
        protected override IEnumerator Run(AnalyticsPayload payload)
        {
            UnityWebRequest req = new(url, "POST")
            {
                uploadHandler = new UploadHandlerRaw(GetBody(payload)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("apikey", apiKey);
            req.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
                Debug.LogError(req.error);
            else
                Debug.Log($"Posted: {url} {ToJson(payload)}");
        }

        string ToJson(AnalyticsPayload payload)
        {
            var session_id = AnalyticsManager.GetSessionID();
            var event_type = payload.GetType().Name;
            var data = JsonUtility.ToJson(payload);
            return $"{{\"session_id\":\"{session_id}\",\"event_type\":\"{event_type}\",\"data\":{data}}}";
        }

        byte[] GetBody(AnalyticsPayload payload) => Encoding.UTF8.GetBytes("[" + ToJson(payload) + "]");

    }
}
