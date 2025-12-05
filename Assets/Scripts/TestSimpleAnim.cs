using UnityEngine;

public class TestSimpleAnim : MonoBehaviour
{
    void Start()
    {
        var anim = GetComponent<Animation>();
        if (anim != null)
        {
            anim.Play("Aqua_Idle_Player");
        }
        else
        {
            Debug.LogError("No Animation component on Player");
        }
    }
}
