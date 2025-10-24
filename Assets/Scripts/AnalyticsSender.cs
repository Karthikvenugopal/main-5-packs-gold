using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class AnalyticsSender : MonoBehaviour
{
    [Header("Google Apps Script Web App URL (ends with /exec)")]
    [SerializeField] private string endpoint = "https://script.google.com/macros/s/AKfycbz8FKFQ0m-JgIKFuVkcmGRgkNvfuVcHkCsEfIYbwP76pfzQAVbZNg6YQzGt7dWV3xxG0Q/exec";  
    [SerializeField] private bool skipWebRequestsInWebGL = true;
    private bool skipLogged;

    public static AnalyticsSender I { get; private set; }

    private const string SessionKey = "chef_session_id";
    public string SessionId { get; private set; }

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        if (!PlayerPrefs.HasKey(SessionKey))
            PlayerPrefs.SetString(SessionKey, Guid.NewGuid().ToString("N"));

        SessionId = PlayerPrefs.GetString(SessionKey);
    }

    [Serializable]
    private class AnalyticsRow
    {
        public string timestamp;
        public string session_id;
        public string level_id;
        public string success;      // "TRUE" or "FALSE" (text, not bool)
        public float  time_spent_s;
    }

    /// <summary>
    /// Call this from GameManager on win/lose.
    /// </summary>
    public void SendLevelResult(string levelId, bool wasSuccessful, float timeSpentS)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            Debug.LogWarning("[AnalyticsSender] Web App URL (endpoint) not set.");
            return;
        }

#if !UNITY_EDITOR
        if (skipWebRequestsInWebGL && Application.platform == RuntimePlatform.WebGLPlayer)
        {
            if (!skipLogged)
            {
                Debug.Log("[WebAnalytics] Skipped analytics post on WebGL build.");
                skipLogged = true;
            }
            return;
        }
#endif

        var payload = new AnalyticsRow
        {
            timestamp    = DateTime.UtcNow.ToString("o"),
            session_id   = SessionId,
            level_id     = levelId,
            success      = wasSuccessful ? "TRUE" : "FALSE",  // send as text
            time_spent_s = Mathf.Max(0f, timeSpentS)
        };

        StartCoroutine(PostJson(endpoint.Trim(), JsonUtility.ToJson(payload)));
    }

    private IEnumerator PostJson(string url, string json)
    {
#if UNITY_EDITOR
        Debug.Log($"[WebAnalytics] POST → {url}\n{json}");
#endif
        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool ok = (req.result == UnityWebRequest.Result.Success);
#else
            bool ok = !req.isNetworkError && !req.isHttpError;
#endif
            if (!ok)
                Debug.LogWarning($"[WebAnalytics] Failed: result={req.result} code={req.responseCode} err={req.error}");
            else
                Debug.Log($"[WebAnalytics] {req.responseCode} {req.downloadHandler.text}");
        }
    }
}
