using System;
using UnityEngine;
using UnityEngine.Networking;

namespace Analytics
{
    public static class GoogleSheetsAnalytics
    {
        private const string ConfigResourceName = "google_sheets_config"; // Resources/google_sheets_config.json
        private static string _webAppUrl;
        private static string _sessionId;
        private static bool _initialized;

        [Serializable]
        private class Config
        {
            public string webAppUrl;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            _sessionId = Guid.NewGuid().ToString("N");

            try
            {
                var ta = Resources.Load<TextAsset>(ConfigResourceName);
                if (ta != null && !string.IsNullOrEmpty(ta.text))
                {
                    var cfg = JsonUtility.FromJson<Config>(ta.text);
                    _webAppUrl = cfg != null ? cfg.webAppUrl : null;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Analytics] Failed to load config: {e.Message}");
            }

            if (string.IsNullOrWhiteSpace(_webAppUrl))
            {
                Debug.LogWarning("[Analytics] Web App URL not set. Set it in Resources/google_sheets_config.json");
            }
        }

        public static void SetWebAppUrl(string url)
        {
            _webAppUrl = url;
        }

        public static void SendLevelResult(string levelId, bool success, float timeSpentSeconds)
        {
            if (string.IsNullOrWhiteSpace(_webAppUrl))
            {
                Debug.LogWarning("[Analytics] Cannot send analytics: Web App URL is empty.");
                return;
            }

            if (string.IsNullOrEmpty(levelId))
            {
                levelId = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            }

            var data = new System.Collections.Generic.Dictionary<string, string>
            {
                { "session_id", _sessionId },
                { "level_id", levelId },
                { "success", success ? "TRUE" : "FALSE" },
                { "time_spent_s", Mathf.RoundToInt(timeSpentSeconds).ToString() }
            };

            SendFormUrlEncoded(_webAppUrl, data);
        }

        private static void SendFormUrlEncoded(string url, System.Collections.Generic.Dictionary<string, string> data)
        {
            var request = UnityWebRequest.Post(url, data);
            var asyncOperation = request.SendWebRequest();

            asyncOperation.completed += _ =>
            {
                try
                {
#if UNITY_2020_2_OR_NEWER
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogWarning($"[Analytics] Post failed: {(int)request.responseCode} {request.result} {request.error}");
                    }
                    else
#else
                    if (request.isNetworkError || request.isHttpError)
                    {
                        Debug.LogWarning($"[Analytics] Post failed: {(int)request.responseCode} {request.error}");
                    }
                    else
#endif
                    {
                        var body = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                        if (!string.IsNullOrEmpty(body))
                        {
                            Debug.Log($"[Analytics] Post ok: {body}");
                        }
                    }
                }
                finally
                {
                    request.Dispose();
                }
            };
        }
    }
}
