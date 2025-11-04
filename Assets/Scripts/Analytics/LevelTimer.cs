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

            // analytics code: only log Level1 and Level2
            var idLower = levelId.ToLowerInvariant();
            var isWhitelisted = idLower == "level1scene" || idLower == "level2scene" ||
                                idLower == "level1" || idLower == "level2";

            if (!isWhitelisted)
            {
                Debug.Log($"[Analytics] Skipping analytics send for scene '{levelId}'. Only Level1/Level2 allowed.");
                return;
            }

            GoogleSheetsAnalytics.SendLevelResult(levelId, success, elapsed);
        }
    }
}
