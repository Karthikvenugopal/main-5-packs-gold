using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Serialization;

public class GameManagerTutorial : MonoBehaviour
{
    public enum TutorialStep { Intro, TryForButter, HitIceWall, GetChili, MeltIce, FinalPopup, Step4Maze, Completed }
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
    [FormerlySerializedAs("peanutButterCleared")] private bool jamCleared = false; 
    private bool iceWallPopupTriggered = false;
    private int collectedIngredients = 0;
    private int totalIngredients = 0;
    
    void Start()
    {
        uiCanvas = FindFirstObjectByType<UICanvas>();
        if (uiCanvas == null) {
            Debug.LogError("UICanvas script not found in scene!");
            return;
        }

        uiCanvas.tutorialPopupPanel.GetComponent<CanvasGroup>().alpha = 0f;
        uiCanvas.tutorialPopupPanel.SetActive(false);
        if (endPanel != null) endPanel.SetActive(false);
        Time.timeScale = 1f;

        GoToStep(TutorialStep.Intro);
    }

    void GoToStep(TutorialStep nextStep)
    {
        currentStep = nextStep;
        switch (currentStep)
        {
            case TutorialStep.Intro:
                mazeBuilder.BuildTutorialLevel(1);
                totalIngredients = GameObject.FindGameObjectsWithTag("Ingredient").Length;
                collectedIngredients = 0;
                ShowPopup("Use W, A, S, D or Arrow keys to move.\nTry to collect the butter!", () => GoToStep(TutorialStep.TryForButter));
                break;
            case TutorialStep.TryForButter:
                break;
            case TutorialStep.HitIceWall:
                ShowPopup("Oops! You can't go through ice.\nMaybe chili can help?", () => GoToStep(TutorialStep.GetChili));
                break;
            case TutorialStep.GetChili:
                mazeBuilder.BuildTutorialLevel(3);
                totalIngredients = GameObject.FindGameObjectsWithTag("Ingredient").Length;
                collectedIngredients = 0;
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) player.transform.position = mazeBuilder.currentPlayerSpawnPoint;
                break;
            case TutorialStep.MeltIce:
                ShowPopup("Great! You've got the Chili power.\nNow try melting that ice wall!", () => {});
                break;
            case TutorialStep.FinalPopup:
                ShowPopup("Yay, You did it! Now try to beat the maze!", 
                () => GoToStep(TutorialStep.Step4Maze));
                break;
            case TutorialStep.Step4Maze:
                mazeBuilder.BuildTutorialLevel(4);
                totalIngredients = GameObject.FindGameObjectsWithTag("Ingredient").Length;
                collectedIngredients = 0;
                player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) player.transform.position = mazeBuilder.currentPlayerSpawnPoint;
                ShowPopup("Collect all ingredients and exit the maze!\nBeware of the Rolling Pins trying to crush you in your path!", () => { /* player starts */ });
                break;
            case TutorialStep.Completed:
                Time.timeScale = 1f;
                ShowPopup("Congratulations! You completed the tutorial!", 
                () => SceneManager.LoadScene("Level2Scene"));
                break;
        }
    }

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
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
        panelTransform.localScale = endScaleVec;

        if (showing)
        {
            Time.timeScale = 0f;
            uiCanvas.continueButton.onClick.RemoveAllListeners();
            uiCanvas.continueButton.onClick.AddListener(() => {
                StartCoroutine(AnimatePopup(false, "", onContinueCallback));
            });
        }
        else
        {
            panel.SetActive(false);
            Time.timeScale = 1f;
            onContinueCallback?.Invoke();
        }
    }
    
    void ShowPopup(string message, Action onContinueCallback)
    {
        StartCoroutine(AnimatePopup(true, message, onContinueCallback));
    }
    
    public void OnPlayerHitRightWall()
    {
        GoToStep(TutorialStep.FinalPopup);
    }


    // --- Public Event Handlers ---

    public void OnPlayerHitIceWall() { 
        if (currentStep == TutorialStep.TryForButter && !iceWallPopupTriggered) {
            iceWallPopupTriggered = true;
            StartCoroutine(DelayedIceWallPopup());
        }
    }
    
    private IEnumerator DelayedIceWallPopup()
    {
        yield return new WaitForSeconds(2f);
        GoToStep(TutorialStep.HitIceWall);
    }
    public void OnChiliCollected() { if (!chiliCollected) { chiliCollected = true; collectedIngredients++; if (currentStep == TutorialStep.GetChili) GoToStep(TutorialStep.MeltIce); CheckForTutorialCompletion(); } }
    public void OnIceWallMelted() { if (!iceMelted) { iceMelted = true; CheckForTutorialCompletion(); } }
    public void OnButterCollected() {
        if (!butterCollected) {
            butterCollected = true; 
            collectedIngredients++; 
            CheckForTutorialCompletion(); 
        } 
    }
    
    public void OnBreadCollected() { if (!breadCollected) { breadCollected = true; collectedIngredients++; CheckForTutorialCompletion(); } }
    public void OnStickyZonePassed() { if (!stickyPassed) { stickyPassed = true; Debug.Log("Player passed a sticky zone."); } }
    public void OnJamCleared() { if (!jamCleared) { jamCleared = true; Debug.Log("Player cleared a jam spill."); } }
    // --- END OF ADDED METHODS ---

    private void CheckForTutorialCompletion()
    {
        if (collectedIngredients >= totalIngredients)
        {
            if (currentStep == TutorialStep.MeltIce || currentStep == TutorialStep.FinalPopup)
            {
                GoToStep(TutorialStep.FinalPopup);
            }
            else if (currentStep == TutorialStep.Step4Maze)
            {
                GoToStep(TutorialStep.Completed);
            }
        }
    }
}
