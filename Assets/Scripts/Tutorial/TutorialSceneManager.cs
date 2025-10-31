using UnityEngine;
using System.Collections.Generic; // <-- 1. ADD THIS LINE

// This is a small, tutorial-only script.
// Its purpose is to find the GameManager, signal OnLevelReady,
// AND activate the first set of tutorial triggers.
public class TutorialSceneManager : MonoBehaviour
{
    // --- 2. ADD THIS SECTION ---
    [Header("Tutorial Steps")]
    [Tooltip("Drag all tutorial text triggers that should appear at the very beginning")]
    [SerializeField] private List<TutorialTextTrigger> initialTriggers;
    // --- END OF SECTION ---

    private void Start()
    {
        // 1. Find the GameManager in the scene.
        GameManager gameManager = FindAnyObjectByType<GameManager>();

        // 2. If it exists, send the "OnLevelReady" signal.
        if (gameManager != null)
        {
            gameManager.OnLevelReady(); // This unlocks player movement
        }
        else
        {
            Debug.LogError("TutorialSceneManager could not find a GameManager to signal.");
        }

        // --- 3. ADD THIS SECTION ---
        // Now, activate all the initial instruction texts
        foreach (TutorialTextTrigger trigger in initialTriggers)
        {
            if (trigger != null)
            {
                trigger.Activate();
            }
        }
        // --- END OF SECTION ---

        // 4. This script's job is done, so it can destroy itself.
        Destroy(this);
    }
}