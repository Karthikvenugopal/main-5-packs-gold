using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Analytics
{
    public class LevelTimer : MonoBehaviour
    {
        [Tooltip("Optional override. Defaults to active scene name if empty.")]
        public string overrideLevelId;

        [Tooltip("If enabled, sends a failure event when the object is destroyed before success is marked.")]
        public bool autoSendFailureOnDestroy = true;

        private float _startTime;
        private bool _sent;
        private void OnEnable()
        {
            _startTime = Time.realtimeSinceStartup;
            _sent = false;
        }

        public void MarkSuccess()
        {
            SendInternal(true);
        }

        public void MarkFailure()
        {
            SendInternal(false);
        }

        private void OnDestroy()
        {
            if (!_sent && autoSendFailureOnDestroy)
            {
                SendInternal(false);
            }
        }

        private void OnApplicationQuit()
        {
            if (!_sent && autoSendFailureOnDestroy)
            {
                SendInternal(false);
            }
        }

        private void SendInternal(bool success)
        {
            if (_sent) return;
            _sent = true;

            var elapsed = Mathf.Max(0f, Time.realtimeSinceStartup - _startTime);
            var levelId = string.IsNullOrWhiteSpace(overrideLevelId)
                ? SceneManager.GetActiveScene().name
                : overrideLevelId.Trim();

            // analytics code: only log Level1/Level2/Level3/Level4/Level5
            var idLower = levelId.ToLowerInvariant();
            var isWhitelisted = idLower == "level1scene" || idLower == "level2scene" || idLower == "level3scene" || idLower == "level4scene" || idLower == "level5scene" ||
                                idLower == "level1" || idLower == "level2" || idLower == "level3" || idLower == "level4" || idLower == "level5";

            if (!isWhitelisted)
            {
                Debug.Log($"[Analytics] Skipping analytics send for scene '{levelId}'. Only Level1/Level2/Level3/Level4/Level5 allowed.");
                return;
            }

            bool hasTokenStats = TryCaptureTokenCompletion(levelId, out float completionRate, out int tokensCollected, out int tokensAvailable);

            GoogleSheetsAnalytics.SendLevelResult(levelId, success, elapsed);

            if (!hasTokenStats)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[Analytics] Token stats unavailable for level '{levelId}'. Sending zeroed completion entry.");
#endif
                completionRate = 0f;
                tokensCollected = 0;
                tokensAvailable = 0;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Analytics] Sending token_completion level={levelId} collected={tokensCollected} available={tokensAvailable} rate={completionRate:0.###} time={elapsed:0.0}s");
#endif
            GoogleSheetsAnalytics.SendTokenCompletion(levelId, completionRate, tokensCollected, tokensAvailable, elapsed);
        }

        public float ElapsedSeconds => Mathf.Max(0f, Time.realtimeSinceStartup - _startTime);

        public string ResolvedLevelId => string.IsNullOrWhiteSpace(overrideLevelId)
            ? SceneManager.GetActiveScene().name
            : overrideLevelId.Trim();

        private bool TryCaptureTokenCompletion(string levelId, out float completionRate, out int tokensCollected, out int tokensAvailable)
        {
            completionRate = 0f;
            tokensCollected = 0;
            tokensAvailable = 0;

            var tracker = TokenTracker.TryGetInstance();
            if (tracker != null && tracker.TryGetSnapshot(levelId, out var cachedSnapshot))
            {
                completionRate = cachedSnapshot.completionRate;
                tokensCollected = cachedSnapshot.tokensCollected;
                tokensAvailable = cachedSnapshot.tokensAvailable;
                return true;
            }

            var manager = FindAnyObjectByType<GameManager>();
            if (manager != null && manager.TryGetTokenCompletionSnapshot(out var snapshot))
            {
                completionRate = snapshot.completionRate;
                tokensCollected = snapshot.tokensCollected;
                tokensAvailable = snapshot.tokensAvailable;
                return true;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Analytics] Token stats unavailable for level '{levelId}'. tracker={(tracker != null ? "present" : "null")}");
#endif
            return false;
        }
    }
}
