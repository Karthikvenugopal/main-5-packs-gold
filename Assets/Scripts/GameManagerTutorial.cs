using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using UnityEngine.UI;
using System.Collections;

public class GameManagerTutorial : MonoBehaviour
{
    // MODIFY THIS ENUM
    public enum TutorialStep
    {
        Intro,              // Initial popup
        TryForButter,       // Moving in maze 1
        HitIceWall,         // Popup about ice
        GetChili,           // Maze 2 appears (with Chili)
        MeltIce,            // Popup after getting Chili
        CollectedFirstButter, // After collecting first butter -> build maze 5
        TryPeanutButter,    // Maze 5 appears (Player needs to hit Peanut Butter)
        HitPeanutButter,    // Popup about needing Bread
        SpawnBread,         // Maze 6 appears (with Bread), waiting for bread collection
        ClearPeanutButter,  // Popup after getting Bread, waiting for PB clear
        CollectFinalButter, // State after clearing PB, waiting for final Butter
        FinalPopup,         // Final completion popup
        Completed           // Load main scene
    }
    private TutorialStep currentStep;


    [Header("Dependencies")]
    public MazeBuilderTutorial mazeBuilder;
    private UICanvas uiCanvas;

    [Header("UI Panels")]
    public GameObject endPanel;

    [Header("Animation Settings")]
    public float animationDuration = 0.3f;
    public Vector3 startScale = new Vector3(0.7f, 0.7f, 1f);

    private bool chiliCollected = false;
    private bool iceMelted = false;
    private bool butterCollected = false;
    private bool breadCollected = false;
    private bool stickyPassed = false;
    private bool waterCleared = false;
    private int collectedIngredients = 0;
    private int totalIngredients = 0;
    private bool peanutButterCleared = false;

    void Start()
    {
        uiCanvas = FindFirstObjectByType<UICanvas>();
        if (uiCanvas == null) { Debug.LogError("UICanvas script not found in scene!"); return; }

        uiCanvas.tutorialPopupPanel.GetComponent<CanvasGroup>().alpha = 0f;
        uiCanvas.tutorialPopupPanel.SetActive(false);
        if (endPanel != null) endPanel.SetActive(false);
        Time.timeScale = 1f;

        GoToStep(TutorialStep.Intro);
    }

    // MODIFY THIS METHOD
    void GoToStep(TutorialStep nextStep)
    {
        Debug.Log("Transitioning to step: " + nextStep); // Add logging
        currentStep = nextStep;
        GameObject player = null; // Declare player here for reuse

        switch (currentStep)
        {
            case TutorialStep.Intro:
                mazeBuilder.BuildTutorialLevel(1);
                ShowPopup("Use W, A, S, D or Arrow keys to move.\nTry to collect the butter!", () => GoToStep(TutorialStep.TryForButter));
                break;
            case TutorialStep.TryForButter: break;
            case TutorialStep.HitIceWall:
                ShowPopup("Oops! You can't go through ice.\nMaybe chili can help?", () => GoToStep(TutorialStep.GetChili));
                break;
            case TutorialStep.GetChili:
                mazeBuilder.BuildTutorialLevel(3);
                totalIngredients = GameObject.FindGameObjectsWithTag("Ingredient").Length;
                collectedIngredients = 0;
                player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) player.transform.position = mazeBuilder.currentPlayerSpawnPoint;
                break;
            case TutorialStep.MeltIce:
                ShowPopup("Great! You've got the Chili power.\nNow try melting that ice wall!", () => {});
                break;

            case TutorialStep.CollectedFirstButter:
                // 1. Build maze with peanut butter (step 5)
                mazeBuilder.BuildTutorialLevel(5); // Ensure MazeBuilder has this layout
                player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) player.transform.position = mazeBuilder.currentPlayerSpawnPoint;
                GoToStep(TutorialStep.TryPeanutButter); // Immediately go to the next state
                break;

            case TutorialStep.TryPeanutButter:
                // Player is now in the maze with PB, waiting to hit it. No popup.
                break;

            case TutorialStep.HitPeanutButter:
                // 2. Show the "need bread" popup
                ShowPopup("You cannot go through the peanut butter, maybe bread can help",
                () => GoToStep(TutorialStep.SpawnBread)); // Go to next step after Continue
                break;

