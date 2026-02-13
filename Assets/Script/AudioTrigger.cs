using UnityEngine;

/// <summary>
/// Script cực đơn giản để phát âm thanh
/// Có thể gọi từ bất kỳ đâu
/// </summary>
public class AudioTrigger : MonoBehaviour
{
    /// <summary>
    /// Gọi method này để phát SFX Ghost Tap
    /// </summary>
    public void PlayGhostTapSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGhostTap();
            Debug.Log("🔊 Ghost tap sound!");
        }
    }
    
    /// <summary>
    /// Gọi method này để phát SFX Obstacle Hit
    /// </summary>
    public void PlayObstacleHitSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayObstacleHit();
            Debug.Log("🔊 Obstacle hit sound!");
        }
    }
    
    /// <summary>
    /// Gọi method này để phát SFX Button Click
    /// </summary>
    public void PlayButtonClickSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
            Debug.Log("🔊 Button click sound!");
        }
    }
}