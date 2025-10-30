using UnityEngine;
using TMPro;




public class TutorialTrigger : MonoBehaviour
{
    private TextMeshPro textMesh;
    private int playersInZone = 0;

    void Awake()
    {
        textMesh = GetComponentInChildren<TextMeshPro>();
        if (textMesh != null)
        {
            textMesh.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("TutorialTrigger 无法在子对象中找到 TextMeshPro 组件！", this.gameObject);
        }
    }

    public void SetText(string text)
    {
        if (textMesh != null)
        {
            textMesh.text = text;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<CoopPlayerController>() != null) 
        {
            playersInZone++;
            if (textMesh != null)
            {
                textMesh.gameObject.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<CoopPlayerController>() != null)
        {
            playersInZone--;
            
            if (playersInZone <= 0 && textMesh != null)
            {
                textMesh.gameObject.SetActive(false);
                playersInZone = 0;
            }
        }
    }
}