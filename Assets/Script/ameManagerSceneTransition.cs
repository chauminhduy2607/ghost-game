using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 🎮 GAME MANAGER - CHUYỂN SCENE KHI GAME OVER
/// Gắn vào GameObject trong SampleScene
/// </summary>
public class GameManagerSceneTransition : MonoBehaviour
{
    [Header("=== REFERENCE ===")]
    [SerializeField] private GhostController ghostController;
    [SerializeField] private ScoreCycle scoreCycle;
    
    [Header("=== SETTINGS ===")]
    [SerializeField] private string gameOverSceneName = "GameOverScene";
    [SerializeField] private float delayBeforeTransition = 0.5f; // Đợi 0.5s trước khi chuyển scene
    
    private bool isTransitioning = false;
    
    void Start()
    {
        // Tự động tìm nếu chưa gắn
        if (ghostController == null)
            ghostController = FindObjectOfType<GhostController>();
        
        if (scoreCycle == null)
            scoreCycle = FindObjectOfType<ScoreCycle>();
        
        Debug.Log("🎮 GameManager: Ready to transition!");
    }
    
    void Update()
    {
        // Kiểm tra Ghost đã Game Over chưa
        if (!isTransitioning && ghostController != null && ghostController.IsGameOver)
        {
            StartTransition();
        }
    }
    
    /// <summary>
    /// Bắt đầu chuyển scene
    /// </summary>
    void StartTransition()
{
    isTransitioning = true;
    
    Debug.Log("💀 GAME OVER - Preparing to transition...");
    
    // Lấy điểm cuối
    int finalScore = 0;
    if (scoreCycle != null)
    {
        finalScore = scoreCycle.GetScore();
    }
    
    // ⭐ LƯU VỊ TRÍ CUỐI CÙNG (để Continue)
    // Vị trí này đã được PositionSaver lưu tự động mỗi 0.5s
    
    // Lưu điểm vào PlayerPrefs
    PlayerPrefs.SetInt("FinalScore", finalScore);
    PlayerPrefs.SetInt("ContinueScore", finalScore); // ⭐ Lưu điểm để Continue
    
    // Kiểm tra Best Score
    int bestScore = PlayerPrefs.GetInt("BestScore", 0);
    if (finalScore > bestScore)
    {
        bestScore = finalScore;
        PlayerPrefs.SetInt("BestScore", bestScore);
        Debug.Log("🏆 NEW BEST SCORE: " + bestScore);
    }
    
    PlayerPrefs.Save();
    
    Debug.Log("💾 Saved - Final Score: " + finalScore + " | Best: " + bestScore);
    
    // Chuyển scene sau delay
    Invoke("TransitionToGameOver", delayBeforeTransition);
}
    /// <summary>
    /// Chuyển sang GameOverScene
    /// </summary>
    void TransitionToGameOver()
    {
        Debug.Log("🔄 Loading GameOverScene...");
        SceneManager.LoadScene(gameOverSceneName);
    }
}