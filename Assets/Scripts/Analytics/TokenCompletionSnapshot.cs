using UnityEngine;

namespace Analytics
{
    
    
    
    public readonly struct TokenCompletionSnapshot
    {
        public readonly int tokensCollected;
        public readonly int tokensAvailable;
        public readonly float completionRate;

        public TokenCompletionSnapshot(int collected, int available)
        {
            tokensCollected = Mathf.Max(0, collected);
            tokensAvailable = Mathf.Max(0, available);
            completionRate = tokensAvailable > 0
                ? Mathf.Clamp01((float)tokensCollected / tokensAvailable)
                : 0f;
        }
    }
}
