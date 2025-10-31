using System;
using System.Collections;
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

            CoroutineHost.Run(SendFormUrlEncoded(_webAppUrl, data));
        }

        private static IEnumerator SendFormUrlEncoded(string url, System.Collections.Generic.Dictionary<string, string> data)
        {
            using (var req = UnityWebRequest.Post(url, data))
            {
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[Analytics] Post failed: {(int)req.responseCode} {req.result} {req.error}");
                }
                else
                {
                    var body = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;
                    if (!string.IsNullOrEmpty(body))
                        Debug.Log($"[Analytics] Post ok: {body}");
                }
            }
        }

        private class CoroutineHost : MonoBehaviour
        {
            private static CoroutineHost _instance;

            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
            private static void Ensure()
            {
                if (_instance != null) return;
                var go = new GameObject("__AnalyticsCoroutineHost");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _instance = go.AddComponent<CoroutineHost>();
            }

            public static void Run(IEnumerator routine)
            {
                if (_instance == null)
                {
                    Ensure();
                }
                _instance.StartCoroutine(routine);
            }
        }
    }
}
