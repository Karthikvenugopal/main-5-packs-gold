using UnityEngine;
using TMPro;

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

    

}