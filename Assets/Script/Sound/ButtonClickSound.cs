using UnityEngine;
using UnityEngine.UI;

public class ButtonClickSound : MonoBehaviour
{
    public AudioClip clickSound;
    
    void Start()
    {
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(PlaySound);
        }
    }
    
    void PlaySound()
    {
        if (clickSound != null)
        {
            AudioSource.PlayClipAtPoint(clickSound, Vector3.zero);
        }
    }
}