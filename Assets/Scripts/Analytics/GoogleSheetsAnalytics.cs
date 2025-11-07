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
                private static string _sheetId; // Optional spreadsheet id for standalone Apps Script
private static string _sessionId;
        private static bool _initialized;

        [Serializable]
        private class Config
        {
            public string webAppUrl;
        
            public string sheetId; // optional
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
                    if (cfg != null) { _webAppUrl = cfg.webAppUrl; _sheetId = cfg.sheetId; }
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

        
        public static void SetSheetId(string sheetId)
        {
            _sheetId = sheetId;
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

            // Client-side whitelist to avoid Tutorial or other scenes
            var idLower = levelId != null ? levelId.ToLowerInvariant() : string.Empty;
            bool allowed = idLower == "level1scene" || idLower == "level2scene" || idLower == "level3scene" || idLower == "level1" || idLower == "level2" || idLower == "level3";
            if (!allowed)
            {
                Debug.Log($"[Analytics] Skipping level result for scene '{levelId}'. Only Level1/Level2/Level3 allowed.");
                return;
            }

            var data = new System.Collections.Generic.Dictionary<string, string>
            {
                { "session_id", _sessionId },
                { "level_id", levelId },
                { "success", success ? "TRUE" : "FALSE" },
                { "time_spent_s", Mathf.RoundToInt(timeSpentSeconds).ToString() }
            };

            
            if (!string.IsNullOrWhiteSpace(_sheetId)) data["sid"] = _sheetId;
CoroutineHost.Run(SendFormUrlEncoded(_webAppUrl, data));
            SendFormUrlEncoded(_webAppUrl, data);
        }

        public static void SendFailureHotspot(
            string levelId,
            Vector3 worldPosition,
            float timeSpentSeconds,
            int heartsRemaining,
            int fireTokensCollected,
            int waterTokensCollected,
            float cellSize = 1f,
            string victimRole = null,
            string cause = null)
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

            // Client-side whitelist to ensure Tutorial is never logged
            var idLower = levelId != null ? levelId.ToLowerInvariant() : string.Empty;
            bool allowed = idLower == "level1scene" || idLower == "level2scene" || idLower == "level3scene" || idLower == "level1" || idLower == "level2" || idLower == "level3";
            if (!allowed)
            {
                Debug.Log($"[Analytics] Skipping hotspot for scene '{levelId}'. Only Level1/Level2/Level3 allowed.");
                return;
            }

            if (cellSize <= 0f) cellSize = 1f;
            int gridX = Mathf.FloorToInt(worldPosition.x / cellSize);
            int gridY = Mathf.FloorToInt(worldPosition.y / cellSize);

            var data = new System.Collections.Generic.Dictionary<string, string>
            {
                { "event", "fail" },
                { "session_id", _sessionId },
                { "level_id", levelId },
                { "grid_x", gridX.ToString() },
                { "grid_y", gridY.ToString() },
                { "time_spent_s", Mathf.RoundToInt(timeSpentSeconds).ToString() },
                { "hearts_remaining", Mathf.Max(0, heartsRemaining).ToString() },
                { "fire_tokens", Mathf.Max(0, fireTokensCollected).ToString() },
                { "water_tokens", Mathf.Max(0, waterTokensCollected).ToString() }
            };

            
            if (!string.IsNullOrWhiteSpace(_sheetId)) data["sid"] = _sheetId;
if (!string.IsNullOrEmpty(victimRole)) data["victim_role"] = victimRole;
            if (!string.IsNullOrEmpty(cause)) data["cause"] = cause;

            // Use GET for robustness across Editor/WebGL/Standalone
            CoroutineHost.Run(SendGetQuery(_webAppUrl, data));
        }

        public static void SendHeartLoss(
            string levelId,
            string player,
            string cause,
            float timeSinceStartSeconds)
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

            var idLower = levelId != null ? levelId.ToLowerInvariant() : string.Empty;
            bool allowed = idLower == "level1scene" || idLower == "level2scene" || idLower == "level3scene" || idLower == "level1" || idLower == "level2" || idLower == "level3";
            if (!allowed)
            {
                Debug.Log($"[Analytics] Skipping heart loss for scene '{levelId}'. Only Level1/Level2/Level3 allowed.");
                return;
            }

            var data = new System.Collections.Generic.Dictionary<string, string>
            {
                { "event", "heart_lost" },
                { "session_id", _sessionId },
                { "level_id", levelId },
                { "player", player ?? string.Empty },
                { "cause", cause ?? string.Empty },
                { "time_since_start_s", Mathf.RoundToInt(timeSinceStartSeconds).ToString() }
            };

            
            if (!string.IsNullOrWhiteSpace(_sheetId)) data["sid"] = _sheetId;
CoroutineHost.Run(SendGetQuery(_webAppUrl, data));
        }

                public static void SendAssist(
            string levelId,
            string actor,
            string recipient,
            string kind,
            float timeSinceStartSeconds)
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

            var idLower = levelId != null ? levelId.ToLowerInvariant() : string.Empty;
            bool allowed = idLower == "level1scene" || idLower == "level2scene" || idLower == "level3scene" || idLower == "level1" || idLower == "level2" || idLower == "level3";
            if (!allowed)
            {
                Debug.Log($"[Analytics] Skipping assist for scene '{levelId}'. Only Level1/Level2/Level3 allowed.");
                return;
            }

            var data = new System.Collections.Generic.Dictionary<string, string>
            {
                { "event", "assist" },
                { "session_id", _sessionId },
                { "level_id", levelId },
                { "actor", actor ?? string.Empty },
                { "recipient", recipient ?? string.Empty },
                { "kind", kind ?? string.Empty },
                { "time_since_start_s", Mathf.RoundToInt(timeSinceStartSeconds).ToString() }
            };

            if (!string.IsNullOrWhiteSpace(_sheetId)) data["sid"] = _sheetId;

            CoroutineHost.Run(SendGetQuery(_webAppUrl, data));
        }private static IEnumerator SendFormUrlEncoded(string url, System.Collections.Generic.Dictionary<string, string> data)
        {
            using (var request = UnityWebRequest.Post(url, data))
            {
                yield return request.SendWebRequest();

                bool failed;
                string errorSummary;
#if UNITY_2020_2_OR_NEWER
                failed = request.result != UnityWebRequest.Result.Success;
                errorSummary = $"{(int)request.responseCode} {request.result} {request.error}";
#else
                failed = request.isNetworkError || request.isHttpError;
                errorSummary = $"{(int)request.responseCode} {request.error}";
#endif

                if (failed)
                {
                    Debug.LogWarning($"[Analytics] Post failed: {errorSummary}");
                }
                else
                {
                    var body = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                    if (!string.IsNullOrEmpty(body))
                    {
                        Debug.Log($"[Analytics] Post ok: {body}");
                    }
                }
            }
        }

        private static IEnumerator SendGetQuery(string url, System.Collections.Generic.Dictionary<string, string> data)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(url);
            if (!url.Contains("?")) sb.Append('?'); else sb.Append('&');
            bool first = true;
            foreach (var kv in data)
            {
                if (!first) sb.Append('&');
                first = false;
                sb.Append(UnityWebRequest.EscapeURL(kv.Key));
                sb.Append('=');
                sb.Append(UnityWebRequest.EscapeURL(kv.Value));
            }

            using (var req = UnityWebRequest.Get(sb.ToString()))
            {
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[Analytics] GET failed: {(int)req.responseCode} {req.result} {req.error}");
                }
                else
                {
                    var body = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;
                    if (!string.IsNullOrEmpty(body))
                        Debug.Log($"[Analytics] GET ok: {body}");
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
