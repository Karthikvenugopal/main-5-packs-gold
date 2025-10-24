using UnityEngine;

public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager I;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LogRow(string levelId, bool success, float timeSpentS)
    {
        AverageTime.Update(levelId, success, timeSpentS);
#if UNITY_EDITOR
        Debug.Log($"[Analytics] levelId={levelId}, success={success}, time={timeSpentS:F2}s (Editor log only)");
#endif
    }
}


public static class AverageTime
{
    static string SumKey(string levelId) => $"avg_sum_{levelId}";
    static string CountKey(string levelId) => $"avg_cnt_{levelId}";

    public static void Update(string levelId, bool success, float timeS)
    {
        
        if (!success) return; 
        float sum = PlayerPrefs.GetFloat(SumKey(levelId), 0f);
        int cnt = PlayerPrefs.GetInt(CountKey(levelId), 0);
        PlayerPrefs.SetFloat(SumKey(levelId), sum + timeS);
        PlayerPrefs.SetInt(CountKey(levelId), cnt + 1);
        PlayerPrefs.Save();
    }

    public static bool TryGetAverage(string levelId, out float avg)
    {
        int cnt = PlayerPrefs.GetInt(CountKey(levelId), 0);
        if (cnt <= 0) { avg = 0f; return false; }
        float sum = PlayerPrefs.GetFloat(SumKey(levelId), 0f);
        avg = sum / cnt;
        return true;
    }
}
