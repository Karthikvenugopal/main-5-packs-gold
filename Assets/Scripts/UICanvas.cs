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

}