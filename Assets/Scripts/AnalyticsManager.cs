using System;
using System.IO;
using UnityEngine;

public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager I;
    public string fileName = "chef_analytics.csv";
    string _path;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        _path = Path.Combine(Application.persistentDataPath, fileName);
        if (!File.Exists(_path))
        {
            File.WriteAllText(_path, "session_id,level_id,success,time_spent_s,utc\n");
        }
    }

    public void LogRow(string levelId, bool success, float timeSpentS)
    {
        string row = $"{SystemInfo.deviceUniqueIdentifier},{levelId},{(success?1:0)},{timeSpentS:F2},{DateTime.UtcNow:o}\n";
        try { File.AppendAllText(_path, row); }
        catch (Exception e) { Debug.LogWarning($"Analytics write failed: {e.Message}"); }

        // visualize in WebGL
        AverageTime.Update(levelId, success, timeSpentS);
#if UNITY_EDITOR
        Debug.Log($"[Analytics] {row} → {_path}");
#endif
    }
}

// Running-average helper 
public static class AverageTime
{
    static string SumKey(string levelId) => $"avg_sum_{levelId}";
    static string CountKey(string levelId) => $"avg_cnt_{levelId}";

    public static void Update(string levelId, bool success, float timeS)
    {
        // only include successful completions in the average calculation
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
