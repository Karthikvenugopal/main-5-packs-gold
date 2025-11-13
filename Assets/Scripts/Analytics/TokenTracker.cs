using System;
using UnityEngine;

namespace Analytics
{
    /// <summary>
    /// Persists token completion progress across scene transitions so analytics
    /// data can be retrieved even after the level GameManager is destroyed.
    /// </summary>
    public class TokenTracker : MonoBehaviour
    {
        private static TokenTracker _instance;

        public static TokenTracker Instance => _instance != null ? _instance : CreateSingleton();

        public static TokenTracker TryGetInstance() => _instance;

        private string _levelId;
        private int _tokensCollected;
        private int _tokensAvailable;
        private bool _hasData;

        private static TokenTracker CreateSingleton()
        {
            if (_instance != null) return _instance;

            var existing = FindAnyObjectByType<TokenTracker>();
            if (existing != null)
            {
                _instance = existing;
            }
            else
            {
                var go = new GameObject("TokenTracker");
                _instance = go.AddComponent<TokenTracker>();
            }

            DontDestroyOnLoad(_instance.gameObject);
            return _instance;
        }

        /// <summary>
        /// Resets tracked data for the specified level (called when a level is about to begin).
        /// </summary>
        public void ResetForLevel(string levelId, int tokensAvailable, int tokensCollected)
        {
            _levelId = levelId ?? string.Empty;
            _tokensAvailable = Mathf.Max(0, tokensAvailable);
            _tokensCollected = Mathf.Max(0, tokensCollected);
            _hasData = _tokensAvailable > 0 || _tokensCollected > 0;
        }

        /// <summary>
        /// Updates the running totals for the active level.
        /// </summary>
        public void UpdateTotals(string levelId, int tokensCollected, int tokensAvailable)
        {
            if (string.IsNullOrWhiteSpace(levelId)) return;

            if (!string.Equals(_levelId, levelId, StringComparison.OrdinalIgnoreCase))
            {
                ResetForLevel(levelId, tokensAvailable, tokensCollected);
                return;
            }

            _tokensCollected = Mathf.Max(0, tokensCollected);
            _tokensAvailable = Mathf.Max(0, tokensAvailable);

            if (_tokensAvailable <= 0 && _tokensCollected <= 0) return;
            _hasData = true;
        }

        public bool TryGetSnapshot(string levelId, out TokenCompletionSnapshot snapshot)
        {
            if (!_hasData || !string.Equals(_levelId, levelId, StringComparison.OrdinalIgnoreCase))
            {
                snapshot = default;
                return false;
            }

            snapshot = new TokenCompletionSnapshot(_tokensCollected, _tokensAvailable);
            return true;
        }
    }
}
