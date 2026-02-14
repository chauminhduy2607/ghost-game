using UnityEngine;

/// <summary>
/// Script riêng để phát âm thanh cho Ghost
/// Không ảnh hưởng đến GhostController
/// </summary>
public class GhostAudio : MonoBehaviour
{
    void OnMouseDown()
    {
        PlayTapSound();
    }
    
    void PlayTapSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGhostTap();
            Debug.Log("🔊 Ghost tap sound!");
        }
        else
        {
            Debug.LogError("❌ AudioManager not found!");
        }
    }
}