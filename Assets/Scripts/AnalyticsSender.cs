using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class AnalyticsSender : MonoBehaviour
{
    [Header("Google Apps Script Web App URL (ends with /exec)")]
    [SerializeField] private string endpoint = "https://script.google.com/macros/s/AKfycbz8FKFQ0m-JgIKFuVkcmGRgkNvfuVcHkCsEfIYbwP76pfzQAVbZNg6YQzGt7dWV3xxG0Q/exec";  
    [SerializeField] private bool skipWebRequestsInWebGL = false;
    [SerializeField] private bool useFormEncodingOnWebGL = true;

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
        public string success;      
        public float  time_spent_s;
    }

    
    
    
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
            return;
        }
#endif

        var payload = new AnalyticsRow
        {
            timestamp    = DateTime.UtcNow.ToString("o"),
            session_id   = SessionId,
            level_id     = levelId,
            success      = wasSuccessful ? "TRUE" : "FALSE",  
            time_spent_s = Mathf.Max(0f, timeSpentS)
        };

        string targetUrl = endpoint.Trim();

#if UNITY_WEBGL && !UNITY_EDITOR
        if (skipWebRequestsInWebGL)
        {
            return;
        }

        if (useFormEncodingOnWebGL)
        {
            StartCoroutine(PostWebGLForm(targetUrl, payload));
        }
        else
        {
            StartCoroutine(PostJson(targetUrl, payload));
        }
#else
#if UNITY_EDITOR
        Debug.Log($"[WebAnalytics] (Editor only) would send analytics: {JsonUtility.ToJson(payload)}");
#endif
#endif
    }

    private IEnumerator PostJson(string url, AnalyticsRow payload)
    {
        string json = JsonUtility.ToJson(payload);
#if UNITY_EDITOR
        Debug.Log($"[WebAnalytics] POST → {url}\n{json}");
#endif
        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 10;
#if UNITY_WEBGL && !UNITY_EDITOR
            req.chunkedTransfer = false;
            req.useHttpContinue = false;
#endif

            yield return req.SendWebRequest();

            if (!IsRequestSuccessful(req))
                Debug.LogWarning($"[WebAnalytics] Failed: result={req.result} code={req.responseCode} err={req.error}");
            else
                Debug.Log($"[WebAnalytics] {req.responseCode} {req.downloadHandler.text}");
        }
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    private IEnumerator PostWebGLForm(string url, AnalyticsRow payload)
    {
        string body = BuildFormEncodedBody(payload);
        byte[] data = Encoding.UTF8.GetBytes(body);

        using (UnityWebRequest req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            req.uploadHandler = new UploadHandlerRaw(data);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
            req.timeout = 10;
            req.chunkedTransfer = false;
            req.useHttpContinue = false;

            yield return req.SendWebRequest();

            if (!IsRequestSuccessful(req))
                Debug.LogWarning($"[WebAnalytics] Failed: result={req.result} code={req.responseCode} err={req.error}");
            else
                Debug.Log($"[WebAnalytics] {req.responseCode} {req.downloadHandler.text}");
        }
    }
#endif

    private static bool IsRequestSuccessful(UnityWebRequest req)
    {
#if UNITY_2020_2_OR_NEWER
        return req.result == UnityWebRequest.Result.Success;
#else
        return !req.isNetworkError && !req.isHttpError;
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    private static string BuildFormEncodedBody(AnalyticsRow payload)
    {
        StringBuilder sb = new StringBuilder();
        AppendFormField(sb, "timestamp", payload.timestamp);
        AppendFormField(sb, "session_id", payload.session_id);
        AppendFormField(sb, "level_id", payload.level_id);
        AppendFormField(sb, "success", payload.success);
        AppendFormField(sb, "time_spent_s", payload.time_spent_s.ToString("F2"));
        return sb.ToString();
    }

    private static void AppendFormField(StringBuilder sb, string key, string value)
    {
        if (sb.Length > 0) sb.Append('&');
        sb.Append(key);
        sb.Append('=');
        sb.Append(UnityWebRequest.EscapeURL(value ?? string.Empty));
    }
#endif
}
