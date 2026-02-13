using UnityEngine;

/// <summary>
/// Script này tự động bắt sự kiện va chạm và phát âm thanh
/// Không cần chỉnh code Obstacle hay Player
/// </summary>
public class AudioCollisionListener : MonoBehaviour
{
    [Header("Collision Settings")]
    [Tooltip("Tag của Player (thường là 'Player')")]
    public string playerTag = "Player";
    
    [Tooltip("Tag của Obstacle (thường là 'Obstacle')")]
    public string obstacleTag = "Obstacle";
    
    [Tooltip("Tag của Ghost (thường là 'Ghost')")]
    public string ghostTag = "Ghost";
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Kiểm tra nếu Player đụng Obstacle
        if (collision.gameObject.CompareTag(playerTag) && gameObject.CompareTag(obstacleTag))
        {
            PlayObstacleHitSound();
        }
        else if (collision.gameObject.CompareTag(obstacleTag) && gameObject.CompareTag(playerTag))
        {
            PlayObstacleHitSound();
        }
    }
    
    void OnMouseDown()
    {
        // Kiểm tra nếu tap vào Ghost
        if (gameObject.CompareTag(ghostTag))
        {
            PlayGhostTapSound();
        }
    }
    
    void PlayObstacleHitSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayObstacleHit();
            Debug.Log("🔊 Obstacle hit sound!");
        }
    }
    
    void PlayGhostTapSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGhostTap();
            Debug.Log("🔊 Ghost tap sound!");
        }
    }
}