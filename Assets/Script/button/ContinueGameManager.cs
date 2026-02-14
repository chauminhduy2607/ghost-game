using UnityEngine;

public class ContinueGameManager : MonoBehaviour
{
    [Header("=== RESPAWN SETTINGS ===")]
    [Tooltip("Offset Y để ghost không spawn đúng vào obstacle")]
    [SerializeField] private float respawnYOffset = 2f;
    
    [Tooltip("Giảm velocity khi respawn (0-1, 0.5 = giảm 50%)")]
    [SerializeField] private float velocityMultiplier = 0.5f;
    
    void Start()
    {
        // ✅ Kiểm tra Continue mode
        CheckContinueMode();
        
        // ✅ KHÔNG pause game ở đây - để SwipeToStartManager xử lý
    }
    
    void CheckContinueMode()
    {
        int isContinue = PlayerPrefs.GetInt("IsContinue", 0);
        
        if (isContinue == 1)
        {
            Debug.Log("🔄 CONTINUE MODE DETECTED - Restoring game state...");
            RestoreGameState();
            
            // ✅ Xóa flag Continue sau khi restore
            PlayerPrefs.SetInt("IsContinue", 0);
            PlayerPrefs.Save();
        }
        else
        {
            Debug.Log("🆕 NEW GAME - Starting from beginning");
        }
    }
    
    void RestoreGameState()
    {
        // ✅ Tự động tìm Ghost
        GhostController ghost = FindAnyObjectByType<GhostController>();
        if (ghost == null)
        {
            Debug.LogError("❌ Ghost not found! Cannot restore state");
            return;
        }
        
        // ✅ Restore điểm số và tiếp tục tính điểm
        int savedScore = PlayerPrefs.GetInt("ContinueScore", 0);
        ScoreManager scoreManager = FindAnyObjectByType<ScoreManager>();
        if (scoreManager != null)
        {
            scoreManager.SetScore(savedScore);
            // ✅ KHÔNG gọi ResumeScore() ngay - đợi user swipe
            Debug.Log($"📊 Restored score: {savedScore}");
        }
        else
        {
            Debug.LogWarning("⚠️ ScoreManager not found - score not restored");
        }
        
        // ✅ Restore vị trí Ghost
        float respawnX = PlayerPrefs.GetFloat("RespawnX", 0f);
        float respawnY = PlayerPrefs.GetFloat("RespawnY", 0f) + respawnYOffset;
        
        ghost.transform.position = new Vector3(respawnX, respawnY, ghost.transform.position.z);
        Debug.Log($"📍 Ghost respawned at: ({respawnX:F1}, {respawnY:F1})");
        
        // ✅ Restore velocity (sẽ apply sau khi swipe)
        float respawnVelocityY = PlayerPrefs.GetFloat("RespawnVelocityY", 0f);
        ghost.SetVelocity(new Vector2(0f, respawnVelocityY * velocityMultiplier));
        
        // ✅ Reset trạng thái Ghost
        ghost.ResetGame();
        ghost.EnablePhysics(true);
        
        // ✅ Update camera position
        CameraFollow cameraFollow = FindAnyObjectByType<CameraFollow>();
        if (cameraFollow != null)
        {
            Vector3 camPos = cameraFollow.transform.position;
            camPos.y = respawnY;
            cameraFollow.transform.position = camPos;
            Debug.Log($"📹 Camera moved to Y={respawnY:F1}");
        }
        
        Debug.Log("✅ Game state restored - Waiting for swipe...");
    }
}