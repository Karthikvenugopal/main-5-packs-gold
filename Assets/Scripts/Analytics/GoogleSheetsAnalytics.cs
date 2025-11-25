using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace Analytics
{
    public static class GoogleSheetsAnalytics
    {
        private const string ConfigResourceName = "google_sheets_config"; // Resources/google_sheets_config.json
        private const string DefaultSheetId = "analytics_elemental_final";

        private static readonly HashSet<string> AllowedLevels = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "level1scene",
            "level2scene",
            "level3scene",
            "level4scene",
            "level5scene",
            "level1",
            "level2",
            "level3",
            "level4",
            "level5"
        };

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
                var configAsset = Resources.Load<TextAsset>(ConfigResourceName);
                if (configAsset != null && !string.IsNullOrEmpty(configAsset.text))
                {
                    var cfg = JsonUtility.FromJson<Config>(configAsset.text);
                    if (cfg != null)
                    {
                        _webAppUrl = cfg.webAppUrl;
                        _sheetId = cfg.sheetId;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Analytics] Failed to load config: {e.Message}");
            }

            // Fallback to the default sheet if one is not provided in config
            if (string.IsNullOrWhiteSpace(_sheetId))
            {
                _sheetId = DefaultSheetId;
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
            if (!EnsureWebAppUrlConfigured()) return;

            levelId = ResolveLevelId(levelId);
            if (!EnsureLevelAllowed(levelId, "level result")) return;

            var data = new Dictionary<string, string>
            {
                { "session_id", _sessionId },
                { "level_id", levelId },
                { "success", success ? "TRUE" : "FALSE" },
                { "time_spent_s", Mathf.RoundToInt(timeSpentSeconds).ToString() }
            };

            TryAddSheetId(data);
            // Single POST send; no coroutine wrapper required for this sender
            SendFormUrlEncoded(_webAppUrl, data);
        }

        public static void SendTokenCompletion(
            string levelId,
            float completionRate,
            int tokensCollected,
            int tokensAvailable,
            float timeSpentSeconds)
        {
            if (!EnsureWebAppUrlConfigured()) return;

            levelId = ResolveLevelId(levelId);
            if (!EnsureLevelAllowed(levelId, "token completion")) return;

            var data = new Dictionary<string, string>
            {
                { "event", "token_completion" },
                { "session_id", _sessionId },
                { "level_id", levelId },
                { "token_completion_rate", Mathf.Clamp01(completionRate).ToString("0.###", CultureInfo.InvariantCulture) },
                { "tokens_collected", Mathf.Max(0, tokensCollected).ToString() },
                { "tokens_available", Mathf.Max(0, tokensAvailable).ToString() },
                { "time_spent_s", Mathf.RoundToInt(timeSpentSeconds).ToString() }
            };

            TryAddSheetId(data);
            SendGetQuery(_webAppUrl, data);
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
            if (!EnsureWebAppUrlConfigured()) return;

            levelId = ResolveLevelId(levelId);
            if (!EnsureLevelAllowed(levelId, "hotspot")) return;

            if (cellSize <= 0f) cellSize = 1f;
            int gridX = Mathf.FloorToInt(worldPosition.x / cellSize);
            int gridY = Mathf.FloorToInt(worldPosition.y / cellSize);

            var data = new Dictionary<string, string>
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

            if (!string.IsNullOrEmpty(victimRole)) data["victim_role"] = victimRole;
            if (!string.IsNullOrEmpty(cause)) data["cause"] = cause;

            TryAddSheetId(data);
            SendGetQuery(_webAppUrl, data);
        }

        // Retry click analytics removed per request

        public static void SendHeartLoss(
            string levelId,
            string player,
            string cause,
            float timeSinceStartSeconds)
        {
            if (!EnsureWebAppUrlConfigured()) return;

            levelId = ResolveLevelId(levelId);
            if (!EnsureLevelAllowed(levelId, "heart loss")) return;

            var data = new Dictionary<string, string>
            {
                { "event", "heart_lost" },
                { "session_id", _sessionId },
                { "level_id", levelId },
                { "player", player ?? string.Empty },
                { "cause", cause ?? string.Empty },
                { "time_since_start_s", Mathf.RoundToInt(timeSinceStartSeconds).ToString() }
            };

            TryAddSheetId(data);
            SendGetQuery(_webAppUrl, data);
        }

        public static void SendAssist(
            string levelId,
            string actor,
            string recipient,
            string kind,
            float timeSinceStartSeconds)
        {
            if (!EnsureWebAppUrlConfigured()) return;

            levelId = ResolveLevelId(levelId);
            if (!EnsureLevelAllowed(levelId, "assist")) return;

            var data = new Dictionary<string, string>
            {
                { "event", "assist" },
                { "session_id", _sessionId },
                { "level_id", levelId },
                { "actor", actor ?? string.Empty },
                { "recipient", recipient ?? string.Empty },
                { "kind", kind ?? string.Empty },
                { "time_since_start_s", Mathf.RoundToInt(timeSinceStartSeconds).ToString() }
            };

            TryAddSheetId(data);
            SendGetQuery(_webAppUrl, data);
        }

        private static bool EnsureWebAppUrlConfigured()
        {
            if (!string.IsNullOrWhiteSpace(_webAppUrl)) return true;

            Debug.LogWarning("[Analytics] Cannot send analytics: Web App URL is empty.");
            return false;
        }

        private static string ResolveLevelId(string levelId)
        {
            return string.IsNullOrEmpty(levelId) ? SceneManager.GetActiveScene().name : levelId;
        }

        private static bool EnsureLevelAllowed(string levelId, string context)
        {
            if (AllowedLevels.Contains(levelId ?? string.Empty)) return true;

            Debug.Log($"[Analytics] Skipping {context} for scene '{levelId}'. Only Level1/Level2/Level3/Level4/Level5 allowed.");
            return false;
        }

        private static void TryAddSheetId(Dictionary<string, string> data)
        {
            if (!string.IsNullOrWhiteSpace(_sheetId))
            {
                data["sid"] = _sheetId;
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[Analytics] Using sheet id '{_sheetId}'");
                #endif
            }
        }

        private static void SendFormUrlEncoded(string url, Dictionary<string, string> data)
        {
            if (string.IsNullOrWhiteSpace(url)) return;

            try
            {
                var request = UnityWebRequest.Post(url, data);
                var operation = request.SendWebRequest();
                operation.completed += _ => HandleUnityWebRequestResult(request, isGet: false);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Analytics] Failed to start POST: {e.Message}");
            }
        }

        private static void SendGetQuery(string url, Dictionary<string, string> data)
        {
            if (string.IsNullOrWhiteSpace(url)) return;

            try
            {
                var sb = new StringBuilder();
                sb.Append(url);
                sb.Append(url.Contains("?") ? '&' : '?');

                bool first = true;
                foreach (var kv in data)
                {
                    if (!first) sb.Append('&');
                    first = false;
                    sb.Append(UnityWebRequest.EscapeURL(kv.Key));
                    sb.Append('=');
                    sb.Append(UnityWebRequest.EscapeURL(kv.Value));
                }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[Analytics] GET -> {sb}");
#endif
                var request = UnityWebRequest.Get(sb.ToString());
                var operation = request.SendWebRequest();
                operation.completed += _ => HandleUnityWebRequestResult(request, isGet: true);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Analytics] Failed to start GET request: {e.Message}");
            }
        }

        private static void HandleUnityWebRequestResult(UnityWebRequest request, bool isGet)
        {
            if (request == null) return;

            try
            {
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
                    Debug.LogWarning($"[Analytics] {(isGet ? "GET" : "POST")} failed: {errorSummary}");
                }
                else
                {
                    var body = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                    var contentType = request.GetResponseHeader("content-type") ?? string.Empty;
                    bool looksHtmlError = (!string.IsNullOrEmpty(body) && body.IndexOf("<html", StringComparison.OrdinalIgnoreCase) >= 0)
                        || contentType.IndexOf("text/html", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (looksHtmlError)
                    {
                        var prefix = body != null && body.Length > 140 ? body.Substring(0, 140) + "…" : body;
                        Debug.LogWarning($"[Analytics] {(isGet ? "GET" : "POST")} returned HTML (Apps Script error page). Check your script. Snippet: {prefix}");
                    }
                    else if (!string.IsNullOrEmpty(body))
                    {
                        Debug.Log($"[Analytics] {(isGet ? "GET" : "POST")} ok: {body}");
                    }
                }
            }
            finally
            {
                request.Dispose();
            }
        }
    }
}
