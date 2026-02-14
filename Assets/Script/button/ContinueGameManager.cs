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
        CheckContinueMode();
    }
    
    void CheckContinueMode()
    {
        int isContinue = PlayerPrefs.GetInt("IsContinue", 0);
        
        if (isContinue == 1)
        {
            RestoreGameState();
            
            PlayerPrefs.SetInt("IsContinue", 0);
            PlayerPrefs.Save();
        }
    }
    
    void RestoreGameState()
    {
        GhostController ghost = FindAnyObjectByType<GhostController>();
        if (ghost == null)
        {
            return;
        }
        
        int savedScore = PlayerPrefs.GetInt("ContinueScore", 0);
        ScoreManager scoreManager = FindAnyObjectByType<ScoreManager>();
        if (scoreManager != null)
        {
            scoreManager.SetScore(savedScore);
        }
        
        float respawnX = PlayerPrefs.GetFloat("RespawnX", 0f);
        float respawnY = PlayerPrefs.GetFloat("RespawnY", 0f) + respawnYOffset;
        
        ghost.transform.position = new Vector3(respawnX, respawnY, ghost.transform.position.z);
        
        float respawnVelocityY = PlayerPrefs.GetFloat("RespawnVelocityY", 0f);
        ghost.SetVelocity(new Vector2(0f, respawnVelocityY * velocityMultiplier));
        
        ghost.ResetGame();
        ghost.EnablePhysics(true);
        
        CameraFollow cameraFollow = FindAnyObjectByType<CameraFollow>();
        if (cameraFollow != null)
        {
            Vector3 camPos = cameraFollow.transform.position;
            camPos.y = respawnY;
            cameraFollow.transform.position = camPos;
        }
    }
}