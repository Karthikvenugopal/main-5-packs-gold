using UnityEngine;
using System.Collections;




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

    private void Start()
    {
        
        if (instructionWASD != null)
        {
            instructionWASD.OnInstructionHidden += OnWASDInstructionHidden;
        }

        
        if (instructionArrow != null)
        {
            instructionArrow.OnInstructionHidden += OnArrowInstructionHidden;
        }

        
        FireWall.OnWatergirlCrossed += OnWatergirlCrossedFirewall;
    }

    private void OnWatergirlCrossedFirewall(FireWall fireWall)
    {
        if (_workTogetherShown) return;
        _workTogetherShown = true;

        
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
        
        StopPlayerGlow(PlayerRole.Fireboy);

        
        if (instructionFireLoss != null)
        {
            instructionFireLoss.Show();
        }

        if (instructionIceMelt != null)
        {
            instructionIceMelt.Show();
        }

        
        
        StartCoroutine(StopFireboyGlowAfterDelay());
    }

    private IEnumerator StopFireboyGlowAfterDelay()
    {
        yield return null; 
        StopPlayerGlow(PlayerRole.Fireboy);
    }

    private void ShowWatergirlInstructions()
    {
        
        StopPlayerGlow(PlayerRole.Watergirl);

        
        if (instructionIceFreeze != null)
        {
            instructionIceFreeze.Show();
        }

        if (instructionFirePutOut != null)
        {
            instructionFirePutOut.Show();
        }

        
        
        StartCoroutine(StopFireboyGlowAfterWatergirlInstructions());
    }

    private IEnumerator StopFireboyGlowAfterWatergirlInstructions()
    {
        yield return null; 
        StopPlayerGlow(PlayerRole.Fireboy);
    }

    private void StopPlayerGlow(PlayerRole role)
    {
        
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
        
        if (instructionWASD != null)
        {
            instructionWASD.OnInstructionHidden -= OnWASDInstructionHidden;
        }
        if (instructionArrow != null)
        {
            instructionArrow.OnInstructionHidden -= OnArrowInstructionHidden;
        }
        
        
        FireWall.OnWatergirlCrossed -= OnWatergirlCrossedFirewall;
    }
}
