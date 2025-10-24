// Sends level results from Unity (Editor/WebGL) to a Google Sheets Apps Script Web App.
// Schema sent (JSON):
// { timestamp, session_id, level_id, success, time_spent_s, user_agent }

using System.Collections;
using UnityEngine;
using UnityEngine.Networking;   // UnityWebRequest

public class AnalyticsSender : MonoBehaviour
{
    [Header("Google Apps Script Web App URL (/exec)")]
    [Tooltip("Example URL: https://script.google.com/macros/s/AKfycb.../exec")]
    [SerializeField] private string endpoint = "";

    private static string SessionId
    {
        get
        {
            const string key = "chef_session_id";
            if (!PlayerPrefs.HasKey(key))
                PlayerPrefs.SetString(key, System.Guid.NewGuid().ToString("N"));
            return PlayerPrefs.GetString(key);
        }
    }

    [System.Serializable]
    private class AnalyticsData
    {
        public string timestamp;
        public string session_id;
        public string level_id;
        public bool success;
        public float time_spent_s;
        //public string user_agent;
    }
    
    // Call this from GameManager on win/lose.
    public void SendLevelResult(string levelId, bool success, float timeSpentS)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            Debug.LogWarning("[WebAnalytics] Endpoint is empty. Set it on AnalyticsSender.");
            return;
        }
        StartCoroutine(PostJSON(levelId, success, timeSpentS));
    }

    private IEnumerator PostJSON(string levelId, bool success, float timeSpentS)
    {
        var payload = new AnalyticsData
        {
            timestamp    = System.DateTime.UtcNow.ToString("o"),
            session_id   = SessionId,
            level_id     = levelId,
            success      = success,
            time_spent_s = timeSpentS,
            //user_agent   = SystemInfo.operatingSystem
        };

        string json = JsonUtility.ToJson(payload);
        string url  = endpoint.Trim();

#if UNITY_EDITOR
        Debug.Log($"[WebAnalytics] POST → {url}\n{json}");
#endif

        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            req.uploadHandler   = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool ok = req.result == UnityWebRequest.Result.Success;
#else
            bool ok = !req.isNetworkError && !req.isHttpError;
#endif

            if (!ok)
            {
                Debug.LogWarning($"[WebAnalytics] Failed: result={req.result} code={req.responseCode} err={req.error}");
            }
            else
            {
                Debug.Log($"[WebAnalytics] {req.responseCode} {req.downloadHandler.text}");
            }
        }
    }
}
