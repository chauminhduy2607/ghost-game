using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 🎮 GAME MANAGER - ĐƠN GIẢN HÓA
/// Chỉ việc: Phát hiện IsGameOver = true → Chuyển scene
/// Ghost tự lo phần ads
/// </summary>
public class GameManagerSceneTransition : MonoBehaviour
{
    [Header("=== REFERENCE ===")]
    [SerializeField] private GhostController ghostController;
    [SerializeField] private ScoreCycle scoreCycle;
    
    [Header("=== SETTINGS ===")]
    [SerializeField] private string gameOverSceneName = "GameOverScene";
    
    [Tooltip("Delay ngắn trước khi chuyển scene (cho mượt)")]
    [SerializeField] private float delayBeforeTransition = 0.5f;
    
    private bool isTransitioning = false;
    
    void Start()
    {
        // Tự động tìm nếu chưa gắn
        if (ghostController == null)
            ghostController = FindObjectOfType<GhostController>();
        
        if (scoreCycle == null)
            scoreCycle = FindObjectOfType<ScoreCycle>();
        
        Debug.Log("🎮 GameManager: Ready! (Simple mode)");
    }
    
    void Update()
    {
        // ✅ SIÊU ĐỠN GIẢN: Chỉ cần check IsGameOver
        if (!isTransitioning && ghostController != null && ghostController.IsGameOver)
        {
            Debug.Log("🎮 GameManager: Phát hiện IsGameOver = true → Chuyển scene");
            isTransitioning = true;
            StartCoroutine(TransitionToGameOver());
        }
    }
    
    IEnumerator TransitionToGameOver()
    {
        Debug.Log($"🎮 Đợi {delayBeforeTransition}s trước khi chuyển...");
        
        // Delay ngắn cho mượt
        yield return new WaitForSeconds(delayBeforeTransition);
        
        SaveScoreAndTransition();
    }
    
    void SaveScoreAndTransition()
    {
        Debug.Log("💀 GAME OVER - Saving scores...");
        
        // Lấy điểm cuối
        int finalScore = 0;
        if (scoreCycle != null)
        {
            finalScore = scoreCycle.GetScore();
        }
        
        // Lưu điểm
        PlayerPrefs.SetInt("FinalScore", finalScore);
        PlayerPrefs.SetInt("ContinueScore", finalScore);
        
        // Best Score
        int bestScore = PlayerPrefs.GetInt("BestScore", 0);
        if (finalScore > bestScore)
        {
            bestScore = finalScore;
            PlayerPrefs.SetInt("BestScore", bestScore);
            Debug.Log("🏆 NEW BEST SCORE: " + bestScore);
        }
        
        PlayerPrefs.Save();
        
        Debug.Log($"💾 Saved - Final: {finalScore} | Best: {bestScore}");
        Debug.Log($"🔄 Loading {gameOverSceneName}...");
        
        SceneManager.LoadScene(gameOverSceneName);
    }
}