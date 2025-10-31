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

            // analytics code: avoid logging from MainMenu
            if (string.Equals(levelId, "MainMenu", StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log("[Analytics] Skipping analytics send for MainMenu scene.");
                return;
            }

            GoogleSheetsAnalytics.SendLevelResult(levelId, success, elapsed);
        }
    }
}
