using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UICanvas : MonoBehaviour
{

    [Header("UI Element References")]
    public TextMeshProUGUI timerText;
    public GameObject gameWonPanel;
    public UnityEngine.UI.Image[] stars;
    public GameObject gameOverPanel;
    public UnityEngine.UI.Button[] restartButtons;
    public TextMeshProUGUI loseReasonText;
    [Header("Top Instruction Text")]
    public TextMeshProUGUI instructionText;
    [Header("Tutorial Popup")]
    public GameObject tutorialPopupPanel;
    public TextMeshProUGUI popupText;
    public UnityEngine.UI.Button continueButton;
    [Header("Next Level Button")]
    public UnityEngine.UI.Button nextLevelButton;
    [Header("Main Menu Button")]
    public UnityEngine.UI.Button mainMenuButton;

    [Header("Level Info")]
    public UnityEngine.UI.Button infoButton;
    public UnityEngine.UI.Button infoCloseButton;
    public GameObject levelInfoPanel;

    public void ShowLevelInfo(bool show)
    {
        if (levelInfoPanel != null)
        {
            levelInfoPanel.SetActive(show);
        }
    }
}
