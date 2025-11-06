using UnityEngine;
using System.Collections;

/// <summary>
/// Manages the sequence of tutorial instructions.
/// </summary>
public class TutorialSequenceManager : MonoBehaviour
{
    [Header("WASD Instruction Sequence")]
    [Tooltip("The WASD instruction that triggers the Fireboy sequence")]
    [SerializeField] private TutorialInstructionText instructionWASD;

    [Tooltip("First instruction text: 'Ember loses intensity in fire'")]
    [SerializeField] private TutorialInstructionText instructionFireLoss;

    [Tooltip("Second instruction text: 'Ember can melt ice walls'")]
    [SerializeField] private TutorialInstructionText instructionIceMelt;

    [Header("Arrow Key Instruction Sequence")]
    [Tooltip("The Arrow key instruction that triggers the Watergirl sequence")]
    [SerializeField] private TutorialInstructionText instructionArrow;

    [Tooltip("First instruction text: 'Aqua's powers freeze in ice'")]
    [SerializeField] private TutorialInstructionText instructionIceFreeze;

    [Tooltip("Second instruction text: 'Aqua can put out fires'")]
    [SerializeField] private TutorialInstructionText instructionFirePutOut;

    [Header("Work Together Instruction")]
    [Tooltip("Instruction text: 'Work together but never collide!'")]
    [SerializeField] private TutorialInstructionText instructionWorkTogether;

    [Tooltip("Delay before showing work together instruction after Watergirl crosses firewall (in seconds)")]
    [SerializeField] private float workTogetherDelay = 2f;

    private bool _wasdSequenceStarted = false;
    private bool _arrowSequenceStarted = false;
    private bool _workTogetherShown = false;
    private bool _instructionsHiddenOnExit = false;

    private void Start()
    {
        // Subscribe to WASD instruction hide event
        if (instructionWASD != null)
        {
            instructionWASD.OnInstructionHidden += OnWASDInstructionHidden;
        }

        // Subscribe to Arrow instruction hide event
        if (instructionArrow != null)
        {
            instructionArrow.OnInstructionHidden += OnArrowInstructionHidden;
        }

        // Subscribe to FireWall crossed event
        FireWall.OnWatergirlCrossed += OnWatergirlCrossedFirewall;

        // Subscribe to GameManagerTutorial exit events
        GameManagerTutorial.OnPlayerEnteredExitEvent += OnPlayerEnteredExit;
    }

    private void OnPlayerEnteredExit(CoopPlayerController player)
    {
        if (_instructionsHiddenOnExit) return;
        _instructionsHiddenOnExit = true;

        // Hide all instruction texts when one player reaches exit
        HideInstructionTexts();
    }

    private void HideInstructionTexts()
    {
        if (instructionFireLoss != null)
        {
            instructionFireLoss.Hide();
        }

        if (instructionIceMelt != null)
        {
            instructionIceMelt.Hide();
        }

        if (instructionIceFreeze != null)
        {
            instructionIceFreeze.Hide();
        }

        if (instructionFirePutOut != null)
        {
            instructionFirePutOut.Hide();
        }
    }

    private void OnWatergirlCrossedFirewall(FireWall fireWall)
    {
        if (_workTogetherShown) return;
        _workTogetherShown = true;

        // Show instruction after delay
        StartCoroutine(ShowWorkTogetherInstruction());
    }

    private IEnumerator ShowWorkTogetherInstruction()
    {
        yield return new WaitForSeconds(workTogetherDelay);

        if (instructionWorkTogether != null)
        {
            instructionWorkTogether.Show();
        }
    }

    private void OnWASDInstructionHidden()
    {
        if (_wasdSequenceStarted) return;
        _wasdSequenceStarted = true;

        ShowFireboyInstructions();
    }

    private void OnArrowInstructionHidden()
    {
        if (_arrowSequenceStarted) return;
        _arrowSequenceStarted = true;

        ShowWatergirlInstructions();
    }

    private void ShowFireboyInstructions()
    {
        // Stop Fireboy player glow
        StopPlayerGlow(PlayerRole.Fireboy);

        // Show Fireboy-specific instructions
        if (instructionFireLoss != null)
        {
            instructionFireLoss.Show();
        }

        if (instructionIceMelt != null)
        {
            instructionIceMelt.Show();
        }

        // Stop glow again after showing instructions (in case they started it)
        // Use a small delay to ensure Show() has completed
        StartCoroutine(StopFireboyGlowAfterDelay());
    }

    private IEnumerator StopFireboyGlowAfterDelay()
    {
        yield return null; // Wait one frame for Show() to complete
        StopPlayerGlow(PlayerRole.Fireboy);
    }

    private void ShowWatergirlInstructions()
    {
        // Stop Watergirl player glow
        StopPlayerGlow(PlayerRole.Watergirl);

        // Show Watergirl-specific instructions
        if (instructionIceFreeze != null)
        {
            instructionIceFreeze.Show();
        }

        if (instructionFirePutOut != null)
        {
            instructionFirePutOut.Show();
        }

        // Stop Fireboy glow after showing instructions (in case they incorrectly target Fireboy)
        // Use a small delay to ensure Show() has completed
        StartCoroutine(StopFireboyGlowAfterWatergirlInstructions());
    }

    private IEnumerator StopFireboyGlowAfterWatergirlInstructions()
    {
        yield return null; // Wait one frame for Show() to complete
        StopPlayerGlow(PlayerRole.Fireboy);
    }

    private void StopPlayerGlow(PlayerRole role)
    {
        // Find player with matching role
        CoopPlayerController[] players = FindObjectsByType<CoopPlayerController>(FindObjectsSortMode.None);
        foreach (CoopPlayerController player in players)
        {
            if (player.Role == role)
            {
                PlayerGlowEffect glow = player.GetComponent<PlayerGlowEffect>();
                if (glow != null)
                {
                    glow.StopGlow();
                }
                break;
            }
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (instructionWASD != null)
        {
            instructionWASD.OnInstructionHidden -= OnWASDInstructionHidden;
        }
        if (instructionArrow != null)
        {
            instructionArrow.OnInstructionHidden -= OnArrowInstructionHidden;
        }
        
        // Unsubscribe from FireWall event
        FireWall.OnWatergirlCrossed -= OnWatergirlCrossedFirewall;

        // Unsubscribe from GameManagerTutorial exit event
        GameManagerTutorial.OnPlayerEnteredExitEvent -= OnPlayerEnteredExit;
    }
}