            case TutorialStep.SpawnBread:
                // 3. Rebuild maze with bread included (step 6)
                mazeBuilder.BuildTutorialLevel(6); // Ensure MazeBuilder has this layout
                player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) player.transform.position = mazeBuilder.currentPlayerSpawnPoint;
                // No popup here, just wait for the player to collect the bread.
                break;

            case TutorialStep.ClearPeanutButter:
                // 4. Show the "now clear it" popup AFTER bread is collected
                ShowPopup("Awesome! You got the bread power.\nNow try go through the peanut butter",
                () => { /* Let player proceed */ });
                // Stay in this state, waiting for the peanut butter to be cleared.
                break;

            case TutorialStep.CollectFinalButter:
                // 5. State after clearing peanut butter, waiting for the final butter. No popup.
                break;

            case TutorialStep.FinalPopup:
                ShowPopup("Yay, You did it! Now try to beat the maze!", () => GoToStep(TutorialStep.Completed));
                break;
            case TutorialStep.Completed:
                Time.timeScale = 1f;
                ShowPopup("Congratulations! You completed the tutorial!", 
                () => SceneManager.LoadScene("Level2Scene"));

                break;
        }
    }

    // --- Complete AnimatePopup Coroutine ---
    IEnumerator AnimatePopup(bool showing, string message, Action onContinueCallback)
    {
        GameObject panel = uiCanvas.tutorialPopupPanel;
        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        Transform panelTransform = panel.transform;

        float startAlpha = showing ? 0f : 1f;
        float endAlpha = showing ? 1f : 0f;
        Vector3 startScaleVec = showing ? startScale : Vector3.one;
        Vector3 endScaleVec = showing ? Vector3.one : startScale;

        if (showing) { uiCanvas.popupText.text = message; panel.SetActive(true); }

        float time = 0f;
        while (time < animationDuration)
        {
            time += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(time / animationDuration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
            panelTransform.localScale = Vector3.Lerp(startScaleVec, endScaleVec, progress);
            yield return null; // Wait for the next frame
        }

        // Ensure final values are set correctly
        canvasGroup.alpha = endAlpha;
        panelTransform.localScale = endScaleVec;

        if (showing)
        {
            // Pause game ONLY after fade-in animation completes
            Time.timeScale = 0f;
            uiCanvas.continueButton.onClick.RemoveAllListeners();
            uiCanvas.continueButton.onClick.AddListener(() => {
                // Start fade-out animation when Continue is clicked
                StartCoroutine(AnimatePopup(false, "", onContinueCallback));
            });
        }
        else
        {
            // Hide panel AFTER fade-out animation completes
            panel.SetActive(false);
            // Unpause game ONLY after fade-out completes
            Time.timeScale = 1f;
            // Execute the callback function (e.g., GoToStep)
            onContinueCallback?.Invoke();
        }


        yield break; // Explicitly end the coroutine path
    }


    void ShowPopup(string message, Action onContinueCallback)
    {
        StartCoroutine(AnimatePopup(true, message, onContinueCallback));
    }


    public void OnPlayerHitIceWall() { if (currentStep == TutorialStep.TryForButter) GoToStep(TutorialStep.HitIceWall); }
    // KEEP THIS METHOD AS IS
    public void OnPlayerHitPeanutButter()
    {
        // Check if we are in the correct tutorial step to react
        if (currentStep == TutorialStep.TryPeanutButter)
        {
            // Go to the step that shows the "Need Bread" popup
            GoToStep(TutorialStep.HitPeanutButter);
        }
    }
   
    public void OnChiliCollected() { if (!chiliCollected) { chiliCollected = true; collectedIngredients++; if (currentStep == TutorialStep.GetChili) GoToStep(TutorialStep.MeltIce); CheckForTutorialCompletion(); } }
    public void OnIceWallMelted() { if (!iceMelted) { iceMelted = true; CheckForTutorialCompletion(); } }
    public void OnButterCollected()
    {
        // Check context based on the CURRENT step
        if (currentStep == TutorialStep.MeltIce && !butterCollected) // Assuming 'butterCollected' tracks the first one
        {
            // This is the FIRST butter after melting ice
            butterCollected = true; // Mark first butter as collected
            collectedIngredients++; // Still need to count it for CheckForTutorialCompletion
            GoToStep(TutorialStep.CollectedFirstButter); // Trigger the peanut butter phase
        }
        else if (currentStep == TutorialStep.CollectFinalButter) // Check if we are waiting for the FINAL butter
        {
            // This is the FINAL butter collection
            collectedIngredients++; // Count it
            GoToStep(TutorialStep.FinalPopup); // Trigger the end sequence
        }
        else
        {
            // If butter collected in any other state, just count it (might happen if level design allows)
            collectedIngredients++;
            Debug.LogWarning($"Butter collected during unexpected step: {currentStep}");
            CheckForTutorialCompletion(); // Still check if this completes the level somehow
        }
    }
    public void OnBreadCollected()
    {
        if (!breadCollected)
        {
            breadCollected = true;
            collectedIngredients++; // Count bread as an ingredient if needed by totalIngredients logic
            // Only trigger the next step if we were in the state waiting for bread
            if (currentStep == TutorialStep.SpawnBread)
            {
                GoToStep(TutorialStep.ClearPeanutButter); // Show the "now clear it" popup
            }
            CheckForTutorialCompletion(); // Check if collecting bread itself completes the level
        }
    }
    public void OnStickyZonePassed() { if (!stickyPassed) { stickyPassed = true; Debug.Log("Player passed a sticky zone."); } }
    public void OnWaterPatchCleared() { if (!waterCleared) { waterCleared = true; Debug.Log("Player cleared a water patch."); } }
    public void OnPeanutButterCleared()
    {
        if (!peanutButterCleared)
        {
            peanutButterCleared = true;
            Debug.Log("Player cleared a peanut butter spill.");
            // Only transition if we were in the step right after getting the bread popup
            if (currentStep == TutorialStep.ClearPeanutButter)
            {
                GoToStep(TutorialStep.CollectFinalButter); // Advance to the state waiting for the final butter
            }
        }
    }
    

    private void CheckForTutorialCompletion()
    {
        // This function might not be strictly needed anymore if the flow is
        // driven entirely by specific events leading to FinalPopup.
        // However, we can keep it as a fallback or for later complex mazes.
        // Let's ensure it doesn't trigger too early.
        if (currentStep >= TutorialStep.CollectFinalButter && collectedIngredients >= totalIngredients)
        {
            // GoToStep(TutorialStep.FinalPopup); // The OnButterCollected now handles the final trigger
            Debug.Log("CheckForTutorialCompletion: Conditions met, but letting OnButterCollected handle final step.");
        }
    }
}